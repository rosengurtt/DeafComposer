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
        public static List<Note> GetNotesOfMidiFile(string base64encodedMidiFile)
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
  
            return notesObj;
        }
    }
}
