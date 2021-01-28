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

        // Given a group of notes that may be played by several voices, returns all the different
        // voices found in the notes
        private static List<int> GetVoicesOfNotes(List<Note> notes, bool includePercusionNotes = false)
        {
            var retObj = new List<int>();
            var notesToEvaluate = includePercusionNotes ? notes : notes.Where(x => x.IsPercussion == false);
            notesToEvaluate.Select(n => n.Voice).Distinct().ToList().ForEach(v => retObj.Add(v));
            return retObj;
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
            var voicesNotes = GetNotesAsDictionary(notes.Where(x => x.IsPercussion == false).ToList().Clone());
            // we will build this new dictionary that possibly will have more voices, but the same total of notes
            var newVoicesNotes = new Dictionary<byte, List<Note>>();
            byte voice = 0;
            foreach (byte v in voicesNotes.Keys)
            {
                // If voice has less than 20 notes, don't split it
                if (voicesNotes[v].Count < 20)
                {
                    var voiceNotes = new List<Note>(voicesNotes[v]);
                    RemoveOverlappingsInNotes(voiceNotes);
                    newVoicesNotes[voice++] = voiceNotes;
                }
                else
                {
                    // We keep in this variable the notes of this voice that we still haven't added to newVoicesNotes
                    // We initialize it with all the notes of the voice
                    var remainingNotes = new List<Note>(voicesNotes[v]);
                    // we store in this variable the total number of notes originally in voice v
                    var totalNotesInVoice_v = voicesNotes[v].Count;
                    var totalSplitsSoFar = 0;
                    List<Note> upperVoice;
                    while (remainingNotes.Count > 0)
                    {
                        // If we have been extracting voices from the current voice v and there are not many notes left, 
                        // add them to the last voice added, don't create a new voice
                        // notes.Count / voice is the average of notes per voice for this song (we add 1 to avoid division by 0)
                        if (totalSplitsSoFar >= 4 || remainingNotes.Count < totalNotesInVoice_v / 5 &&
                            remainingNotes.Count < notes.Count / (6 * (voice + 1)))
                        {
                            var lastVoice = newVoicesNotes[(byte)(voice - 1)];
                            remainingNotes.ForEach(n => lastVoice.Add(n));
                            RemoveOverlappingsInNotes(lastVoice);
                            break;
                        }
                        // The value 20 is empirical
                        if (GetProportionOfNotesInChords(remainingNotes) > 10)
                            upperVoice = GetUpperVoiceForTrackWithChords(remainingNotes);
                        else
                            upperVoice = GetUpperVoiceForTrackWithNoChords(remainingNotes);
                        newVoicesNotes[voice++] = upperVoice;
                        upperVoice.ForEach(x => remainingNotes.Remove(remainingNotes.Where(y=>y.Id==x.Id).FirstOrDefault()));
                        totalSplitsSoFar++;

                    }
                }
            }
            // We have now to reassing each note to the right voice
            foreach (var v in newVoicesNotes.Keys)
            {
                foreach (var n in newVoicesNotes[v])
                {
                    n.Voice = v;
                    retObj.Add(n);
                }
            }
            return retObj.OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
        }
        /// <summary>
        /// Given a set of notes belonging to a track, where we may have different melodies playing at the 
        /// same time, it returns the upper voice
        /// This track has chords, so when different notes are played at the same time, but they start and stop
        /// at the same time, we put them in the same voice
        /// The notes not returned are notes played simultaneously with upper notes,
        /// that start and/or stop on different times of the upper notes  by a margin greater than 'tolerance'
        /// The notes returned are not cloned, are the original notes passed in the "notes" parameter
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> GetUpperVoiceForTrackWithChords(List<Note> notes)
        {
            var retObj = new List<Note>();
            foreach (var n in notes)
            {
                // Check if we have already added this note
                if (retObj.Where(x => x.Id == n.Id).Count() > 0) continue;

                var simulNotes = notes.Where(m =>
                m.Id != n.Id &&
                GetIntersectionOfNotesInTicks(m, n) > 0 &&
                !AreNotesExactlySimultaneous(m, n))
                .ToList();
                if (simulNotes.Where(m => m.Pitch > n.Pitch).ToList().Count == 0)
                {
                    retObj.Add(n);

                    // add also notes that start and end both at the same time
                    var chordNotes = notes.Where(m => m.Id != n.Id && AreNotesExactlySimultaneous(m, n)).ToList();
                    foreach (var chordNote in chordNotes)
                    {
                        // If it was not already added to retObj, add it
                        if (retObj.Where(x => x.Id == chordNote.Id).Count() == 0)
                            retObj.Add(chordNote);
                    }
                }
            }
            // Now correct the timings, so there are no small overlapping of consecutive notes or
            // very short rests between consecutive notes
            retObj = retObj.OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
            for (var i = 0; i < retObj.Count - 1; i++)
            {
                var n1 = retObj[i];
                var n2 = retObj[i + 1];
                if (n2.StartSinceBeginningOfSongInTicks == n1.StartSinceBeginningOfSongInTicks) continue;
                var intersection = GetIntersectionOfNotesInTicks(n1, n2);
                // check overlappings
                if (intersection > 0 && intersection * 8 < (n1.DurationInTicks + n2.DurationInTicks))
                {
                    n1.EndSinceBeginningOfSongInTicks = n2.StartSinceBeginningOfSongInTicks;
                    continue;
                }
                // check short rests between notes
                var separation = n2.StartSinceBeginningOfSongInTicks - n1.EndSinceBeginningOfSongInTicks;
                var shorterNote = n1.DurationInTicks < n2.DurationInTicks ? n1 : n2;
                if (separation > 0 && separation * 3 < shorterNote.DurationInTicks)
                {
                    // There may be notes that are exactly simultaneous with the first one, we must change them all
                    // so they stay exactly simultaneous
                    var notesToChange = retObj.Where(x =>
                        x.StartSinceBeginningOfSongInTicks == n1.StartSinceBeginningOfSongInTicks &&
                        x.EndSinceBeginningOfSongInTicks == n1.EndSinceBeginningOfSongInTicks)
                        .ToList();
                    notesToChange.ForEach(x => x.EndSinceBeginningOfSongInTicks = n2.StartSinceBeginningOfSongInTicks);
                }
            }
            RemoveNotesThatAreTooLowForThisVoice(retObj, notes);

            return retObj;
        }


        /// <summary>
        /// Given a set of notes belonging to a track, where we may have different melodies playing at the 
        /// same time, it returns the upper voice. This method assumes that there are no chords being played, so
        /// if 2 different notes are played at the same time and they start and stop exactly in the same place,
        /// they are not part of a chord, they belong to different voices
        /// 
        /// The notes are not cloned. It returns the same note objects passed in the "notes" parameter
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> GetUpperVoiceForTrackWithNoChords(List<Note> notes)
        {
            var retObj = new List<Note>();
            foreach (var n in notes)
            {
                var maxTolerance = 8;
                // simulNotes are the notes played at the same time for a period of time that is more than
                // 1/43 of the shorter note or maxTolerance, whichever is smaller
                var simulNotes = notes.Where(m =>
                m.Id != n.Id &&
                GetIntersectionOfNotesInTicks(m, n) > Math.Min(maxTolerance, Math.Min(m.DurationInTicks, n.DurationInTicks) / 3))
                .ToList();
                if (simulNotes.Where(m => m.Pitch > n.Pitch).ToList().Count == 0)
                    retObj.Add(n);
            }
            // Fix duration of last note
            var lastNote = retObj[retObj.Count - 1];
            var maxVariation = lastNote.DurationInTicks / 4;
            var bestEndForLastNote = GetTickOfHighestImportanceInSegment(lastNote.EndSinceBeginningOfSongInTicks - maxVariation, lastNote.EndSinceBeginningOfSongInTicks + maxVariation);
            lastNote.EndSinceBeginningOfSongInTicks = bestEndForLastNote;

            RemoveNotesThatAreTooLowForThisVoice(retObj, notes);

            retObj = RemoveMinorOverlappingsAndSmallGapsBetweenConsecutiveNotesInVoiceWithNoChords(retObj);

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
        /// <summary>
        /// When we have a group of notes that are essentially sequencial with no overlappings, but there
        /// are some minor overlapping between them, we want to remove them completely
        /// If there are small gaps between consecutive notes, we fill that gap by extending the first, so
        /// we don't leave spurious rests that complicate the music notation display
        /// It is assumed that there are no chords, so there shouldn't be notes starting and ending both
        /// at the same time
        /// It modifies the notes passed in the notes parameter
        /// </summary>
        /// <param name="notes"></param>
        private static List<Note> RemoveMinorOverlappingsAndSmallGapsBetweenConsecutiveNotesInVoiceWithNoChords(List<Note> notes)
        {
            var retObj = notes.Clone();
            foreach (var n in retObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks))
            {
                // Remove overlappings
                var simulNote = retObj.Where(m =>
                      m.Id != n.Id && GetIntersectionOfNotesInTicks(m, n) > 0 &&
                      !AreNotesExactlySimultaneous(m,n))
                    .OrderBy(x => x.StartSinceBeginningOfSongInTicks)
                   .FirstOrDefault();

                if (simulNote != null)
                    n.EndSinceBeginningOfSongInTicks = simulNote.StartSinceBeginningOfSongInTicks;
           
                // Remove gaps
                var nextNote = retObj.Where(x => x.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks)
                    .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                    .FirstOrDefault();
                if (nextNote != null)
                {
                    var gap = GetGapBetweenNotes(n, nextNote);
                    if (gap > 0)
                        n.EndSinceBeginningOfSongInTicks = GetTickOfHighestImportanceInSegment(n.EndSinceBeginningOfSongInTicks, nextNote.StartSinceBeginningOfSongInTicks);
                }
            }
            return retObj;
        }


        /// <summary>
        /// If there is a period of time between the end of one note and the start of the other, it returns that
        /// period of time
        /// Otherwise, it returns 0
        /// </summary>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static long GetGapBetweenNotes(Note m, Note n)
        {
            var first = m.StartSinceBeginningOfSongInTicks <= n.StartSinceBeginningOfSongInTicks ? m : n;
            var second = m.StartSinceBeginningOfSongInTicks <= n.StartSinceBeginningOfSongInTicks ? n : m;
            if (m.EndSinceBeginningOfSongInTicks >= n.StartSinceBeginningOfSongInTicks) return 0;          
            return second.StartSinceBeginningOfSongInTicks - first.EndSinceBeginningOfSongInTicks;
        }

        // When we extract the upper voice, we select at each tick the highest note of all the ones that are playing
        // at that time. But it could be that the upper voice is silent and the highest note playing belongs to another voice
        // In that case we "return" that note to the pool of notes 
        // We have to do this at the end and not while we are extracting the upper voice, because we need the average pitch
        // of the upper voice and the average pitch of the rest of the notes to decide if a note belongs by its pitch to the
        // upper voice or not
        // The function modifies the parameters sent to it and doesn't return a value. It basically removes some notes from
        // the upper voice and puts them in the pool of notes
        private static void RemoveNotesThatAreTooLowForThisVoice(List<Note> upperVoice, List<Note> poolOfNotes)
        {
            var restOfNotes = new List<Note>();
            foreach (var n in poolOfNotes)
                if (upperVoice.Where(x => x.Id == n.Id).Count() == 0) restOfNotes.Add(n);

            if (restOfNotes.Count == 0) return;

            var notesToRemove = new List<Note>();
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
            notesToRemove.ForEach(n => upperVoice.Remove(n));
        }

        /// <summary>
        /// When we are splitting a polyphonic voice in multiple monophonic voices we have to consider the case when a
        /// voice is essentially playing chords. A voice may consist of 2 independent melodies, like a piano piece where the
        /// right hand and the left hand play independent melodies, in which case there can be ocasionally different notes
        /// starting and ending together, but the voice is not a succession of chords. Or it can be for ex. a guitar playing 
        /// chords, in which case all the notes played are part of chords and it doesn't make sense to split the voice in
        /// different voices, because there aren't independent melodies playing together
        /// 
        /// There is also the case where the left hand is playing chords and the right hand is playing a melody, in which
        /// case we have to split the voice in 2, one that is a melody and the other consisting of chords
        /// 
        /// This function returns the proportion (as percentage) of notes that are part of chords, so we can handle the 
        /// situation when we have chords being played
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static int GetProportionOfNotesInChords(List<Note> notes)
        {
            var notesInChords = new List<Note>();
            var remainingNotesToAnalyze = notes.Clone();
            var tolerance = 3;

            while (remainingNotesToAnalyze.Count > 0)
            {
                var n = remainingNotesToAnalyze[0];
                var simultaneousWithN = remainingNotesToAnalyze.Where(x =>
                 Math.Abs(x.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) < tolerance &&
                 Math.Abs(x.EndSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks) < tolerance).ToList();
                if (simultaneousWithN.Count() > 2)
                    simultaneousWithN.ForEach(x => { notesInChords.Add(x); remainingNotesToAnalyze.Remove(x); });
                else
                    remainingNotesToAnalyze.Remove(n);
            }
            return notesInChords.Count * 100 / notes.Count;
        }

        /// <summary>
        /// This is a bit drastic, it is used when we have a few notes to assign to a voice and we don't want to create
        /// a new voice to hold them. We remove all overlappings by making the notes exactly simultaneous or shortening
        /// some of them. We don't change start times
        /// </summary>
        /// <param name="notes"></param>
        private static void RemoveOverlappingsInNotes(List<Note> notes)
        {
            foreach (var n in notes.OrderBy(x => x.StartSinceBeginningOfSongInTicks))
            {
                var simulNotes = notes.Where(m =>
                   m.Id != n.Id &&
                   GetIntersectionOfNotesInTicks(m, n) > 0 &&
                   !AreNotesExactlySimultaneous(m, n))
                    .OrderBy(x => x.StartSinceBeginningOfSongInTicks)
                   .ToList();
                if (simulNotes.Count == 0) continue;
                // We first make all notes starting at the same time exactly simultaneous
                foreach (var y in simulNotes.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks))
                    MakeEndingsSimultaneous(n, y);
                // we now look for the first note that is not exactly simultaneous that starts later
                var nextSimul = simulNotes.Where(x => x.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks)
                    .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                    .FirstOrDefault();
                // If we find such a note, we make all the notes that started together with n to end when that note starts
                if (nextSimul != null)
                {
                    // if the start time difference is small change start times and end times
                    var difStart = nextSimul.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks;
                    var averageNoteDuration = (n.DurationInTicks + nextSimul.DurationInTicks) / 2;
                    if (difStart < Math.Min(24, averageNoteDuration / 4))
                    {
                        var bestStart = GetTickOfHighestImportanceInSegment(n.StartSinceBeginningOfSongInTicks, nextSimul.StartSinceBeginningOfSongInTicks);
                        var bestEnd = GetTickOfHighestImportanceInSegment(n.EndSinceBeginningOfSongInTicks, nextSimul.EndSinceBeginningOfSongInTicks);

                        // There may be other notes that are exactly simultaneous with simulNote or with n, we have to change them all
                        foreach (var m in simulNotes.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks
                        || x.StartSinceBeginningOfSongInTicks == nextSimul.StartSinceBeginningOfSongInTicks))
                        {
                            m.StartSinceBeginningOfSongInTicks = bestStart;
                            m.EndSinceBeginningOfSongInTicks = bestEnd;
                        }
                    }
                    else
                    {
                        foreach (var m in notes.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks))
                            m.EndSinceBeginningOfSongInTicks = nextSimul.StartSinceBeginningOfSongInTicks;
                    }
                }
            }
        }

        /// <summary>
        /// Used when we have 2 notes ending on slightly different times and we prefer to make them start exactly at the same time
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        private static void MakeEndingsSimultaneous(Note n, Note m)
        {
            var bestEnd = GetTickOfHighestImportanceInSegment(n.EndSinceBeginningOfSongInTicks, m.EndSinceBeginningOfSongInTicks);
            n.EndSinceBeginningOfSongInTicks = bestEnd;
            m.EndSinceBeginningOfSongInTicks = bestEnd;
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
            var notesCopy = notes.Clone();

            // The variable newVoice is used to generate the numbers of the reordered voices
            byte newVoice = 0;
            // first split the notes by instrument
            var noteInstrument = notesCopy.Where(m => m.IsPercussion == false).GroupBy(n => n.Instrument).OrderBy(x => x.Key).ToList();
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
                        var m = (Note)n.Clone();
                        m.Voice = newVoice;
                        retObj.Add(m);
                    }
                    // increment the voice number so the next group of notes will get the next available integer
                    newVoice++;
                }
            }
            // now add the percusion notes
            foreach (var n in notesCopy.Where(n => n.IsPercussion == true))
            {
                var m = (Note)n.Clone();
                m.Voice = newVoice;
                retObj.Add(m);
            }
            return retObj.OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
        }
   
        /// <summary>
        /// Returns the voice numbers ordered by the average pitch of their notes, higher averages first
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static IEnumerable<byte> OrderInstrumentVoicesByPitch(IEnumerable<Note> notes)
        {
            var voicesAveragePitches = new Dictionary<byte, double>();
            foreach (byte v in GetVoicesOfNotes(notes.ToList()))
            {
                voicesAveragePitches[v] = notes.Where(n => n.Voice == v).Average(x => x.Pitch);
            }
            var reorderedDic = voicesAveragePitches.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            return reorderedDic.Keys;
        }
    }
}
