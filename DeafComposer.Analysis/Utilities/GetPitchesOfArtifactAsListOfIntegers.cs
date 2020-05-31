using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {
        public static List<int> GetPitchesOfArtifactAsListOfIntegers(Artifact artifact)
        {

            if (artifact.ArtifactTypeId == ArtifactType.PitchPattern)
            {
                return artifact.AsString.Split(",").Select(x => int.Parse(x)).ToList();
            }
            if (artifact.ArtifactTypeId == ArtifactType.MelodyPattern)
                return artifact.AsString.Split(",")
                    .Select(x => { var y = x.Split("."); return int.Parse(y[0]); }).ToList();
            return null;
        }
    }
}