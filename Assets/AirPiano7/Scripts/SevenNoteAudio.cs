using UnityEngine;

namespace AirPiano7
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SevenNoteAudio : MonoBehaviour
    {
        public AudioClip[] clips = new AudioClip[RhythmSession.LaneCount];
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            for (var id = 0; id < RhythmSession.LaneCount; id++)
                if (!Clip(id)) Debug.LogError($"Air Piano: missing AudioClip for note ID {id}.", this);
        }

        private AudioClip Clip(int id) => clips != null && id >= 0 && id < clips.Length ? clips[id] : null;

        public void Play(int id, AudioClip pitched = null)
        {
            var clip = pitched ? pitched : Clip(id);
            if (clip) source.PlayOneShot(clip);
        }

        public void Silence() => source.Stop();
    }
}
