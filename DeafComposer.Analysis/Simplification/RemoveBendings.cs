using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Simplification
{
    /// <summary>
    /// When a note is bent from pitch a to picth b, we replace it by a note with
    /// pitch a and a note with pitch b.
    /// </summary>
    public static partial class SimplificationUtilities
    {
        public static List<Note> RemoveBendings(List<Note> notes)
        {
            int tolerance = 500;
            var addedNotes = new List<Note>();
            // When the pitch bend value is 8192*1.5 the note has raised 1 semitone,
            // when it is 16384 it has raised 2 semitones
            // We give it a tolerance of 500, so we create a new note when it reaches
            // 8192*1.5 - 500 or 16384 - 500
            foreach (var n in notes)
            {
                var noteWithoutBendings = new Note
                {
                    Instrument = n.Instrument,
                    IsPercussion = n.IsPercussion,
                    Pitch = n.Pitch,
                    StartSinceBeginningOfSongInTicks = n.StartSinceBeginningOfSongInTicks,
                    EndSinceBeginningOfSongInTicks = n.EndSinceBeginningOfSongInTicks,
                    Voice = n.Voice,
                    Volume = n.Volume
                };
                if (n.PitchBending != null && n.PitchBending.Count > 0)
                {
                    var sortedEvents = n.PitchBending.OrderBy(x => x.TicksSinceBeginningOfSong);
                    var currentLevel = 8192;
                    var startTick = n.StartSinceBeginningOfSongInTicks;
                    var keepLooping = true;
                    var isFirstLoop = true;
                    while (keepLooping)
                    {
                        // We find the next event where the pitch crosses or reaches one of the values
                        // 0, 4096, 8192, 12288, 16384 (with a tolerance of 500)
                        var nextPitchChange = sortedEvents
                                .Where(x => Math.Abs(x.Pitch - currentLevel) > (4096 - tolerance) &&
                                    x.TicksSinceBeginningOfSong > startTick).FirstOrDefault();

                        // If we don't find such an event, we are done
                        if (nextPitchChange == null)
                        {
                            keepLooping = false;
                            break;
                        }
                        // We update the currentLevel value
                        currentLevel = CalculateCurrentLevel(nextPitchChange.Pitch);

                        // We find the next event that crosses a boundary
                        var followingPitchChange = sortedEvents
                                .Where(x => Math.Abs(x.Pitch - currentLevel) > (4096 - tolerance) &&
                                    x.TicksSinceBeginningOfSong > nextPitchChange.TicksSinceBeginningOfSong).FirstOrDefault();
                        // We calculate the ending time of nextPitchChange as the start of the
                        // next crossing boundary event or the end of the note
                        var endTick = (followingPitchChange == null) ? n.EndSinceBeginningOfSongInTicks :
                                    followingPitchChange.TicksSinceBeginningOfSong;
                        // We add the amount of bending to the pitch
                        byte notePitch = (byte)(n.Pitch + Math.Round((currentLevel - 8192) / (double)4096));
                        var addedNote = new Note
                        {
                            Pitch = notePitch,
                            StartSinceBeginningOfSongInTicks = nextPitchChange.TicksSinceBeginningOfSong,
                            EndSinceBeginningOfSongInTicks = endTick,
                            Voice = n.Voice,
                            IsPercussion = n.IsPercussion,
                            Volume = n.Volume,
                            Instrument = n.Instrument,
                        };
                        // We set the value of startTick  for the next iteration
                        startTick = nextPitchChange.TicksSinceBeginningOfSong;
                        addedNotes.Add(addedNote);
                        // If we have added a note immediately after the original note, update
                        // the duration of the original note to finish where the new one starts
                        if (isFirstLoop)
                        {
                            n.EndSinceBeginningOfSongInTicks = addedNote.StartSinceBeginningOfSongInTicks;
                            isFirstLoop = false;
                        }
                    }
                }
            }
            foreach (var n in addedNotes)
            {
                notes.Add(n);
            }
            return notes;
        }
        private static int CalculateCurrentLevel(int number)
        {
            var tolerance = 500;
            if (number > 8192 + 4096 - tolerance && number < 16384 - tolerance)
                return 8192 + 4096;
            if (number > 16384 - tolerance) return 16384;
            if (number < 8192 + -4096 + tolerance && number > tolerance)
                return 4096;
            else return 0;
        }
    }
}
