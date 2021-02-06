using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using System.Linq;
using DeafComposer.Models.Helpers;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {

  

        private static List<Note> SplitPolyphonicVoiceInMonophonicVoices(List<Note> notes)
        {
            var retObj = new List<Note>();
            var notesCopy = notes.Clone();
            // in voicesNotes we have the original notes separated by voice
            var voices = notesCopy.Select(n => n.Voice).Distinct().OrderBy(v => v);

            foreach (byte v in voices)
            {
                var voiceNotes = notesCopy.Where(n => n.Voice == v).OrderBy(m => m.StartSinceBeginningOfSongInTicks).ToList();
                // We initialize the subVoice value to an impossible number to indicate the notes that have not been assigned to a SubVoice
                byte impossibleValue = 255;
                voiceNotes.ForEach(n => n.SubVoice = impossibleValue);

                byte currentSubVoiceNumber = 0;
                // we keep looping while there are notes not assigned to a subVoice
                while (voiceNotes.Where(x => x.SubVoice == impossibleValue).Any())
                {
                    var notesNotAssignedToSubVoice = voiceNotes.Where(n => n.SubVoice == impossibleValue).ToList();
                    var totalDurationOfNotAssignedNotes = notesNotAssignedToSubVoice.Select(n => n.DurationInTicks).Sum();
                    var averageTotalDurationOfNotesPerVoice = voiceNotes.Where(n => n.SubVoice != impossibleValue).Select(y => y.DurationInTicks).Sum() / (currentSubVoiceNumber + 1);
                    // We don't want to split a voice in more than 4, so if we have already 4 subvoices, stop
                    if (currentSubVoiceNumber >= 4 ||
                        // If there are not many notes left, add them to the last voice added, don't create a new voice
                        notesNotAssignedToSubVoice.Count < 20 || 
                        // if the total playing time of the notes left is less than half of the average of total playing time per voice don't create new voice
                        totalDurationOfNotAssignedNotes < averageTotalDurationOfNotesPerVoice / 2)
                    {
                        var lastProcessedVoiceNumber = currentSubVoiceNumber == 0 ? 0 : currentSubVoiceNumber - 1;
                        // we assign the notes left to the last processed subvoice
                        notesNotAssignedToSubVoice.ForEach(n => n.SubVoice = (byte)lastProcessedVoiceNumber);

                        var lastVoice = voiceNotes.Where(n => n.SubVoice == lastProcessedVoiceNumber).ToList();
                        // Now correct the timings, so there are no small overlapping of consecutive notes or
                        // very short rests between consecutive notes
                        var notesToModify = GetNotesToModifyToRemoveOverlappingsAndGaps(lastVoice);
                        foreach (var n in notesToModify)
                        {
                            var nota = lastVoice.Where(m => m.Id == n.Id).FirstOrDefault();
                            nota.EndSinceBeginningOfSongInTicks = n.EndSinceBeginningOfSongInTicks;
                        }
                        var voiceNotesFixed = FixMissplacedNotes(voiceNotes);
                        retObj = retObj.Concat(voiceNotesFixed).ToList();
                        break;
                    }

                    var upperVoice = GetUpperVoice(notesNotAssignedToSubVoice);

                    // the start and ending of notes may have been modified when getting the upper voice
                    foreach (var n in upperVoice)
                    {
                        var noteAssigned = voiceNotes.Where(m => m.Id == n.Id).FirstOrDefault();
                        noteAssigned.SubVoice = currentSubVoiceNumber;
                        noteAssigned.StartSinceBeginningOfSongInTicks = n.StartSinceBeginningOfSongInTicks;
                        noteAssigned.EndSinceBeginningOfSongInTicks = n.EndSinceBeginningOfSongInTicks;
                    }
                    currentSubVoiceNumber++;
                }
           
            }
            retObj= ExpandSubVoicesToVoices(retObj);
            return retObj;
        }

        /// <summary>
        /// After we have splitted a voice in subvoices, we check for notes that we put in the wrong voice, considering its pitch
        /// and the pitches of other notes played aprox at the same time. This happens specially when we have 2 notes starting and ending
        /// at the same time and we put them both in the higher voice
        /// 
        /// The notes are expected to be all in the same voice, but they have different subvoices, so we reassing subvoices not voices
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> FixMissplacedNotes(List<Note> notas)
        {
            var retObj = notas.Clone().OrderBy(x => x.StartSinceBeginningOfSongInTicks).ThenByDescending(y => y.Pitch).ToList();
            var tolerance = 3;
            var noSubVoices = retObj.NonPercussionSubVoices().Count();

            // reassign notes by pitch
            foreach (var n in retObj)
            {
                var neighboors = retObj.Where(x => StartDifference(x, n) < 300).ToList();
                var subVoices = neighboors.NonPercussionSubVoices();
                var subVoicePitchAverage = new double[noSubVoices];
                foreach (var sv in subVoices)
                    subVoicePitchAverage[sv] = neighboors.Where(y => y.SubVoice == sv).Average(z => z.Pitch);
                foreach (var SV in subVoices)
                {
                    //if there are no neighboors in this voice, skip
                    if (subVoicePitchAverage[SV] == 0) continue;

                    var difInCurrentSubVoice = Math.Abs(n.Pitch - subVoicePitchAverage[n.SubVoice]);
                    var difInVoiceV = Math.Abs(n.Pitch - subVoicePitchAverage[SV]);
                    if (difInVoiceV + tolerance < difInCurrentSubVoice)
                        n.SubVoice = SV;
                }
            }

            // reassign notes when there are chords in an upper voice and holes in a lower voice
            foreach (var n in retObj)
            {
                if (n.StartSinceBeginningOfSongInTicks == 816)
                {

                }
                var chordNotes = retObj.Where(x => StartDifference(n, x) == 0 && EndDifference(n, x) == 0 && x.SubVoice==n.SubVoice && x.Pitch < n.Pitch).ToList();
                if (chordNotes.Count == 0) continue;
                byte nextSubVoice = (byte)(n.SubVoice + 1);
                var notesNextSubVoice = retObj.Where(x => x.SubVoice == nextSubVoice).ToList();
                if (!notesNextSubVoice.Where(x => GetIntersectionOfNotesInTicks(x, n) > Math.Min(x.DurationInTicks, n.DurationInTicks) / 2).Any())
                {
                    var noteToModify = chordNotes.OrderByDescending(x => x.Pitch).ToList()[0];
                    noteToModify.SubVoice = nextSubVoice;
                }

            }

            foreach (var subVoice in retObj.NonPercussionSubVoices())
            {
                var subVoiceNotes = retObj.Where(x => x.SubVoice == subVoice).ToList();
                var notesToModify = GetNotesToModifyToRemoveOverlappingsAndGaps(subVoiceNotes);
                foreach (var z in notesToModify)
                {
                    var nota = retObj.Where(m => m.Id == z.Id).FirstOrDefault();
                    nota.EndSinceBeginningOfSongInTicks = z.EndSinceBeginningOfSongInTicks;
                }
            }

            return retObj;
        }

        /// <summary>
        /// We do the splitting of polyphonic voices in 2 steps. In step 1 we create subvoices for the polyphonic voices, so notes
        /// stay in the same voice but are assigned to different subvoices. Then we call this function that will create a new set of
        /// voices from the subvoices
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> ExpandSubVoicesToVoices(List<Note> notes)
        {
            byte voiceNumber = 0;
            var retObj = new List<Note>();
                
            var voicesNotes = notes.GroupBy(n => n.Voice).OrderBy(x => x.Key).ToList();
            foreach(var notas in voicesNotes)
            {
                var notesOfVoice = notas.ToList();
                var voiceSubVoices = notesOfVoice.SubVoices();
                foreach(var s in voiceSubVoices)
                {
                    var subVoiceNotes = notesOfVoice.Where(y => y.SubVoice == s).ToList().Clone();
                    subVoiceNotes.ForEach(z => z.Voice = voiceNumber);
                    retObj = retObj.Concat(subVoiceNotes).ToList();
                    voiceNumber++;
                }
            }
            return retObj.OrderBy(n=>n.StartSinceBeginningOfSongInTicks).ToList();
        }


        /// <summary>
        /// Given a set of notes belonging to a track, where we may have different melodies playing at the 
        /// same time, it returns the upper voice
        /// We try to assign notes eagerly, so for ex. when different notes are played at the same time, but they start and stop at the same time, 
        /// we put them in the same voice
        /// This may be wrong, but we correct that later. 
        /// The notes not returned are notes played simultaneously with upper notes,
        /// that start and/or stop on different times of the upper notes  by a margin greater than 'tolerance'
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> GetUpperVoice(List<Note> notes)
        {
            var retObj = new List<Note>();
            foreach (var n in notes)
            {
                // Check if we have already added this note
                if (retObj.Where(x => x.Id == n.Id).Count() > 0) continue;

                // Get notes playing at the same time as this one, that don't start and end together
                // If the notes are played together less than 1/4 of the duration shortest note, then we consider that they don't overlap
                var simulNotes = notes.Where(m =>
                m.Id != n.Id &&
                GetIntersectionOfNotesInTicks(m, n) >= Math.Min(m.DurationInTicks, n.DurationInTicks) / 3 &&
                !DoNotesStartAndEndTogether(m, n))
                .ToList();
                // If there are no notes simulaneous to this with a higher pitch, then add it to the upper voice
                if (simulNotes.Where(m => m.Pitch > n.Pitch).ToList().Count == 0)
                {
                    retObj.Add(n);

                    // add also notes that start and end both at the same time
                    var chordNotes = notes.Where(m => m.Id != n.Id && DoNotesStartAndEndTogether(m, n)).ToList();
                    foreach (var chordNote in chordNotes)
                    {
                        // If it was not already added to retObj, add it
                        if (retObj.Where(x => x.Id == chordNote.Id).Count() == 0)
                            retObj.Add(chordNote);
                    }
                }
            }

            // Remove notes which pitch is too far from the average
            var notesToRemove = GetNotesThatAreTooLowForThisVoice(retObj, notes);
            notesToRemove.ForEach(x => retObj.Remove(retObj.Where(y => y.Id == x.Id).FirstOrDefault()));

            // Now correct the timings, so there are no small overlapping of consecutive notes or
            // very short rests between consecutive notes
            var notesToModify = GetNotesToModifyToRemoveOverlappingsAndGaps(retObj);
            foreach(var n in notesToModify)
            {
                var nota = retObj.Where(m => m.Id == n.Id).FirstOrDefault();
                nota.EndSinceBeginningOfSongInTicks = n.EndSinceBeginningOfSongInTicks;
            }
         
            return retObj;
        }

        /// <summary>
        /// When we have extracted a voice from a group of notes, we include notes that have minor overlappings
        /// The overlappings are problematic when creating the musical notation of the song, so we remove them complitely
        /// Also we remove small gaps between notes that would make the musical notation confusing
        /// It returns the notes that need to be changed
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> GetNotesToModifyToRemoveOverlappingsAndGaps(List<Note> notas)
        {
            var retObj = new List<Note>();
            var notes = notas.Clone().OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
            foreach (var n in notes)
            {
                // check overlappings
                var overlappingNotes = notes.Where(m => GetIntersectionOfNotesInTicks(m, n) > 0 && !DoNotesStartAndEndTogether(m, n)).ToList();

                if (overlappingNotes.Count > 0)
                {
                    // we first find if there are notes starting together with n (that will end on a different time)
                    var notesStartingTogether = overlappingNotes.Where(m => m.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks).ToList();
                    if (notesStartingTogether.Count > 0)
                    {
                        notesStartingTogether.Add(n);
                        // we found notes that start with at the same time as n but end on a different time. 
                        // We want to find the best time to end the notes and set them all to end at that time
                        // if there are notes after these notes, we don't want to overlap them, so we calculate maxPossibleEnd that is the maximum time to not overlap the next note
                        var nextNoteToStart = notes.Where(x => x.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks)
                                          .OrderBy(y => y.StartSinceBeginningOfSongInTicks).FirstOrDefault();
                        var maxPossibleEnd = nextNoteToStart != null ? nextNoteToStart.StartSinceBeginningOfSongInTicks : 99999999;
                        // we calculate the end of the note in the group that ends last
                        var maxEnd = notesStartingTogether.Max(x => x.EndSinceBeginningOfSongInTicks);
                        // we select the end of the longest note in the group or the beginning of the next note whichever is first
                        var bestEnd = Math.Min(maxEnd, maxPossibleEnd);
                        foreach (var p in notesStartingTogether)
                        {
                            p.EndSinceBeginningOfSongInTicks = bestEnd;
                            retObj.Add((p));
                        }
                    }
                    else
                    {
                        // there are no notes starting at the same time as n. So we end n when the next note starts to avoid the overlapping
                        var nextNote = notes.Where(x => x.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks)
                                          .OrderBy(y => y.StartSinceBeginningOfSongInTicks).FirstOrDefault();
                        n.EndSinceBeginningOfSongInTicks = nextNote.StartSinceBeginningOfSongInTicks;
                        retObj.Add((n));
                    }

                }

                // check short rests between notes
                var nextNota = notes.Where(x => x.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks)
                                         .OrderBy(y => y.StartSinceBeginningOfSongInTicks).FirstOrDefault();
                if (nextNota != null)
                {
                    var separation = nextNota.StartSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks;
                    var shorterNote = nextNota.DurationInTicks < n.DurationInTicks ? nextNota : n;
                    if (separation > 0 && separation * 2 < shorterNote.DurationInTicks)
                    {
                        var chordNotes = notes.Where(x => DoNotesStartAndEndTogether(x, n)).ToList();
                        foreach (var m in chordNotes)
                        {
                            m.EndSinceBeginningOfSongInTicks = nextNota.StartSinceBeginningOfSongInTicks;
                            retObj.Add(m);
                        }
                    }
                }
            }
            return retObj;
        }


  

      
        private static List<(Note, Note)> buscameLasNotasConflictivas(List<Note> notes)
        {
            var retObj = new List<(Note, Note)>();
            foreach (var n in notes)
            {
                var unison = notes.Where(m => GetIntersectionOfNotesInTicks(m, n) > 0 && m.Id != n.Id);
                if (unison.Count() > 0)
                {
                    foreach (var x in unison)
                        retObj.Add((n, x));
                }
            }
            return retObj;
        }
        private static List<(Note, Note)> buscameLasNotasPolyphonicas(List<Note> notes)
        {
            var retObj = new List<(Note, Note)>();
            foreach (var n in notes)
            {
                var unison = notes.Where(m => m.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks &&
                  m.EndSinceBeginningOfSongInTicks == n.EndSinceBeginningOfSongInTicks && m.Id != n.Id);
                if (unison.Count() > 0)
                {
                    foreach (var x in unison)
                        retObj.Add((n, x));
                }
            }
            return retObj;
        }

        private static List<(Note, Note)> DameLaDif(List<Note> notes1, List<Note> notes2)
        {
            var retObj = new List<(Note, Note)>();
            foreach (var n in notes1)
            {
                if (notes2.Where(m => m.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks &&
                m.EndSinceBeginningOfSongInTicks == n.EndSinceBeginningOfSongInTicks
                && m.Pitch == n.Pitch).Count() == 0)
                {
                    var x = notes2.Where(y => y.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks && y.Pitch == n.Pitch).FirstOrDefault();
                    retObj.Add((n, x));
                }
            }
            return retObj;
        }
     




   
        // When we extract the upper voice, we select at each tick the highest note of all the ones that are playing
        // at that time. But it could be that the upper voice is silent and the highest note playing belongs to another voice
        // In that case we "return" that note to the pool of notes 
        // We have to do this at the end and not while we are extracting the upper voice, because we need the average pitch
        // of the upper voice and the average pitch of the rest of the notes to decide if a note belongs by its pitch to the
        // upper voice or not
        // The function modifies the parameters sent to it and doesn't return a value. It basically removes some notes from
        // the upper voice and puts them in the pool of notes
        private static List<Note> GetNotesThatAreTooLowForThisVoice(List<Note> upperVoice, List<Note> poolOfNotes)
        {
            var notesToRemove = new List<Note>();

            // restOfNotes have the notes in poolOfNotes that are not in upperVoice
            var restOfNotes = new List<Note>();
            foreach (var n in poolOfNotes)
                if (upperVoice.Where(x => x.Id == n.Id).Count() == 0) restOfNotes.Add(n);

            if (restOfNotes.Count == 0) return notesToRemove;

            var tolerance = 7;
            foreach (var n in upperVoice)
            {
                var upperVoiceNeighbors = upperVoice
                    .Where(y => y.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks - 300 &&
                    y.StartSinceBeginningOfSongInTicks < n.StartSinceBeginningOfSongInTicks + 300).ToList();

                var restOfNotesNeighboors = restOfNotes
                    .Where(y => y.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks - 300 &&
                    y.StartSinceBeginningOfSongInTicks < n.StartSinceBeginningOfSongInTicks + 300).ToList();

                if (upperVoiceNeighbors.Count > 0 && restOfNotesNeighboors.Count > 0)
                {
                    var upperVoiceAveragePitch = upperVoiceNeighbors.Average(x => x.Pitch);
                    var restOfNotesAveragePitch = restOfNotesNeighboors.Average(x => x.Pitch);
                    var difWithUpperVoiceAverage = Math.Abs(n.Pitch - upperVoiceAveragePitch);
                    var difWithRestOfNotes = Math.Abs(n.Pitch - restOfNotesAveragePitch);
                    if (difWithUpperVoiceAverage - tolerance > difWithRestOfNotes)
                        notesToRemove.Add(n);
                }
            }
            return notesToRemove;
        }



    }
}
