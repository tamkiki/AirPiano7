using System;
using System.Text;
using System.Globalization;

namespace AirPiano7
{
    public sealed class NoteProtocol
    {
        private readonly StringBuilder pending = new StringBuilder();
        private bool dropping;

        public void Feed(string fragment, Action<int> accept)
        {
            foreach (char c in fragment)
            {
                if (c == '\n')
                {
                    if (!dropping && TryParse(pending.ToString(), out int id)) accept(id);
                    pending.Clear();
                    dropping = false;
                }
                else if (!dropping)
                {
                    if (pending.Length == 128) dropping = true;
                    else pending.Append(c);
                }
            }
        }

        private static bool TryParse(string line, out int id)
        {
            id = -1;
            var fields = line.Trim().Split(',');
            if (fields.Length != 3 || fields[0] != "NOTE") return false;
            var culture = CultureInfo.InvariantCulture;
            return int.TryParse(fields[1], NumberStyles.Integer, culture, out id)
                && id >= -1 && id < RhythmSession.LaneCount
                && double.TryParse(fields[2], NumberStyles.Float, culture, out double cm)
                && !double.IsNaN(cm) && !double.IsInfinity(cm)
                && (id == -1 ? cm == -1 : cm >= 0);
        }
    }

    public sealed class NoteTransition
    {
        private int previous = -1;

        public void Receive(int id, Action<int> onEntry)
        {
            if (id < -1 || id >= RhythmSession.LaneCount || previous == id) return;
            previous = id;
            if (id >= 0) onEntry(id);
        }
    }
}
