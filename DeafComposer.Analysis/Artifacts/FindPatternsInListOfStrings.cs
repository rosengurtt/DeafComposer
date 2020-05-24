using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Artifacts
{
    public static partial class ArtifactUtilities
    {
        /// <summary>
        /// Finds sequences of strings that appear more than once in the list of string
        /// The strings must not have a comma, because the comma is used as a separator when
        /// writing a sequence of strings as one string
        /// 
        /// We can use this method for looking patterns in lists of notes, if we express the notes
        /// as a couple of numbers separated by some separator that is not a comma. We use this notation
        /// for notes: 
        /// 
        /// (3-5)
        /// 
        /// where the first number (3) is the pitch and the second (5) the duration.
        /// 
        /// 
        /// The dictionary that returns has
        /// - in the keys the sequences found
        /// - in the values the locations in the list of numbers where the sequences starts
        /// 
        /// For example if the sequence ["1","4","1","5"] was found in the index 35 and 76 of of the list 'strings'
        /// the dictionary will have an entry with key=["1","4","1","5"] and value [35,76]
        /// </summary>
        /// <param name="strings">The strings may represent
        /// - pitches (ex: {"45", "57", "63"})
        /// - relative durations (ex: {"2", "4", "1", "1"})
        /// - a pitch and a duration (ex: {"(45-2)", "(57-4)"}</param>
        /// <param name="minLength">The minimum length of the sequences to find</param>
        /// <param name="maxLength">The maximum length of the sequences to find</param>
        /// <returns></returns>
        public static Dictionary<string, List<int>> FindPatternsInListOfStrings(List<string> strings, int minLength, int maxLength)
        {
            // This dictionary stores all the sequences of up to maxLength elements
            var sequences = new Dictionary<string, List<int>>();

            for (int i = 0; i < strings.Count - minLength; i++)
            {
                for (int j = minLength; j <= maxLength; j++)
                {
                    if (i + j >= strings.Count) break;
                    var seq = StringifyList(strings.GetRange(i, j));
                    if (!IsRepetitionOfaShorterPattern(seq))
                    {
                        if (!sequences.Keys.Contains(seq))
                            sequences[seq] = new List<int>();
                        sequences[seq].Add(i);
                    }
                }
            }
            sequences = RemoveSingletons(sequences);
            return RemoveSubsequences(sequences);
        }




        /// <summary>
        /// If we find the pattern 1,2,3,4 we will also find the patterns 1,2,3 and 2,3,4
        /// But if 1,2,3 and 2,3,4 never happer on their own, but always as part of 1,2,3,4
        /// then we remove them
        /// </summary>
        /// <param name="sequences"></param>
        /// <returns></returns>
        private static Dictionary<string, List<int>> RemoveSubsequences(Dictionary<string, List<int>> sequences)
        {
            var retObj = new Dictionary<string, List<int>>();
            var sortedKeys = sequences.Keys.OrderBy(x => x).ToList();
            for (int i = 0; i < sequences.Keys.Count - 1; i++)
            {
                if (!sortedKeys[i + 1].Contains(sortedKeys[i]) ||
                    sequences[sortedKeys[i]].Count > sequences[sortedKeys[i + 1]].Count)
                    retObj[sortedKeys[i]] = sequences[sortedKeys[i]];
            }
            return retObj;
        }

        private static Dictionary<string, List<int>> RemoveSingletons(Dictionary<string, List<int>> sequences)
        {
            var retObj = new Dictionary<string, List<int>>();
            foreach (string key in sequences.Keys)
                if (sequences[key].Count > 1)
                    retObj[key] = sequences[key];
            return retObj;
        }

        private static string StringifyList(List<string> numbers)
        {
            return String.Join(",", numbers);
        }

        // Tells if the pattern itself consists of a repetitive shorter pattern
        // For ex  2,1,2,1,2,1 is a repetirion of 2,1
        public static bool IsRepetitionOfaShorterPattern(string patternAsString)
        {
            try
            {
                var elements = patternAsString.Split(",");
                var quantElements = elements.Count();
                // We try with shortest possible subpattern and increase by one until finding a 
                // subpattern or not finding any, in which case we return the original pattern
                for (int i = 1; i <= (quantElements + 1) / 2; i++)
                {
                    // If i doesn't divide quantElements, then there is no subpattern of lenght i
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
                            {
                                sliceIsRepeatedUntilTheEnd = false;
                                break;
                            }
                            if (!sliceIsRepeatedUntilTheEnd) break;
                        }
                    }
                    // If the flag is still true, we found a pattern of length i
                    if (sliceIsRepeatedUntilTheEnd) return true;
                }
                return false;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
    }
}
