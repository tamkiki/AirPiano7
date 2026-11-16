using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AirPiano7
{
    public sealed class PianoGame : MonoBehaviour
    {
        [Serializable]
        public sealed class Lane
        {
            public string pitch;
            public KeyCode key;
            public Color color;
            public Image keyFace;
            public RectTransform track;
            public RectTransform notePrefab;
            public Image hintStrip;
            public RectTransform keySurface;
            public StageLight laneLight;
        }

        public PianoSong song;
        public Lane[] lanes;
        [Min(0.01f)] public float fallDuration = 4.5f;
        [Min(0.01f)] public float noteInterval = 3f;
        [Min(0)] public float hintLeadTime = 2f;
        [Min(0)] public float perfectWindow = 0.4f;
        public SevenNoteAudio piano;
        public AudioSource backingSource;
        public Text score, combo, progress, feedback, title, summary;
        public RectTransform progressFill, effectsLayer;
        public RectTransform sparkPrefab;
        public HitRing hitRing;
        public StageLight hitFlash;
        public GameObject startPanel, resultsPanel;
        public Button startButton, restartButton;
        public event Action SongStarted;
        public event Action<int, Judgement> NoteJudged;

        private readonly Dictionary<RhythmSession.Note, RectTransform> blocks =
            new Dictionary<RhythmSession.Note, RectTransform>();
        private readonly List<RectTransform> sparks = new List<RectTransform>();
        private readonly Dictionary<int, AudioClip> pitchedClips = new Dictionary<int, AudioClip>();
        private readonly bool[] highlighted = new bool[RhythmSession.LaneCount];
        private Color[] restColors;
        private Vector2[] keyPositions;
        private readonly double[] impacts = new double[RhythmSession.LaneCount];
        private Judgement lastResult;
        private Color lastHitColor;
        private RhythmSession session;
        private double started, lastFeedback;
        private double Clock => song.backing ? AudioSettings.dspTime : Time.unscaledTimeAsDouble;
        public double SongTime => Clock - started;
        private bool Playing => session != null && !session.Finished;

        private void Start()
        {
            title.text = song.title;
            if (song.pitchedClips != null)
                foreach (var sample in song.pitchedClips) pitchedClips[sample.midi] = sample.clip;
            restColors = Array.ConvertAll(lanes, lane => lane.keyFace.color);
            keyPositions = Array.ConvertAll(lanes, lane => lane.keySurface ? lane.keySurface.anchoredPosition : Vector2.zero);
            progress.text = $"0 / {song.notes.Length}";
            progressFill.anchorMax = new Vector2(0, 1);
        }

        public void Begin()
        {
            RhythmSession next;
            try
            {
                next = new RhythmSession(song.notes, noteInterval, fallDuration, hintLeadTime, perfectWindow);
            }
            catch (ArgumentException ex)
            {
                Debug.LogError(ex.Message, this);
                return;
            }

            foreach (var block in blocks.Values)
            {
                Destroy(block.gameObject);
            }
            blocks.Clear();
            ClearSparks();
            piano.Silence();
            startPanel.SetActive(false);
            resultsPanel.SetActive(false);
            feedback.text = "";
            lastFeedback = -10;
            for (int i = 0; i < impacts.Length; i++) impacts[i] = -10;
            started = Clock;
            if (song.backing)
            {
                backingSource.Stop();
                backingSource.clip = song.backing;
                backingSource.PlayScheduled(started + fallDuration);
            }
            session = next;
            session.Spawned += Spawn;
            session.Judged += OnJudgement;
            UpdateCounters();
            session.Advance(0);
            SongStarted?.Invoke();
        }

        public void Submit(int id)
        {
            if (Playing) session.TryHit(id, SongTime);
        }

        public void OpenSong(string scenePath) => SceneManager.LoadScene(System.IO.Path.GetFileNameWithoutExtension(scenePath));

        private void Update()
        {
            if (session == null) return;
            var now = SongTime;
            session.Advance(now);
            if (Playing)
                for (var i = 0; i < lanes.Length; i++)
                    if (UnityEngine.Input.GetKeyDown(lanes[i].key)) Submit(i);

            Array.Clear(highlighted, 0, highlighted.Length);
            foreach (var note in session.Active)
            {
                var block = blocks[note];
                var p = Mathf.Clamp01((float)((now - note.Spawn) / (note.Target - note.Spawn)));
                PositionNote(block, lanes[note.Id], p);
                highlighted[note.Id] |= session.IsEligible(note, now);
            }
            for (var i = 0; i < lanes.Length; i++)
            {
                var lane = lanes[i];
                float impact = 1 - Mathf.Clamp01((float)(now - impacts[i]) / .19f);
                float strength = highlighted[i] ? .47f + Mathf.Sin((float)now * 8) * .045f : 0;
                lane.keyFace.color = Color.Lerp(restColors[i], lane.color, Mathf.Max(strength, .53f * impact));
                if (lane.keySurface) lane.keySurface.anchoredPosition = keyPositions[i] - Vector2.up * (6 * impact);
                if (lane.hintStrip)
                {
                    var hint = lane.color;
                    hint.a = Mathf.Max(highlighted[i] ? .95f : .32f, impact);
                    lane.hintStrip.color = hint;
                }
                if (lane.laneLight)
                {
                    var glow = lane.color;
                    glow.a = Mathf.Max(highlighted[i] ? .29f : 0, impact * .46f);
                    lane.laneLight.color = glow;
                }
            }

            var age = Mathf.Clamp01((float)(now - lastFeedback) / .65f);
            var pulse = 1 + .14f * Mathf.Exp(-age * 8);
            score.rectTransform.localScale = combo.rectTransform.localScale = Vector3.one * pulse;
            feedback.rectTransform.localScale = Vector3.one * (1 + .23f * Mathf.Exp(-age * 7));
            var tint = feedback.color;
            tint.a = 1 - Mathf.SmoothStep(0, 1, age);
            feedback.color = tint;
            if (hitRing)
            {
                float ringAge = Mathf.Clamp01((float)(now - lastFeedback) / .33f);
                float size = Mathf.Lerp(.55f, lastResult == Judgement.Perfect ? 1.32f : 1f, ringAge);
                hitRing.rectTransform.localScale = new Vector3(size, size, 1);
                tint.a = lastResult == Judgement.Miss ? 0 : (1 - ringAge) * .85f;
                hitRing.color = tint;
                if (hitFlash)
                {
                    var flash = lastHitColor;
                    flash.a = lastResult == Judgement.Perfect ? (1 - ringAge) * .62f : 0;
                    hitFlash.color = flash;
                    hitFlash.rectTransform.localScale = new Vector3(size, size, 1);
                }
            }
            for (var i = 0; i < sparks.Count; i++)
            {
                var spark = sparks[i];
                var angle = i * Mathf.PI * 2 / sparks.Count;
                float travel = 1 - Mathf.Pow(1 - age, 3);
                spark.anchoredPosition = new Vector2(Mathf.Cos(angle) * 70, Mathf.Sin(angle) * 31 + 26) * travel;
                spark.localScale = Vector3.one * (1 - age);
            }
            if (age >= 1 && sparks.Count > 0) ClearSparks();

            bool backingFinished = !song.backing || now >= fallDuration + song.backing.length;
            if (session.Finished && backingFinished && !resultsPanel.activeSelf && age >= 1)
            {
                summary.text = $"{session.Score:N0} / {session.Total * 100:N0}\n\n" +
                    $"{session.Perfect} PERFECT     {session.Good} GOOD     {session.Miss} MISS\n" +
                    $"BEST COMBO  {session.BestCombo}";
                resultsPanel.SetActive(true);
            }
        }

        private void Spawn(RhythmSession.Note note)
        {
            var lane = lanes[note.Id];
            var block = Instantiate(lane.notePrefab, lane.track, false);
            if (note.MidiPitch > 0)
                block.GetComponentInChildren<Text>().text = PitchName(note);
            float seconds = note.DisplaySeconds > 0 && !float.IsInfinity(note.DisplaySeconds)
                ? note.DisplaySeconds : .75f;
            float height = (float)(seconds * lane.track.rect.height / (note.Target - note.Spawn));
            block.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Clamp(height, 54, lane.track.rect.height));
            PositionNote(block, lane, 0);
            blocks.Add(note, block);
        }

        private static void PositionNote(RectTransform block, Lane lane, float progress)
        {
            block.anchoredPosition = new Vector2(0, lane.track.rect.height * (1 - progress));
        }

        private void OnJudgement(RhythmSession.Note note, Judgement result)
        {
            var lane = lanes[note.Id];
            var block = blocks[note];
            blocks.Remove(note);
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
            if (result != Judgement.Miss && !song.backingIncludesPlayableNotes)
            {
                pitchedClips.TryGetValue(note.MidiPitch, out var pitchClip);
                piano.Play(note.Id, pitchClip);
            }
            feedback.text = $"{PitchName(note)}  {result.ToString().ToUpperInvariant()}";
            feedback.color = result == Judgement.Miss ? new Color(1, .43f, .57f) :
                result == Judgement.Perfect ? new Color(1, .87f, .46f) : lane.color;
            ClearSparks();
            if (result != Judgement.Miss)
            {
                lastHitColor = Color.Lerp(lane.color, Color.white, result == Judgement.Perfect ? .36f : .12f);
                impacts[note.Id] = SongTime;
                effectsLayer.position = lane.track.TransformPoint(new Vector3(0, -lane.track.rect.height / 2, 0));
                int count = result == Judgement.Perfect ? 7 : 4;
                for (int i = 0; i < count; i++)
                {
                    var spark = Instantiate(sparkPrefab, effectsLayer, false);
                    spark.GetComponent<Image>().color = i % 3 == 0 ? Color.white : lastHitColor;
                    sparks.Add(spark);
                }
            }
            lastResult = result;
            lastFeedback = SongTime;
            UpdateCounters();
            NoteJudged?.Invoke(note.Id, result);
        }

        private string PitchName(RhythmSession.Note note) => note.MidiPitch > 0
            ? $"{lanes[note.Id].pitch}{note.MidiPitch / 12 - 1}" : lanes[note.Id].pitch;

        private void ClearSparks()
        {
            foreach (var spark in sparks)
            {
                spark.gameObject.SetActive(false);
                Destroy(spark.gameObject);
            }
            sparks.Clear();
        }

        private void UpdateCounters()
        {
            score.text = session.Score.ToString("D5");
            combo.text = $"×{session.Combo}";
            progress.text = $"{session.Resolved} / {session.Total}";
            progressFill.anchorMax = new Vector2((float)session.Resolved / session.Total, 1);
        }
    }
}

