using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {

        /// <summary>
        /// Calculates the tick corresponding to a number of seconds counted from the
        /// beginning of the song
        /// </summary>
        /// <param name="secondsFromBeginningOfSong"></param>
        /// <returns></returns>
        public static long GetTickForPointInTime(string base64EncodedMidi, int secondsFromBeginningOfSong)
        {
            var midiFile = MidiFile.Read(base64EncodedMidi);
            midiFile = ConvertDeltaTimeToAccumulatedTime(midiFile);
            int ticksPerBeat = ((TicksPerQuarterNoteTimeDivision)midiFile.TimeDivision).TicksPerQuarterNote;
            var totalDurationInTicks = GetSongDurationInTicks(base64EncodedMidi);
            var totalDurationInSeconds = GetSongDurationInSeconds(base64EncodedMidi);
            var tempoEvents = GetEventsOfType(midiFile, MidiEventType.SetTempo, true);
            var defaultTempo = 500000;
            if (tempoEvents == null || tempoEvents.Count == 0)
                return (totalDurationInTicks * secondsFromBeginningOfSong) / totalDurationInSeconds;

            double durationSoFar = GetSeconds(tempoEvents[0].DeltaTime, ticksPerBeat, defaultTempo);
            if (durationSoFar > secondsFromBeginningOfSong)
                return (long)Math.Floor((durationSoFar * secondsFromBeginningOfSong) / totalDurationInSeconds);

            double previousDuration = durationSoFar;

            for (int i = 0; i < tempoEvents.Count; i++)
            {
                var durationInTicks = (i < tempoEvents.Count - 1) ?
                    tempoEvents[i + 1].DeltaTime - tempoEvents[i].DeltaTime :
                    totalDurationInTicks - tempoEvents[i].DeltaTime;
                var tempo = ((SetTempoEvent)tempoEvents[i]).MicrosecondsPerQuarterNote;

                durationSoFar += GetSeconds(durationInTicks, ticksPerBeat, tempo);
                if (durationSoFar >= secondsFromBeginningOfSong)
                {
                    return tempoEvents[i].DeltaTime +
                        (long)Math.Floor(durationInTicks * (secondsFromBeginningOfSong - previousDuration)/ durationSoFar);
                }
                previousDuration = durationSoFar;
            }
            return totalDurationInTicks;
        }
    }
}
