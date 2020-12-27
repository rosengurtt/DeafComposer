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
