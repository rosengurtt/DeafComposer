using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// Generates the list of bar entities of a midi file
        /// </summary>
        /// <param name="base64encodedMidiFile"></param>
        /// <returns></returns>
        public static List<Bar> GetBarsOfSong(string base64encodedMidiFile, SongSimplification songSimplification)
        {
            List<Bar> retObj = new List<Bar>();
            int barNumber = 1;

            var ticksPerBeat = GetTicksPerBeatOfSong(base64encodedMidiFile);
            var songDurationInTicks = GetSongDurationInTicks(base64encodedMidiFile);
            var timeSignatureEvents = GetEventsOfType(base64encodedMidiFile, MidiEventType.TimeSignature);
            var setTempoEvents = GetEventsOfType(base64encodedMidiFile, MidiEventType.SetTempo);
            timeSignatureEvents = ConvertDeltaTimeToAccumulatedTime(timeSignatureEvents);
            var TempoEvents = QuantizeTempos(ConvertDeltaTimeToAccumulatedTime(setTempoEvents));

            //status
            TimeSignatureEvent currentTimeSignature = new TimeSignatureEvent
            {
                Numerator = 4,
                Denominator = 4
            };
            if (timeSignatureEvents.Count > 0)
                currentTimeSignature = (TimeSignatureEvent)timeSignatureEvents[0];

            int currentTempo = 500000;

            int timeSigIndex = 0;
            int tempoIndex = 0;
            long currentTick = 0;

            while (currentTick < songDurationInTicks)
            {
                if (TempoEvents.Count > 0)
                    currentTempo = (int)TempoEvents[tempoIndex].MicrosecondsPerQuarterNote;
                long timeOfNextTimeSignatureEvent = songDurationInTicks;
                if (timeSignatureEvents.Count - 1 > timeSigIndex)
                    timeOfNextTimeSignatureEvent = timeSignatureEvents[timeSigIndex + 1].DeltaTime;
                long timeOfNextSetTempoEvent = songDurationInTicks;
                if (TempoEvents.Count - 1 > tempoIndex)
                    timeOfNextSetTempoEvent = TempoEvents[tempoIndex + 1].DeltaTime;

                long lastTickOfBarToBeAdded = currentTimeSignature.Numerator * ticksPerBeat + currentTick;

                while ((lastTickOfBarToBeAdded <= timeOfNextTimeSignatureEvent && lastTickOfBarToBeAdded <= timeOfNextSetTempoEvent) ||
                    (lastTickOfBarToBeAdded > songDurationInTicks))
                {
                    var timeSignature = new TimeSignature
                    {
                        Numerator = currentTimeSignature.Numerator,
                        Denominator = currentTimeSignature.Denominator
                    };
                    var bar = new Bar
                    {
                        BarNumber = barNumber++,
                        TicksFromBeginningOfSong = currentTick,
                        TimeSignature = timeSignature,
                        TempoInMicrosecondsPerQuarterNote = currentTempo
                    };
                    bar.HasTriplets = HasBarTriplets(songSimplification, bar);
                    retObj.Add(bar);
                    currentTick += currentTimeSignature.Numerator * ticksPerBeat;
                    lastTickOfBarToBeAdded += currentTimeSignature.Numerator * ticksPerBeat;
                    if (currentTick >= songDurationInTicks)
                        break;
                }
                if (lastTickOfBarToBeAdded >= timeOfNextTimeSignatureEvent)
                    timeSigIndex++;
                if (lastTickOfBarToBeAdded >= timeOfNextSetTempoEvent)
                    tempoIndex++;
            }
            return retObj;
        }
    }
}
