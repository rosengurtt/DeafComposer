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
            notesObj = notesObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
            notesObj = QuantizeNotes(notesObj);
            notesObj = RemoveDuplicateNotes(notesObj);
            notesObj = CorrectNotesTimings(notesObj);

            // Split voices that have more than one melody playing at the same time
            notesObj = SplitPolyphonicVoiceInMonophonicVoices(notesObj);

            // Reorder voices so when we have for ex the left and right hand of a piano in 2 voices, the right hand comes first
            notesObj = ReorderVoices(notesObj);

            if (!AreVoicesMonophonic(notesObj))
            {
                var kiki = GetProblematicNotes(notesObj);
            }
            var retObj = new SongSimplification()
            {
                Notes = notesObj,
                SimplificationVersion = 0,
                NumberOfVoices = chunkNo
            };
            return retObj;
        }
        // Used to check that we have splitted the notes in different voices correctly
        // After the split, we shouldn't have simultaneous notes that are not exactly simultaneous
        // When 2 or more notes start and end together, we consider them a chord and it is OK to have
        // them in a monophonic voice. But otherwise, if they play together for a while but not all
        // the time, then they are problematic and they shouldn't be there, something went wrong
        // when we did the split
        private static List<List<Note>> GetProblematicNotes(List<Note> notes, int tolerance = 0, bool notesOfSamePitch = false)
        {
            var retObj = new List<List<Note>>();
            var voices = GetVoicesOfNotes(notes);
            foreach (var v in voices)
            {
                foreach (var n in notes.Where(x => x.Voice == v))
                {
                    foreach (var m in notes.Where(y => y.Voice == v && n.TempId != y.TempId))
                    {
                        if (GetIntersectionOfNotesInTicks(m, n) == 0) continue;
             
                        var soret = new List<Note>();
                        if (!AreNotesExactlySimultaneous(m, n))
                        {
                            soret.Add(m);
                            soret.Add(n);
                        }
                        if (soret.Count > 0) retObj.Add(soret);
                    }
                }
            }
            return retObj;
        }


        /// <summary>
        /// If we have 2 notes with the same pitch and the same instrument playing at the same time, this
        /// is problematic for the analysis and displaying. So we do one of 2 things:
        /// - if the notes start at the same time (or almost at the same time) we remove one
        /// - if the notes start in different times, we shorten the first, so they don't play simultaneously
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> RemoveDuplicateNotes(List<Note> notes)
        {
            // We first copy all the notes to retObj, we will then remove and alter notes in rettObj, but the original notes are left unchanged
            var retObj = new List<Note>();
            notes.ForEach(n => retObj.Add(n.Clone()));
            retObj = retObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
            // If 2 notes with the same pitch and the same instrument start at the same time, we remove the 
            // one with the lower volume, or if the volume is more or less the same, the shortest one
            var notesToRemove = new List<Note>();
            var volumeTolerance = 5;    // The amount the volumes have to differ to consider them different
            var voices = GetVoicesOfNotes(retObj);
            foreach (var v in voices)
            {
                var notesOfVoice = retObj.Where(w => w.Voice == v)
                                    .OrderBy(z => z.StartSinceBeginningOfSongInTicks).ToList();
                for (var i = 0; i < notesOfVoice.Count - 1; i++)
                {
                    for (var j = i + 1; j < notesOfVoice.Count; j++)
                    {
                        var ni = notesOfVoice[i];
                        var nj = notesOfVoice[j];
                        // This deduplication applies only when notes are played by the same instrument
                        if (ni.Instrument != nj.Instrument) continue;
                        // Check if they play simultaneously and if they have the same pitch
                        if (GetIntersectionOfNotesInTicks(ni, nj) == 0 || ni.Pitch != nj.Pitch)
                            continue;
                        // Found duplicates. Check if they start together or not
                        var timeTolerance = GetTolerance(ni, nj);
                        if (Math.Abs(ni.StartSinceBeginningOfSongInTicks - nj.StartSinceBeginningOfSongInTicks) < timeTolerance)
                        {
                            // They start together, Select the one to remove and store it in notesToRemove
                            if (Math.Abs(ni.Volume - nj.Volume) > volumeTolerance)
                            {
                                var quieterNote = ni.Volume < nj.Volume ? ni : nj;
                                notesToRemove.Add(quieterNote);
                                continue;
                            }
                            var shorterNote = ni.DurationInTicks < nj.DurationInTicks ? ni : nj;
                            notesToRemove.Add(shorterNote);
                        }
                        else
                        {
                            // They don't start together. Shorten the first one
                            var firstNote = ni.StartSinceBeginningOfSongInTicks < nj.StartSinceBeginningOfSongInTicks ? ni : nj;
                            var secondNote = ni.StartSinceBeginningOfSongInTicks < nj.StartSinceBeginningOfSongInTicks ? nj : ni;
                            firstNote.EndSinceBeginningOfSongInTicks = secondNote.StartSinceBeginningOfSongInTicks;
                        }
                    }
                }

            }
            // Now remove the duplicate notes
            foreach (var n in notesToRemove) retObj.Remove(n);
            return retObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
        }

        /// <summary>
        /// When we have 2 notes starts or ends happening almost at the same time but not exactly
        /// in most cases they are meant to happen at the same time. If one of the notes is very short, then the fact
        /// that they start at short different times may be on purpose like when we have an embelishment
        /// We try to find the notes that are meant to start together but are not, and make them start exactly at the
        /// same time. We select the time so it matches a suitable subdivision of the beat
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> CorrectNotesTimings(List<Note> notes)
        {  
            // We first copy all the notes to retObj, we will then remove and alter notes in rettObj, but the original notes are left unchanged
            var retObj = new List<Note>();
            notes.ForEach(n => retObj.Add(n.Clone()));
            retObj = retObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
            for (var i = 0; i < retObj.Count - 1; i++)
            {
                for (var j = i + 1; j < retObj.Count; j++)
                {
                    var ni = retObj[i];
                    var nj = retObj[j];
                    var tolerance = GetTolerance(ni, nj);
                    var dif = Math.Abs(ni.StartSinceBeginningOfSongInTicks - nj.StartSinceBeginningOfSongInTicks);
                    // If any of the notes is very short, then don't change the timings
                    if (ni.DurationInTicks < dif * 4 || nj.DurationInTicks < dif * 4) continue;
                    if (EventsAreAlmostSimultaneousButNotExactly(ni.StartSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks, tolerance))
                    {
                        var bestTime = GetMostAppropriateTime(ni.StartSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks);
                        ni.StartSinceBeginningOfSongInTicks = bestTime;
                        nj.StartSinceBeginningOfSongInTicks = bestTime;
                    }
                    if (EventsAreAlmostSimultaneousButNotExactly(ni.EndSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks, tolerance))
                    {
                        var bestTime = GetMostAppropriateTime(ni.EndSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks);
                        ni.EndSinceBeginningOfSongInTicks = bestTime;
                        nj.StartSinceBeginningOfSongInTicks = bestTime;
                    }
                }
            }
            return retObj;
        }
        /// <summary>
        /// When we want to clean the timing of notes, we have to consider the duration of the notes
        /// If 2 quarter notes start with a difference of 2 ticks, they are probably meant to start at the same time
        /// But if 2 thirtysecond notes or one thirtysecond and a quarter start with a difference of 2 ticks, it may
        /// be on purpose, it could be an embelishment for ex.
        /// So the amount of time tolerance to be used to decide if 2 notes are meant to be played together or not
        /// depends on the duration of the notes.
        /// This method returns the appropriate tolerance to be used when checking 2 notes
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static int GetTolerance(Note n, Note m)
        {
            var shorterNote = n.DurationInTicks < m.DurationInTicks ? n : m;
            return Math.Min(shorterNote.DurationInTicks / 4, 6);
        }

        /// <summary>
        /// When we clean the notes timing, we are interested in the events (note starts or note ends) that
        /// happen almost at the same time but not exactly at the same time
        /// This method tells if that is the case for 2 event times expressed in ticks
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private static bool EventsAreAlmostSimultaneousButNotExactly(long e1, long e2, int tolerance)
        {
            if (e1 == e2 || Math.Abs(e1 - e2) > tolerance) return false;
            return true;
        }
      
        /// <summary>
        /// When we are doing cleaning of notes timings, and we have for ex 2 notes that start almost together
        /// but not exactly at the same time, we change them so they start exactly in the same tick
        /// We have to decide wich tick to use. We select the most appropriate one as any between the 2 times
        /// that matches the bigger subdivision of the beat.
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        private static long GetMostAppropriateTime(long e1, long e2)
        {
            var divisor = 96;
            while (divisor > 1)
            {
                for (var i = Math.Min(e1, e2); i <= Math.Max(e1, e2); i++)
                {
                    if (i % divisor == 0) return i;
                    if (i % (divisor/3) == 0) return i;
                }
                divisor = divisor / 2;
            }
            return e1;
        }
        /// <summary>
        /// If 2 notes are not played at the same time, it returns the number of ticks between the end of the
        /// first and the start of the second
        /// Used to fix the starts and ends of notes to avoid having short rests between notes, that would
        /// make the display of the song confusing
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static long GetNumberOfTicksBetweenNotes(Note n, Note m)
        {
            if (GetIntersectionOfNotesInTicks(n, m) > 0) return 0;
            var firstNote = n.StartSinceBeginningOfSongInTicks < m.StartSinceBeginningOfSongInTicks ? n : m;
            var secondNote= n.StartSinceBeginningOfSongInTicks < m.StartSinceBeginningOfSongInTicks ? m : n;
            return secondNote.StartSinceBeginningOfSongInTicks - firstNote.EndSinceBeginningOfSongInTicks;
        }
        
        private static bool AreVoicesMonophonic(List<Note> notes, int tolerance = 0)
        {
            var voices = GetVoicesOfNotes(notes);
            foreach (var v in voices)
            {
                foreach (var n in notes.Where(x => x.Voice == v))
                {
                    if (n.StartSinceBeginningOfSongInTicks >= 1440)
                    {

                    }
                    foreach (var m in notes.Where(y => y.Voice == v && n.TempId != y.TempId))
                    {
                        if (m.StartSinceBeginningOfSongInTicks > 1440)
                        {

                        }
                        if (GetIntersectionOfNotesInTicks(m, n) == 0)  continue;
                        if (!AreNotesExactlySimultaneous(m,n)) return false;
                    }
                }
            }
            return true;
        }
        // Given a group of notes that may be played by several voices, returns all the different
        // voices found in the notes
        private static List<int> GetVoicesOfNotes(List<Note> notes)
        {
            var retObj = new List<int>();
            foreach (var v in notes.Select(n => n.Voice).Distinct())
                retObj.Add(v);
            return retObj;
        }

     
        // Returns true if the 2 voices start and finish at the same time (optionally with some tolerance)
        private static bool AreNotesExactlySimultaneous(Note m, Note n, int tolerance = 0)
        {
            return Math.Abs(m.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) <= tolerance &&
                            Math.Abs(m.EndSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks) <= tolerance; ;
        }
        // Returns the number of ticks that the song play both at the same time
        private static long GetIntersectionOfNotesInTicks(Note n, Note m)
        {
            var dif = Math.Min(m.EndSinceBeginningOfSongInTicks, n.EndSinceBeginningOfSongInTicks) -
                Math.Max(m.StartSinceBeginningOfSongInTicks, n.StartSinceBeginningOfSongInTicks);
            return dif > 0 ? dif : 0;
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
            var retObj = new List<Note>();
            // we make a copy of the notes parameter so we return a new object without modifying the parameter
            var notesCopy= new List<Note>();
            notes.ForEach(n => notesCopy.Add(n));
            
            // The variable newVoice is used to generate the numbers of the reordered voices
            byte newVoice = 0;
            // first split the notes by instrument
            var noteInstrument = notesCopy.Where(m => m.IsPercussion == false).GroupBy(n => n.Instrument).OrderBy(x => x);
            // now loop by instrument
            foreach (var noteInstGroup in noteInstrument)
            {
                // get notes of instrument
                var instrNotes = notesCopy.Where(n => n.Instrument == noteInstGroup.Key);
                // get the order of this instrument voices by pitch
                var orderedInstrVoices = OrderInstrumentVoicesByPitch(instrNotes);
                // loop on the instrument voices
                foreach (var voice in orderedInstrVoices)
                {
                    // add to the return object the notes of this voice, assigning a new voice number
                    foreach (var n in notesCopy.Where(m => m.Voice == voice))
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
            foreach(var n in notesCopy.Where(n => n.IsPercussion == true))
            {
                var m = n.Clone();
                m.Voice = newVoice;
                retObj.Add(m);
            }
            return retObj.OrderBy(n=>n.StartSinceBeginningOfSongInTicks).ToList();
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
        // Biven a group of notes (possibly from different voices) returns an average of their pitches
        // Used to order voices, with higher pitched voices first
        private static double getAveragePitchOfNOtes(IEnumerable<Note> notes)
        {
            return notes.Average(n => n.Pitch);
        }
        // Given a group of notes that may be played by different voices, returns the different
        // instruments played by the voices. There may be many voices but all play the same instrument
        // in which case it would return a list with only one element
        private static List<byte> GetInstrumentVoicesOfNotes(IEnumerable<Note> notes)
        {
            var instrumentsNotes = notes.Where(n => n.IsPercussion == false);
            return instrumentsNotes.Select(n => n.Voice).Distinct().ToList();
        }
        // Returns the numbers of the voices which consist of percusion notes
        // Voices that have percusion notes, have only percusion notes
        // Percusion notes and melodic notes are never mixed together in the same voice
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
                    if (!AreVoicesMonophonic(upperVoice))
                    {
                        var kiki = GetProblematicNotes(upperVoice);
                    }
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
            return retObj.OrderBy(n=>n.StartSinceBeginningOfSongInTicks).ToList();
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
            foreach (var n in notes)
            {
                var simulNotes = notes.Where(m =>
                m.TempId != n.TempId &&
                GetIntersectionOfNotesInTicks(m, n) > 0 &&
                !AreNotesExactlySimultaneous(m, n))
                .ToList();
                if (simulNotes.Where(m => m.Pitch > n.Pitch).ToList().Count == 0)
                {
                    retObj.Add(n);
                    // add also notes that start and end both at the same time
                    var chordNotes = notes.Where(m => AreNotesExactlySimultaneous(m, n) && m.TempId != n.TempId).ToList();
                    chordNotes.ForEach(x => retObj.Add(x));
                }
            }
            // Now correct the timings, so there are no small overlapping of consecutive notes or
            // very short rests between consecutive notes
            retObj = retObj.OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
            for (var i = 0; i < retObj.Count - 1; i++)
            {
                var n1 = retObj[i];
                var n2 = retObj[i + 1];
                // check overlappings
                if (GetIntersectionOfNotesInTicks(n1, n2) > 0)
                {
                    n1.EndSinceBeginningOfSongInTicks = n2.StartSinceBeginningOfSongInTicks;
                    continue;
                }
                // check short rests between notes
                var separation = n2.StartSinceBeginningOfSongInTicks - n1.EndSinceBeginningOfSongInTicks;
                var shorterNote = n1.DurationInTicks < n2.DurationInTicks ? n1 : n2;
                if (separation * 3 < shorterNote.DurationInTicks)
                    n1.EndSinceBeginningOfSongInTicks = n2.StartSinceBeginningOfSongInTicks;
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
                    TempId = Guid.NewGuid(),
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
