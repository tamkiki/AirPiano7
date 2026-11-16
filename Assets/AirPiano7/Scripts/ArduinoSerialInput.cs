using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

namespace AirPiano7
{
    public sealed class ArduinoSerialInput : MonoBehaviour
    {
        public string portName = "COM3";
        [Min(1)] public int baudRate = 9600;
        public PianoGame game;

        private sealed class Connection
        {
            public readonly ConcurrentQueue<int> Notes = new ConcurrentQueue<int>();
            public readonly ConcurrentQueue<string> Status = new ConcurrentQueue<string>();
            public volatile bool Stop;
        }

        private Connection connection;
        private NoteTransition transitions;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            var channel = new Connection();
            connection = channel;
            transitions = new NoteTransition();
            string port = portName;
            int speed = baudRate;
            new Thread(() => ReadPort(channel, port, speed)) { IsBackground = true, Name = "Air Piano serial" }.Start();
        }

        private void OnDisable()
        {
            if (connection != null) connection.Stop = true;
            connection = null;
        }

        private void Update()
        {
            if (connection == null) return;
            while (connection.Status.TryDequeue(out string status))
                Debug.LogWarning(status, this);
            while (connection.Notes.TryDequeue(out int note))
                transitions.Receive(note, game.Submit);
        }

        private static void ReadPort(Connection channel, string port, int speed)
        {
            try
            {
                using (var serial = new SerialPort(port, speed) { ReadTimeout = 100 })
                {
                    serial.Open();
                    var protocol = new NoteProtocol();
                    var buffer = new char[128];
                    while (!channel.Stop)
                    {
                        try
                        {
                            int count = serial.Read(buffer, 0, buffer.Length);
                            protocol.Feed(new string(buffer, 0, count), channel.Notes.Enqueue);
                        }
                        catch (TimeoutException) { }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!channel.Stop) channel.Status.Enqueue($"Air Piano serial {port}: {ex.Message}");
            }
            finally
            {
                channel.Notes.Enqueue(-1);
            }
        }
    }
}
