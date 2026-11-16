using UnityEngine;

namespace AirPiano7
{
    public sealed class SerialSimulator : MonoBehaviour
    {
        public PianoGame game;
        private readonly NoteProtocol protocol = new NoteProtocol();
        private readonly NoteTransition transitions = new NoteTransition();
        private string message = "NOTE,0,31.0";
        private bool show;

        public void Feed(string fragment) => protocol.Feed(fragment, id => transitions.Receive(id, game.Submit));

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2)) show = !show;
        }

        private void OnGUI()
        {
            if (!show) return;
            GUILayout.BeginArea(new Rect(16, Screen.height - 180, 330, 164), GUI.skin.box);
            GUILayout.Label("Arduino simulator • F2 to close");
            message = GUILayout.TextField(message, 128);
            if (GUILayout.Button("Send NOTE line")) Feed(message + "\n");
            GUILayout.BeginHorizontal();
            for (int id = 0; id < RhythmSession.LaneCount; id++)
                if (GUILayout.Button(id.ToString())) Feed($"NOTE,{id},31.0\n");
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Release")) Feed("NOTE,-1,-1.0\n");
            GUILayout.EndArea();
        }
    }
}
