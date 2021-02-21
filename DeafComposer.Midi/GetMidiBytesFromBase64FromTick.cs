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
        /// come after a certain tick. In this way we can play a midi file starting
        /// from any arbitrary point in time
        /// 
        /// It works by removing all note events prior to the tick, and setting to 0 the deltatime
        /// of all non note events prior to tick
        /// </summary>
        public static byte[] GetMidiBytesFromTick(string base64EncodedMidi, long tick)
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
                // Update delta time so song starts at "tick"
                eventos = eventos.Select(x =>
                {
                    if (x.DeltaTime <= tick) x.DeltaTime = 0;
                    else x.DeltaTime -= tick;
                    return x;
                }).ToList();
                chunky.Events._events = ConvertAccumulatedTimeToDeltaTime(eventos);
                mf.Chunks.Add(chunky);
            }
            using (MemoryStream memStream = new MemoryStream(1000000))
            {
                mf.Write(memStream);
                return memStream.ToArray();
            }
        }
        public static byte[] GetMidiBytesFromPointInTime(string base64EncodedMidi, int secondsFromBeginningOfSong)
        {
            var tick = GetTickForPointInTime(base64EncodedMidi, secondsFromBeginningOfSong);
            return GetMidiBytesFromTick(base64EncodedMidi, tick);
        }
        public static byte[] GetMidiBytesFromPointInTime(MidiFile midiFile, int secondsFromBeginningOfSong)
        {
            var base64EncodedMidi = Base64EncodeMidiFile(midiFile);
            var tick = GetTickForPointInTime(base64EncodedMidi, secondsFromBeginningOfSong);
            return GetMidiBytesFromTick(base64EncodedMidi, tick);
        }
    }
}
