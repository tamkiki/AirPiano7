using System;
using System.Collections.Generic;

namespace AirPiano7
{
    public enum Judgement { Perfect, Good, Miss }

    [Serializable]
    public struct SongNote
    {
        public int noteId;
        public float spawnBeat;
        // Length shown in the piano roll. A value of zero uses the default.
        public float displaySeconds;
        public int midiPitch;

        public SongNote(int id, float beat, float displaySeconds = 0)
        {
            noteId = id;
            spawnBeat = beat;
            this.displaySeconds = displaySeconds;
            midiPitch = 0;
        }
    }

    public sealed class RhythmSession
    {
        public const int LaneCount = 7;
        private const double Epsilon = 0.000001;

        public sealed class Note
        {
            public int Id { get; }
            public double Spawn { get; }
            public double Target { get; }
            public float DisplaySeconds { get; }
            public int MidiPitch { get; }
            public bool Resolved { get; internal set; }

            internal Note(int id, double spawn, double fall, float displaySeconds, int midiPitch)
            {
                Id = id;
                Spawn = spawn;
                Target = spawn + fall;
                DisplaySeconds = displaySeconds;
                MidiPitch = midiPitch;
            }
        }

        private readonly List<Note> schedule = new List<Note>();
        private readonly List<Note> active = new List<Note>();
        private readonly double lead;
        private readonly double perfectWindow;
        private int generated;

        public IReadOnlyList<Note> Active => active;
        public int Total => schedule.Count;
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Perfect { get; private set; }
        public int Good { get; private set; }
        public int Miss { get; private set; }
        public int Resolved => Perfect + Good + Miss;
        public bool Finished => generated == Total && active.Count == 0;
        public event Action<Note> Spawned;
        public event Action<Note, Judgement> Judged;

        public RhythmSession(IEnumerable<SongNote> chart, double interval, double fall, double lead, double perfect)
        {
            this.lead = Math.Min(fall, lead);
            perfectWindow = perfect;
            foreach (var entry in chart)
            {
                if (entry.noteId < 0 || entry.noteId >= LaneCount || entry.spawnBeat < 0 ||
                    float.IsNaN(entry.spawnBeat) || float.IsInfinity(entry.spawnBeat))
                    throw new ArgumentException("Invalid song entry.");
                schedule.Add(new Note(entry.noteId, entry.spawnBeat * interval, fall, entry.displaySeconds, entry.midiPitch));
            }
            if (schedule.Count == 0) throw new ArgumentException("The song has no notes.");
            schedule.Sort((a, b) => a.Spawn.CompareTo(b.Spawn));
        }

        public void Advance(double time)
        {
            while (generated < Total && schedule[generated].Spawn <= time)
            {
                var note = schedule[generated++];
                active.Add(note);
                Spawned?.Invoke(note);
            }
            while (active.Count > 0 && time >= active[0].Target)
                Resolve(0, Judgement.Miss);
        }

        public bool IsEligible(Note note, double time) =>
            !note.Resolved && time >= note.Target - lead && time < note.Target;

        public bool TryHit(int id, double time)
        {
            Advance(time);
            for (var i = 0; i < active.Count; i++)
            {
                var note = active[i];
                if (note.Id != id || !IsEligible(note, time)) continue;
                var age = time - (note.Target - lead);
                Resolve(i, age <= perfectWindow + Epsilon ? Judgement.Perfect : Judgement.Good);
                return true;
            }
            return false;
        }

        private void Resolve(int index, Judgement result)
        {
            var note = active[index];
            active.RemoveAt(index);
            note.Resolved = true;
            switch (result)
            {
                case Judgement.Perfect: Perfect++; Score += 100; break;
                case Judgement.Good: Good++; Score += 60; break;
                case Judgement.Miss: Miss++; break;
            }
            Combo = result == Judgement.Miss ? 0 : Combo + 1;
            BestCombo = Math.Max(Combo, BestCombo);
            Judged?.Invoke(note, result);
        }
    }
}
