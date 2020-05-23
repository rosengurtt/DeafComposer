using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static SongStats GetSongStats(string base64EncodedMidi)
        {
            var retObj = new SongStats();
            var midiFile = MidiFile.Read(base64EncodedMidi);

            retObj.TotalTracks = midiFile.Chunks.Count;
            retObj.DurationInSeconds = GetSongDurationInSeconds(base64EncodedMidi);
            retObj.TimeSignature = GetMainTimeSignatureOfSong(base64EncodedMidi);
            retObj.InstrumentsAsString = MidiUtilities.GetInstrumentsAsString(base64EncodedMidi);
            var channels = new List<FourBitNumber>();
            var pitches = new List<SevenBitNumber>();
            var uniquePitches = new List<int>();
            var instruments = new List<int>();
            var percussionInstruments = new List<int>();
            var tempoChanges = new List<int>();
            long songDurationInTicks = 0;
            var highestPitch = 0;
            var lowestPitch = 127;

            foreach (TrackChunk chunk in midiFile.Chunks)
            {
                long chunkDurationInTicks = 0;
                bool hasProgramChangeEvent = false;
                var chunkNoteEventsWithNullDeltas = 0;
                var chunkNoteEvents = 0;
                var chunkIsDrumChannel = false;
                var channelsInChunk = new List<FourBitNumber>();
                var averagePitch = 0;

                foreach (var eventito in chunk.Events)
                {
                    retObj.TotalEvents++;
                    chunkDurationInTicks += eventito.DeltaTime;
                    if (eventito is ChannelEvent)
                    {
                        var chanEv = (ChannelEvent)eventito;
                        if ((int)(chanEv.Channel) == 9)
                            chunkIsDrumChannel = true;
                        if (!(channelsInChunk.Contains(chanEv.Channel)))
                            channelsInChunk.Add(chanEv.Channel);
                        if (chanEv is NoteEvent)
                        {
                            var notEv = (NoteEvent)chanEv;
                            if (notEv.DeltaTime < 5)
                                chunkNoteEventsWithNullDeltas++;
                            chunkNoteEvents++;
                            if (!pitches.Contains(notEv.NoteNumber))
                                pitches.Add(notEv.NoteNumber);
                            if (!uniquePitches.Contains(notEv.NoteNumber.valor % 12))
                                uniquePitches.Add(notEv.NoteNumber.valor % 12);
                            if (notEv.NoteNumber.valor > highestPitch && notEv.Channel.valor != 9)
                                highestPitch = notEv.NoteNumber.valor;
                            if (notEv.NoteNumber.valor < lowestPitch && notEv.Channel.valor != 9)
                                lowestPitch = notEv.NoteNumber.valor;
                            averagePitch += (notEv.NoteNumber.valor - averagePitch) / chunkNoteEvents;
                            if (notEv.Channel.valor == 9)
                            {
                                if (!percussionInstruments.Contains(notEv.NoteNumber))
                                    percussionInstruments.Add(notEv.NoteNumber);
                            }
                        }
                        if (chanEv is PitchBendEvent)
                            retObj.TotalPitchBendEvents++;
                        if (chanEv is ProgramChangeEvent)
                        {
                            retObj.TotalProgramChangeEvents++;
                            var progChEv = (ProgramChangeEvent)chanEv;
                            if (!instruments.Contains(progChEv.ProgramNumber))
                                instruments.Add(progChEv.ProgramNumber);
                            if (hasProgramChangeEvent)
                                retObj.HasMoreThanOneInstrumentPerTrack = true;
                            hasProgramChangeEvent = true;
                        }
                        if (chanEv is ControlChangeEvent)
                        {
                            retObj.TotalControlChangeEvents++;
                            ControlChangeEvent ctrlChEv = chanEv as ControlChangeEvent;
                            if (ctrlChEv.ControlNumber == 64)
                                retObj.TotalSustainPedalEvents++;
                        }
                    }
                    else
                    {
                        retObj.TotalChannelIndependentEvents++;
                        if (eventito is SetTempoEvent)
                        {
                            var tempChEv = (SetTempoEvent)eventito;
                            tempoChanges.Add((int)tempChEv.MicrosecondsPerQuarterNote);
                        }
                    }
                }
                if (channelsInChunk.Count > 1)
                    retObj.HasMoreThanOneChannelPerTrack = true;
                foreach (var ch in channelsInChunk)
                {
                    if (!channels.Contains(ch))
                        channels.Add(ch);
                }
                retObj.TotalNoteEvents += chunkNoteEvents;
                if (chunkIsDrumChannel)
                    retObj.TotalPercussionTracks++;
                else
                {
                    if (chunkNoteEvents > 0 && (chunkNoteEventsWithNullDeltas / (double)chunkNoteEvents) > 0.75)
                        retObj.TotalChordTracks++;
                    else if (chunkNoteEvents > 0 && averagePitch > 55)
                        retObj.TotalMelodicTracks++;
                    else if (chunkNoteEvents > 0 && averagePitch <= 55)
                        retObj.TotalBassTracks++;
                }
                if (chunkNoteEvents == 0) retObj.TotalTracksWithoutNotes++;
                if (chunkDurationInTicks > songDurationInTicks)
                    songDurationInTicks = chunkDurationInTicks;
                if (!hasProgramChangeEvent && !instruments.Contains((SevenBitNumber)0))
                    instruments.Add((SevenBitNumber)0);
            }
            retObj.NumberOfTicks = (int)songDurationInTicks;
            retObj.TotalChannels = channels.Count;
            retObj.TotalInstruments = instruments.Count;
            retObj.TotalPercussionInstruments = percussionInstruments.Count;
            retObj.TotalDifferentPitches = pitches.Count;
            retObj.TotalUniquePitches = uniquePitches.Count;
            retObj.HighestPitch = highestPitch;
            retObj.LowestPitch = lowestPitch;
            retObj.TotalTempoChanges = tempoChanges.Count;
            if (tempoChanges.Count > 0)
            {
                retObj.TempoInMicrosecondsPerBeat = (int)Math.Floor(tempoChanges.Average());
                double microsoftIsShit = 120 * ((double)500000 / (double)retObj.TempoInMicrosecondsPerBeat);
                retObj.TempoInBeatsPerMinute = (int)Math.Floor(microsoftIsShit);
            }

            return retObj;

        }
    }
}
