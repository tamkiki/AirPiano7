using UnityEngine;

namespace AirPiano7
{
    [CreateAssetMenu(menuName = "Air Piano/Song")]
    public sealed class PianoSong : ScriptableObject
    {
        public string title = "Afterglow";
        [Tooltip("Spawn beat multiplied by the controller's Note Interval in seconds.")]
        public SongNote[] notes;
        public AudioClip backing;
        public bool backingIncludesPlayableNotes;
        public PitchedClip[] pitchedClips;

        [System.Serializable]
        public struct PitchedClip
        {
            public int midi;
            public AudioClip clip;
        }
    }
}
