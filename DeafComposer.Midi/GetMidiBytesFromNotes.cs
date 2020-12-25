using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Standards;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static string GetMidiBytesFromNotes(List<Note> notes, List<TempoChange> tempoChanges = null)
        {
            var standardTicksPerQuarterNote = 96;
            notes = ResetTimeOfNotes(notes);
            var mf = new MidiFile();
            mf.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)standardTicksPerQuarterNote);
            var chunkitos = new Dictionary<GeneralMidi2Program, TrackChunk>();
            TrackChunk percusionChunk = null;
            var instrumentsChannels = new Dictionary<GeneralMidi2Program, byte>();
            foreach (var n in notes.OrderBy(n => n.StartSinceBeginningOfSongInTicks))
            {
                GeneralMidi2Program instrument = (GeneralMidi2Program)n.Instrument;
                if (n.IsPercussion)
                {
                    if (percusionChunk == null)
                        percusionChunk = new TrackChunk();
                    percusionChunk = AddEventsOfNote(percusionChunk, n, 9);
                }
                else
                {
                    if (!chunkitos.Keys.Contains(instrument))
                        chunkitos[instrument] = new TrackChunk();
                    if (!(instrumentsChannels.Keys).ToList().Contains((GeneralMidi2Program)n.Instrument))
                    {
                        byte channel = GetFreeChannelForNote(notes, instrumentsChannels, n);
                        instrumentsChannels[instrument] = channel;
                        chunkitos[instrument] = AddProgramChangeEventToChunk(chunkitos[instrument], channel,
                            n.StartSinceBeginningOfSongInTicks, (byte)n.Instrument);
                    }
                    chunkitos[instrument] = AddEventsOfNote(chunkitos[instrument], n, instrumentsChannels[instrument]);
                }
            }

            var channelIndependentChunkito = new TrackChunk();
            channelIndependentChunkito = AddSetTempoEvents(channelIndependentChunkito, tempoChanges);
            var allChunks = chunkitos.Values.ToList();
            if (percusionChunk != null)
                allChunks.Add(percusionChunk);
            if (channelIndependentChunkito.Events._events.Count > 0)
                allChunks.Add(channelIndependentChunkito);

            foreach (var chunky in allChunks)
            {
                chunky.Events._events = ConvertAccumulatedTimeToDeltaTime(chunky.Events.OrderBy(x => x.DeltaTime).ToList());
                mf.Chunks.Add(chunky);
            }
            using (MemoryStream memStream = new MemoryStream(1000000))
            {
                mf.Write(memStream);
                var bytes = memStream.ToArray();
                return Convert.ToBase64String(bytes);
            }
        }
        /// <summary>
        /// If we have an arbitrary group of notes from a song, they could be from the middle of the
        /// song. We want to create a midi file where the first note starts at time zero, so we
        /// have to substract the number of the ticks of the first note to all notes.
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> ResetTimeOfNotes(List<Note> notes)
        {
            if (notes == null || notes.Count == 0) return notes;
            var firstNote = notes?.OrderBy(n => n.StartSinceBeginningOfSongInTicks).First();
            return notes.Select(n => ResetTimeOfNote(n, firstNote.StartSinceBeginningOfSongInTicks)).ToList();
        }
        private static Note ResetTimeOfNote(Note n, long noTicks)
        {
            var notita = (Note)n.Clone();
            notita.StartSinceBeginningOfSongInTicks -= noTicks;
            return notita;
        }
        private static TrackChunk AddSetTempoEvents(TrackChunk chunkito, List<TempoChange> tc)
        {
            foreach (var t in tc)
            {
                var setTempoEvent = new SetTempoEvent()
                {
                    DeltaTime = t.TicksSinceBeginningOfSong,
                    MicrosecondsPerQuarterNote = t.MicrosecondsPerQuarterNote
                };
                chunkito.Events.Add(setTempoEvent);
            }
            return chunkito;
        }
        private static TrackChunk AddEventsOfNote(
            TrackChunk chunkito,
            Note n,
            byte channel)
        {
            var noteOn = new NoteOnEvent()
            {
                Channel = new FourBitNumber(channel),
                DeltaTime = n.StartSinceBeginningOfSongInTicks,
                NoteNumber = new SevenBitNumber(n.Pitch),
                Velocity = new SevenBitNumber(n.Volume)
            };
            chunkito.Events.Add(noteOn);
            var noteOff = new NoteOffEvent()
            {
                Channel = new FourBitNumber(channel),
                DeltaTime = n.EndSinceBeginningOfSongInTicks,
                NoteNumber = new SevenBitNumber(n.Pitch)
            };
            chunkito.Events.Add(noteOff);
            foreach (var pb in n.PitchBending)
            {
                var p = new PitchBendEvent()
                {
                    Channel = new FourBitNumber(channel),
                    DeltaTime = pb.TicksSinceBeginningOfSong,
                    PitchValue = pb.Pitch
                };
                chunkito.Events.Add(p);
            }
            return chunkito;
        }

        private static TrackChunk AddProgramChangeEventToChunk(
            TrackChunk chunkito,
            byte channel,
            long deltaTime,
            byte instrument)
        {
            var programChange = new ProgramChangeEvent()
            {
                Channel = new FourBitNumber(channel),
                DeltaTime = deltaTime,
                ProgramNumber = new SevenBitNumber(instrument)
            };
            chunkito.Events.Add(programChange);
            return chunkito;
        }
        private static byte GetFreeChannelForNote(
            List<Note> notes,
            Dictionary<GeneralMidi2Program, byte> instrumentsChannels,
            Note n)
        {
            var busyChannels = new List<byte>();
            foreach (var noti in GetSimultaneousNotesOf(notes, n))
            {
                foreach (var inst in instrumentsChannels.Keys)
                {
                    if (noti.Instrument == (byte)inst)
                        busyChannels.Add(instrumentsChannels[inst]);
                }
            }
            for (byte i = 0; i < 16; i++)
            {
                // Channel 9 is for percussion
                if (i == 9) continue;
                if (!busyChannels.Contains(i))
                {
                    return i;
                }
            }
            throw (new Exception("There are no enough channels for this song"));
        }
        private static List<Note> GetSimultaneousNotesOf(List<Note> notes, Note n)
        {
            var retObj = new List<Note>();
            foreach (var noti in notes)
            {
                if ((noti.StartSinceBeginningOfSongInTicks < n.EndSinceBeginningOfSongInTicks
                    && noti.EndSinceBeginningOfSongInTicks >= n.EndSinceBeginningOfSongInTicks) ||
                    (n.StartSinceBeginningOfSongInTicks < noti.EndSinceBeginningOfSongInTicks
                    && n.EndSinceBeginningOfSongInTicks >= noti.EndSinceBeginningOfSongInTicks))
                    retObj.Add(noti);
            }
            return retObj;
        }
    }
}
