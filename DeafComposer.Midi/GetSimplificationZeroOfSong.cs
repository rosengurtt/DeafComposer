using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static SongSimplification GetSimplificationZeroOfSong(string base64encodedMidiFile)
        {
            var notesObj = new List<Note>();
            var midiFile = MidiFile.Read(base64encodedMidiFile);
            long songDuration = GetSongDurationInTicks(base64encodedMidiFile);
            var isSustainPedalOn = false;
            var notesOnBecauseOfSustainPedal = new List<Note>();
            var instrumentOfChannel = new byte[16];

            short chunkNo = -1;
            foreach (TrackChunk chunk in midiFile.Chunks)
            {
                chunkNo++;
                var currentNotes = new List<Note>();
                long currentTick = 0;

                foreach (MidiEvent eventito in chunk.Events)
                {
                    currentTick += eventito.DeltaTime;

                    if (eventito is ProgramChangeEvent)
                    {
                        var pg = eventito as ProgramChangeEvent;
                        instrumentOfChannel[pg.Channel] = (byte)pg.ProgramNumber.valor;
                        continue;
                    }

                    if (IsSustainPedalEventOn(eventito))
                    {
                        isSustainPedalOn = true;
                        continue;
                    }

                    if (IsSustainPedalEventOff(eventito))
                    {
                        isSustainPedalOn = false;
                        foreach (var n in notesOnBecauseOfSustainPedal)
                        {
                            ProcessNoteOff(n.Pitch, currentNotes, notesObj, currentTick,
                                n.Instrument, (byte)chunkNo);
                        }
                        continue;
                    }
                    if (eventito is NoteOnEvent)
                    {
                        NoteOnEvent noteOnEvent = eventito as NoteOnEvent;
                        if (noteOnEvent.Velocity > 0 || isSustainPedalOn == false)
                        {
                            ProcessNoteOn(noteOnEvent.NoteNumber, noteOnEvent.Velocity,
                                currentNotes, notesObj, currentTick,
                                instrumentOfChannel[noteOnEvent.Channel],
                                IsPercussionEvent(eventito), (byte)chunkNo);
                        }
                        continue;
                    }
                    if (eventito is NoteOffEvent && isSustainPedalOn == false)
                    {
                        NoteOffEvent noteOffEvent = eventito as NoteOffEvent;
                        ProcessNoteOff(noteOffEvent.NoteNumber, currentNotes, notesObj, currentTick,
                            instrumentOfChannel[noteOffEvent.Channel], (byte)chunkNo);
                        continue;
                    }
                    if (eventito is PitchBendEvent)
                    {
                        PitchBendEvent bendito = eventito as PitchBendEvent;
                        foreach (var notita in currentNotes)
                        {
                            notita.PitchBending.Add(new PitchBendItem
                            {
                                Note = notita,
                                Pitch = bendito.PitchValue,
                                TicksSinceBeginningOfSong = currentTick
                            });
                        }
                        continue;
                    }
                }
            }

            notesObj = notesObj.OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();

            // Split voices that have more than one melody playing at the same time
            notesObj = SplitPolyphonicVoiceInMonophonicVoices(notesObj);
            // Reorder voices so when we have for ex the left and right hand of a piano in 2 voices, the right hand comes first
            notesObj = ReorderVoices(notesObj);
            var retObj = new SongSimplification()
            {
                Notes = notesObj,
                SimplificationVersion = 0,
                NumberOfVoices = chunkNo
            };
            return retObj;
        }

        /// <summary>
        /// We want the drum voices to be at the end, and when there are several voices with the same instrument
        /// we want the higher pitch voices first (in this way if we have the left and the right hand of a piano
        /// in 2 different voices, we want the right hand first)
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> ReorderVoices(List<Note> notes)
        {
            // The variable newVoice is used to generate the numbers of the reordered voices
            byte newVoice = 0;
            var retObj = new List<Note>();
            // first split the notes by instrument
            var noteInstrument = notes.Where(m => m.IsPercussion == false).GroupBy(n => n.Instrument).OrderBy(x => x);
            // now loop by instrument
            foreach (var noteInstGroup in noteInstrument)
            {
                // get notes of instrument
                var instrNotes = notes.Where(n => n.Instrument == noteInstGroup.Key);
                // get the order of this instrument voices by pitch
                var orderedInstrVoices = OrderInstrumentVoicesByPitch(instrNotes);
                // loop on the instrument voices
                foreach (var voice in orderedInstrVoices)
                {
                    // add to the return object the notes of this voice, assigning a new voice number
                    foreach (var n in notes.Where(m => m.Voice == voice))
                    {
                        var m = n.Clone();
                        m.Voice = newVoice;
                        retObj.Add(m);
                    }
                    // increment the voice number so the next group of notes will get the next available integer
                    newVoice++;
                }
            }
            // now add the percusion notes
            foreach(var n in notes.Where(n => n.IsPercussion == true))
            {
                var m = n.Clone();
                m.Voice = newVoice;
                retObj.Add(m);
            }
            return retObj;
        }
        // Returns the voice numbers ordered by the average pitch of their notes, higher averages first
        private static IEnumerable<byte> OrderInstrumentVoicesByPitch(IEnumerable<Note> notes)
        {
            var voicesAveragePitches = new Dictionary<byte, double>();
            foreach (var v in GetInstrumentVoicesOfNotes(notes))
            {
                voicesAveragePitches[v] = getAveragePitchOfNOtes(notes.Where(n => n.Voice == v));
            }
            var reorderedDic = voicesAveragePitches.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            return reorderedDic.Keys;
        }

        private static double getAveragePitchOfNOtes(IEnumerable<Note> notes)
        {
            return notes.Average(n => n.Pitch);
        }
        private static List<byte> GetInstrumentVoicesOfNotes(IEnumerable<Note> notes)
        {
            var instrumentsNotes = notes.Where(n => n.IsPercussion == false);
            return instrumentsNotes.Select(n => n.Voice).Distinct().ToList();
        }
        private static List<byte> getPercusionVoicesOfNotes(List<Note> notes)
        {
            var instrumentsNotes = notes.Where(n => n.IsPercussion == true);
            return instrumentsNotes.Select(n => n.Voice).Distinct().ToList();
        }

        /// <summary>
        /// A single track may be polyphonic in the sense that 2 or more notes that start and stop 
        /// independently are played simultaneously. When the 2 notes start and stop at the
        /// same time, then we can consider them to be 1 voice.
        /// We split these tracks so each voice of the song simplification is really 1 voice
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> SplitPolyphonicVoiceInMonophonicVoices(List<Note> notes)
        {
            var retObj = new List<Note>();
            // in voicesNotes we have the original notes separated by voice
            var voicesNotes = GetNotesAsDictionary(notes);
            // we will build this new dictionary that possibly will have more voices, but the same total of notes
            var newVoicesNotes = new Dictionary<byte, List<Note>>();
            byte voice = 0;
            foreach (byte v in voicesNotes.Keys)
            {
                // We keep in this variable the notes of this voice that we still haven't added to newVoicesNotes
                // We initialize it with all the notes of the voice
                var remainingNotes = new List<Note>(voicesNotes[v]);
                List<Note> upperVoice;
                while (remainingNotes.Count > 0)
                {
                    upperVoice = GetUpperVoice(remainingNotes);
                    newVoicesNotes[voice++] = upperVoice;
                    upperVoice.ForEach(x => remainingNotes.Remove(x));
                }
            }
            // We have now to reassing each note to the right voice
            foreach(var v in newVoicesNotes.Keys)
            {
                foreach (var n in newVoicesNotes[v])
                {
                    n.Voice = v;
                    retObj.Add(n);
                }
            }
            return retObj;
        }
        /// <summary>
        /// Separates a list of notes in diferent lists, one for each voice (that is the key of the dictionary)
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static Dictionary<byte, List<Note>> GetNotesAsDictionary(List<Note> notes)
        {
            var retObj = new Dictionary<byte, List<Note>>();
            foreach (var n in notes)
            {
                if (!retObj.ContainsKey(n.Voice)) retObj[n.Voice] = new List<Note>();
                retObj[n.Voice].Add(n);
            }
            return retObj;
        }
        // Given a set of notes where we may have polyphony, it returns the upper voice
        // When there are several notes playing at the same time but they start and end together, then all of
        // them are returned. The notes not returned are notes played simultaneously with upper notes,
        // that start and/or stop on different times of the upper notes  by a margin greater than 'tolerance'
        private static List<Note> GetUpperVoice(List<Note> notes)
        {
            var retObj = new List<Note>();
            foreach (var n in notes.OrderBy(m => m.StartSinceBeginningOfSongInTicks))
            {
                var tolerance = n.DurationInTicks / 10;

                var simulNotes = notes.Where(m =>
                // intersection is significant
                Math.Max(m.StartSinceBeginningOfSongInTicks, n.StartSinceBeginningOfSongInTicks) <
                Math.Min(m.EndSinceBeginningOfSongInTicks, n.EndSinceBeginningOfSongInTicks) - tolerance &&
                // they don't start and end both at the same time
                (Math.Abs(m.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) > tolerance ||
                Math.Abs(m.EndSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks) > tolerance) &&
                // pitch is higher than note under consideration
                (m.Pitch > n.Pitch ||
                //pitch is the same as note under consideration, but duration is longer
                (m.Pitch == n.Pitch && m.DurationInTicks > n.DurationInTicks)))
                .ToList();
                if (simulNotes.Where(m => m.Pitch > n.Pitch).ToList().Count == 0)
                {
                    retObj.Add(n);
                    // add also notes that start and end both at the same time
                    var chordNotes = notes.Where(m =>
                          Math.Max(m.StartSinceBeginningOfSongInTicks, n.StartSinceBeginningOfSongInTicks) < tolerance &&
                          Math.Max(m.EndSinceBeginningOfSongInTicks, n.EndSinceBeginningOfSongInTicks) < tolerance &&
                          m.Id != n.Id)
                        .ToList();
                    chordNotes.ForEach(x => retObj.Add(x));
                }
            }
            return retObj;
        }

        private static bool IsSustainPedalEventOn(MidiEvent eventito)
        {
            if (!(eventito is ControlChangeEvent)) return false;
            ControlChangeEvent eve = eventito as ControlChangeEvent;
            if (eve.ControlNumber == 64 && eve.ControlValue > 63) return true;
            return false;
        }
        private static bool IsSustainPedalEventOff(MidiEvent eventito)
        {
            if (!(eventito is ControlChangeEvent)) return false;
            ControlChangeEvent eve = eventito as ControlChangeEvent;
            if (eve.ControlNumber == 64 && eve.ControlValue < 64) return true;
            return false;
        }


        private static void ProcessNoteOn(byte pitch, byte volume, List<Note> currentNotes,
                List<Note> retObj, long currentTick, byte instrument,
                bool isPercussion, byte voice)
        {

            if (volume > 0)
            {
                var notita = new Note
                {
                    Instrument = instrument,
                    Pitch = pitch,
                    StartSinceBeginningOfSongInTicks = currentTick,
                    Volume = volume,
                    IsPercussion = isPercussion,
                    Voice = voice
                };
                currentNotes.Add(notita);
            }
            else
            {
                var notota = currentNotes.Where(n => n.Pitch == pitch).FirstOrDefault();
                if (notota != null)
                {
                    notota.EndSinceBeginningOfSongInTicks = currentTick;
                    retObj.Add(notota);
                    currentNotes.Remove(notota);
                }
            }
        }
        private static void ProcessNoteOff(byte pitch, List<Note> currentNotes,
         List<Note> retObj, long currentTick, byte intrument, byte voice)
        {
            ProcessNoteOn(pitch, 0, currentNotes, retObj, currentTick, intrument, false, voice);
        }

        private static bool IsPercussionEvent(MidiEvent eventito)
        {
            if (!(eventito is ChannelEvent)) return false;
            var chEv = eventito as ChannelEvent;
            if (chEv.Channel == 9)
                return true;
            return false;
        }

    }
}
