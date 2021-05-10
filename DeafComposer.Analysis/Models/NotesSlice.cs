using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Models
{
    /// <summary>
    /// Represents the notes of some time interval
    /// </summary>
    public class NotesSlice
    {

        public NotesSlice(List<Note> notes, long startTick, long endTick, byte voice, Bar bar, long beatNumber)
        {
            Bar = bar;
            BeatNumberFromBarStart = beatNumber;
            var beatStart = bar.TicksFromBeginningOfSong + (beatNumber - 1) * bar.TimeSignature.Numerator * 96 / bar.TimeSignature.Denominator;
            StartTick = startTick;
            EndTick = endTick;
            Voice = voice;
            Notes = notes.Where(x => x.StartSinceBeginningOfSongInTicks >= startTick &&
                        x.StartSinceBeginningOfSongInTicks < EndTick &&
                        x.Voice == voice)
                .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z => z.Pitch)
                .ToList();
            RelativeNotes = new List<RelativeNote>();
            if (Notes.Count > 0)
                RelativeNotes.Add(new RelativeNote(Notes[0], null, beatStart, bar.KeySignature));
            for (var i = 1; i < Notes.Count; i++)
                RelativeNotes.Add(new RelativeNote(Notes[i], Notes[i - 1], beatStart, bar.KeySignature));
        }
        public long BarNumber
        {
            get
            {
                return Bar.BarNumber;
            }
        }
        public Bar Bar { get; set; }

        public long BeatNumberFromBarStart { get; set; }


        public byte Voice { get; set; }
        public long StartTick { get; set; }
        public long EndTick { get; set; }

        public int FirstPitch
        {
            get
            {
                if (Notes.Count == 0) return 0;
                return Notes[0].Pitch;
            }
        }

        public long Duration
        {
            get
            {
                return EndTick - StartTick;
            }
        }
        public List<Note> Notes { get; set; }

        public List<RelativeNote> RelativeNotes { get; set; }

    }
}
