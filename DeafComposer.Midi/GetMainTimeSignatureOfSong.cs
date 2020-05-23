using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static TimeSignature GetMainTimeSignatureOfSong(string base64encodedMidiFile)
        {
            var midiFile = MidiFile.Read(base64encodedMidiFile);
            midiFile = ConvertDeltaTimeToAccumulatedTime(midiFile);
            var timeSignatureEvents = GetEventsOfType(midiFile, MidiEventType.TimeSignature);
            if (timeSignatureEvents == null || timeSignatureEvents.Count == 0)
                return new TimeSignature { Numerator = 4, Denominator = 4 };
            // ticksPerSignatures has the number of ticks spent in each signature
            // the key is a string with the form 'numerator_denomimator'
            var ticksPerSignatures = new Dictionary<string, long>();
            var songDurationInTicks = GetSongDurationInTicks(base64encodedMidiFile);

            for (int i = 0; i < timeSignatureEvents.Count; i++)
            {
                var signatureEvent = (TimeSignatureEvent)timeSignatureEvents[i];
                string currentSignatureAsString =
                    $"{signatureEvent.Numerator.ToString()}_{signatureEvent.Denominator}";

                if (!ticksPerSignatures.Keys.Contains(currentSignatureAsString))
                    ticksPerSignatures[currentSignatureAsString] = 0;
                long beginningOfPeriod = timeSignatureEvents[i].DeltaTime;
                long endOfPeriod = (i < timeSignatureEvents.Count - 1) ?
                    timeSignatureEvents[i + 1].DeltaTime : songDurationInTicks;
                ticksPerSignatures[currentSignatureAsString] += endOfPeriod - beginningOfPeriod;
            }
            long? max = null;
            TimeSignature retObj = null;
            foreach (var key in ticksPerSignatures.Keys)
            {
                if (max == null || max < ticksPerSignatures[key])
                {
                    max = ticksPerSignatures[key];
                    var nums = key.Split("_");
                    retObj = new TimeSignature
                    {
                        Numerator = int.Parse(nums[0]),
                        Denominator = int.Parse(nums[1])
                    };
                }
            }
            return retObj;
        }
    }
}
