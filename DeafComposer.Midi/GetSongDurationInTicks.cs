using Melanchall.DryWetMidi.Core;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static long GetSongDurationInTicks(string base64encodedMidiFile)
        {
            var midiFile = MidiFile.Read(base64encodedMidiFile);
            long duration = 0;
            foreach (TrackChunk chunk in midiFile.Chunks)
            {
                long trackDuration = 0;
                foreach (var eventito in chunk.Events)
                {
                    trackDuration += eventito.DeltaTime;
                }
                if (trackDuration > duration) duration = trackDuration;
            }
            return duration;
        }
    }
}
