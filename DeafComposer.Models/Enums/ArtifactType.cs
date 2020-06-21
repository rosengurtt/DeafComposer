using System;

namespace DeafComposer.Models.Enums

{
    /// <summary>
    /// Artifacts are music objects like harmony, rythm patterns, melodic patterns, etc
    /// 
    /// We use a special notation to represent this objects independently of what they are,
    /// that is a string that uses a comma separator.
    /// 
    /// A pitch pattern representation looks like: 
    /// 0,-2,1,-2
    /// Each digit is a pitch step (the pitch difference with the previous note in semitones)
    /// The first digit is 0 or the delta with the note playing before the pattern is played
    /// 
    /// A rythm pattern representation looks like:
    ///	1,2,4,2,4,2
    ///	Each digit is a relative duration
    ///	
    /// A Melody pattern representation looks like:
    /// 0.1,-2.1,-1.1,2.1
    /// In this case, we have pairs of digits, the first being a pitch step and the second a duration
    /// A dot is used for separation
    /// 
    /// A Chord representation looks like this:
    /// 48,51,55,60  (this a C minor chord)
    /// The numbers are pitches, they represent notes played at the same time at some point in the song
    /// 
    /// A Chord progression looks like this:
    /// 48.51.55.60,50.53.57.62 (this is a C major followed by D major)    //
    /// 
    /// We use the ArtifactTypeId field to tell which type of artifact it is
    /// 
    /// </summary>
    public enum ArtifactType : Byte
    {
        PitchPattern = 1,
        RythmPattern = 2,
        MelodyPattern = 3,
        Chord = 4,
        ChordProgression = 5
    }
}
