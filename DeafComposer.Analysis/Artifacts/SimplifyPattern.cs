using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Artifacts
{
    public static partial class ArtifactUtilities
    {
        // For durations only. Simplify cases like 17,15,16 to 1,1,1
        public static Artifact Simpl1(Artifact Artifact)
        {
            if (Artifact.ArtifactTypeId == ArtifactType.PitchPattern) return Artifact;

            List<int> RelativeDurations = Utilities.GetDurationsOfArtifactAsListOfIntegers(Artifact);

            if (RelativeDurations.Min() > 4 && (RelativeDurations.Max() - RelativeDurations.Min() <= 2))
            {
                return ChangeArtifactDurations(Artifact, new List<int> { 1 });
            }
            return Artifact;
        }
        // For durations only. Simplify cases like 16,3,3,3,3,6 to 5,1,1,1,1,2
        public static Artifact Simpl2(Artifact Artifact)
        {
            if (Artifact.ArtifactTypeId == ArtifactType.PitchPattern) return Artifact;

            List<int> RelativeDurations = Utilities.GetDurationsOfArtifactAsListOfIntegers(Artifact);

            var min = RelativeDurations.Min();
            var totalWithPerfectProportion = 0;
            foreach (var d in RelativeDurations)
            {
                if (d % min == 0) totalWithPerfectProportion++;
            }
            if (Artifact.Length - totalWithPerfectProportion == 1 ||
                totalWithPerfectProportion / (double)Artifact.Length > 0.8)
            {
                var newDurations = RelativeDurations
                    .Select(x => x % min == 0 ? x : (x / min) * min).ToList();
                return ChangeArtifactDurations(Artifact, newDurations);
            }
            return Artifact;
        }

        // For durations only. Divide by the maximum common divisor, 
        // so for ex 4,2,2 is converted to 2,1,1
        public static Artifact Simpl3(Artifact Artifact)
        {
            if (Artifact.ArtifactTypeId == ArtifactType.PitchPattern) return Artifact;

            List<int> RelativeDurations = Utilities.GetDurationsOfArtifactAsListOfIntegers(Artifact);

            int gcd = GCD(RelativeDurations.ToArray());
            var newDurations = RelativeDurations.Select(x => x / gcd).ToList();
            return ChangeArtifactDurations(Artifact, newDurations);

        }

        // If the Artifact itself consists of a repetitive Artifact, get the shortest Artifact
        // For ex instead of 2,1,2,1,2,1 we want just 2,1
        public static Artifact Simpl4(Artifact Artifact)
        {
            var elements = Artifact.AsString.Split(",");
            var quantElements = elements.Count();
            // We try with shortest possible subArtifact and increase by one until finding a 
            // subArtifact or not finding any, in which case we return the original Artifact
            for (int i = 1; i < quantElements / 2; i++)
            {
                // If i doesn't divide quantElements, then there is no subArtifact of lenght i
                if (quantElements % i != 0) continue;
                var slice = elements.Take(i).ToArray();
                // We use this variable as a flag that is initalized as true
                // If we find a case where the repetition of the slice doesn't happen
                // we set it to false
                var sliceIsRepeatedUntilTheEnd = true;
                for (var j = 1; j < quantElements / i; j++)
                {
                    for (var k = 0; k < i; k++)
                    {
                        if (slice[k] != elements[j * i + k])
                            sliceIsRepeatedUntilTheEnd = false;
                    }
                }
                // If the flag is still true, we found a Artifact of length i
                if (sliceIsRepeatedUntilTheEnd)
                {
                    return new Artifact
                    {
                        AsString = string.Join(",", slice),
                        ArtifactTypeId = Artifact.ArtifactTypeId
                    };
                }
            }
            return Artifact;
        }

        private static Artifact ChangeArtifactDurations(Artifact artifact, List<int> durations)
        {
            if (artifact.ArtifactTypeId == ArtifactType.RythmPattern)
                return new Artifact
                {
                    AsString = string.Join(",", durations),
                    ArtifactTypeId = ArtifactType.RythmPattern
                };
            if (artifact.ArtifactTypeId == ArtifactType.MelodyPattern)
            {
                var pitches = Utilities.GetPitchesOfArtifactAsListOfIntegers(artifact);
                var pitchesWithDurations = pitches.Zip(durations, (p, d) => $"({p}-{d})");
                return new Artifact
                {
                    AsString = string.Join(",", pitchesWithDurations),
                    ArtifactTypeId = ArtifactType.MelodyPattern
                };
            }
            return null;
        }

        public static List<int> GetShortestArtifact(List<int> numbers)
        {
            var divisors = GetDivisorsOfNumber(numbers.Count).OrderByDescending(x => x);
            foreach (int j in divisors)
            {
                int lengthOfGroup = (int)(numbers.Count / j);
                int i = 0;
                int n = 1;
                while (i + n * lengthOfGroup < numbers.Count)
                {
                    if (numbers[i] != numbers[i + n * lengthOfGroup]) break;
                    i++;
                    if (i == lengthOfGroup)
                    {
                        i = 0;
                        n++;
                    }
                }
                if (i + n * lengthOfGroup == numbers.Count)
                {
                    return numbers.Take(lengthOfGroup).ToList();
                }
            }
            return numbers;
        }
        private static IEnumerable<int> GetDivisorsOfNumber(int number)
        {
            for (int i = 1; i <= number; i++)
            {
                if (number % i == 0) yield return i;
            }
        }
        private static int GCD(int[] numbers)
        {
            return numbers.Aggregate(GCD);
        }

        private static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }

        public static Artifact SimplifyArtifact(Artifact Artifact)
        {

            return Simpl4(Simpl3(Simpl2(Simpl1(Artifact))));

        }
    }
}
