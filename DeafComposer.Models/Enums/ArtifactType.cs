using System;

namespace DeafComposer.Models.Enums
{
    /// <summary>
    /// A pitch pattern representation looks like: 
    /// 0,-2,1,-2
    /// Each digit is a pitch step (the pitch difference with the previous note in semitones)
    /// A pattern of n notes would have n-1 integers, but we add a 0 at the beginning so
    /// the number of integers matches the number of pitches
    /// 
    /// A rythm pattern representation looks like:
    ///	1,2,4,2,4,2
    ///	Each digit is a relative duration
    ///	
    /// A Melody pattern representation looks like:
    /// 0-1,-2-1,-1-1,-2-1
    /// In this case, we have pairs of digits, the first being a pitch step and the second a duration
    /// 
    /// A "Full Chord" representation looks like this:
    /// 48,51,55,60  (this a C minor chord)
    /// Full Chords have exactly which notes were played
    /// 
    /// A Chord representation uses standard chords notation like
    /// C (C major)
    /// Am (A minor)
    /// Dm7 (D minor 7)
    /// Bb (B bemol)
    /// F#m (F sharp minor)
    /// 
    /// A Full Chord progression looks like this:
    /// 48-51-55-60,50-53-57-62 (this is a C major followed by D major
    /// 
    /// A Chord Progression uses standard chord notation:
    /// F,G,C
    /// Dm,G7,C
    /// 
    /// We use the ArtifactTypeId field to tell which type of artifact it is
    /// </summary>
    public enum ArtifactType : Byte
    {
        PitchPattern = 1,
        RythmPattern = 2,
        MelodyPattern = 3,
        FullChord = 4,
        Chord = 5,
        FullChordProgression = 6,
        ChordProgression = 7
    }
}
