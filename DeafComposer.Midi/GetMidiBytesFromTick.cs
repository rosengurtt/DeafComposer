using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// Given a midi file of a song, it returns a midi file with all the notes that
        /// come before a certain tick removed. In this way we can play a midi file starting
        /// from any arbitrary point in time
        /// </summary>
        public static string GetMidiBytesFromTick(string base64EncodedMidi, long tick)
        {
            var midiFile = MidiFile.Read(base64EncodedMidi);
            var mf = new MidiFile();
            mf.TimeDivision = midiFile.TimeDivision;

            foreach (TrackChunk ch in midiFile.Chunks)
            {
                var chunky = new TrackChunk();
                var acumChunk = ConvertDeltaTimeToAccumulatedTime(ch.Events.ToList());
                // We filter out note on and note off events that come before tick
                var eventos = acumChunk.Where(x =>
                (x.EventType != MidiEventType.NoteOn && x.EventType != MidiEventType.NoteOff)
                || x.DeltaTime > tick).OrderBy(y => y.DeltaTime).ToList();
                chunky.Events._events = ConvertAccumulatedTimeToDeltaTime(eventos);
                mf.Chunks.Add(chunky);
            }
            using (MemoryStream memStream = new MemoryStream(1000000))
            {
                mf.Write(memStream);
                var bytes = memStream.ToArray();
                return Convert.ToBase64String(bytes);
            }
        }

        public static string GetMidiBytesFromPointInTime(string base64EncodedMidi, int secondsFromBeginningOfSong)
        {
            var tick = GetTickForPointInTime(base64EncodedMidi, secondsFromBeginningOfSong);
            return GetMidiBytesFromTick(base64EncodedMidi, tick);
        }
    }
}
