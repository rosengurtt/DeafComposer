using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {
        public static List<int> GetDurationsOfArtifactAsListOfIntegers(Artifact artifact)
        {

            if (artifact.ArtifactTypeId == ArtifactType.RythmPattern)
            {
                return artifact.AsString.Split(",").Select(x => int.Parse(x)).ToList();
            }
            if (artifact.ArtifactTypeId == ArtifactType.MelodyPattern)
                return artifact.AsString.Split(",")
                    .Select(x => { var y = x.Split("-"); return int.Parse(y[1].Replace(")", "")); }).ToList();
            return null;
        }
    }
}
