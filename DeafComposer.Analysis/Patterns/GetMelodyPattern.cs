using DeafComposer.Models.Entities;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeafComposer.Models.Helpers;
using Serilog;
using DeafComposer.Models.Graphs;
using DeafComposer.Models.Graphs.Nodes;
using DeafComposer.Analysis.Simplification;
using DeafComposer.Analysis.Models;

namespace DeafComposer.Analysis.Patterns
{
    public static class GetMelodyPattern
    {

        public static void GetPatternsOfSongSimplification(Song song, IDriver driver, int simplification = 1)
        {
           // IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
            var songNotes = song.SongSimplifications[simplification].Notes;
            var voices = songNotes.NonPercussionVoices();

            var simplifiedNotes = SimplificationUtilities.GetSimplifiedNotes(songNotes, song.Bars) ;

            var matches1 = PatternUtilities.GetMelodyMatchesWithDurationOfUpToNbeats(simplifiedNotes, song.Bars, 1);
            var matches2 = PatternUtilities.GetMelodyMatchesWithDurationOfUpToNbeats(simplifiedNotes, song.Bars, 2);
            var matches3 = PatternUtilities.GetMelodyMatchesWithDurationOfUpToNbeats(simplifiedNotes, song.Bars, 3);
            var matches4 = PatternUtilities.GetMelodyMatchesWithDurationOfUpToNbeats(simplifiedNotes, song.Bars, 4);
            var patterns = ExtractPatterns(matches1.Concat(matches2).Concat(matches4).ToList(), song.Id);
            var matrix = new PatternMatrix(patterns);
        }

        private static Dictionary<Models.MelodyPattern, List<Occurrence>> ExtractPatterns(List<MelodyMatch> matches, long SongId)
        {
            var retObj = new Dictionary<Models.MelodyPattern, List<Occurrence>>();
            foreach (var m in matches)
            {
                var patternito = new Models.MelodyPattern(m);
                if (!retObj.Keys.Where(x => x.AreEqual(patternito)).Any())
                {
                    retObj[patternito] = new List<Occurrence>();
                }                  
                var patternote = retObj.Keys.Where(x => x.AreEqual(patternito)).FirstOrDefault();
                retObj[patternote] = AddPatternOccurrence(retObj[patternote], patternote, m.Slice1, SongId);
                retObj[patternote] = AddPatternOccurrence(retObj[patternote], patternote, m.Slice2, SongId);

            }            
            retObj= RemovePatternsThatHappenLessThanNtimes(retObj, 4);
            retObj = RemovePatternsThatAreEqualToAnotherPatternPlusAstartingSilence(retObj);
            retObj = RemovePatternsThatOnlyHappenInsideAnotherPattern(retObj);
            return retObj;
        }

        private static Dictionary<Models.MelodyPattern, List<Occurrence>> RemovePatternsThatAreEqualToAnotherPatternPlusAstartingSilence(Dictionary<Models.MelodyPattern, List<Occurrence>> patterns)
        {
            var patternsToRemove = new List<Models.MelodyPattern>();
            foreach(var pat1 in patterns.Keys)
            {
                foreach (var pat2 in patterns.Keys)
                {
                    if (pat1.AsString == pat2.AsString && pat1.Duration > pat2.Duration)
                        patternsToRemove.Add(pat1);
                }
            }
            foreach(var pat in patternsToRemove)
            {
                patterns.Remove(pat);
            }
            return patterns;
        }
        private static Dictionary<Models.MelodyPattern, List<Occurrence>> RemovePatternsThatOnlyHappenInsideAnotherPattern(Dictionary<Models.MelodyPattern, List<Occurrence>> patterns)
        {
            var i = 0;
            var patternsToRemove = new List<Models.MelodyPattern>();
            foreach (var pat1 in patterns.Keys)
            {
                foreach (var pat2 in patterns.Keys)
                {
                    if (pat1 == pat2) continue;
                    if (IsPattern1PartOfPattern2(pat1, pat2) && patterns[pat1].Count <= patterns[pat2].Count + 3) { 
                        patternsToRemove.Add(pat1);
                        var soret = patterns[pat1];
                        var trolex = patterns[pat2];
                    }
                }
                i++;
            }
            foreach (var pat in patternsToRemove)
            {
                patterns.Remove(pat);
            }
            return patterns;
        }

        private static bool IsPattern1PartOfPattern2(Models.MelodyPattern pat1, Models.MelodyPattern pat2)
        {
            if (pat2.AsString.Contains(pat1.AsString)) return true;
            return false;
        }


        private static Dictionary<Models.MelodyPattern, List<Occurrence>> RemovePatternsThatHappenLessThanNtimes(Dictionary<Models.MelodyPattern, List<Occurrence>> patterns, int n)
        {
            var patternsToRemove = new List<Models.MelodyPattern>();
            foreach(var pato in patterns.Keys)
            {
                if (patterns[pato].Count < n)
                    patternsToRemove.Add(pato);
            }
            foreach (var pato in patternsToRemove)
            {
                patterns.Remove(pato);
            }
            return patterns;
        }

        private static List<Occurrence> AddPatternOccurrence(List<Occurrence> occurrences, Models.MelodyPattern pattern, NotesSlice slice, long SongId)
        {
            var occu = new Occurrence
            {
                BarNumber = slice.BarNumber,
                Beat = slice.BeatNumberFromBarStart,
                Voice = slice.Voice,
                SongId = SongId
            };
            if (!occurrences.Where(x => x.SongId==SongId&& x.BarNumber==slice.BarNumber&& x.Beat==slice.BeatNumberFromBarStart && x.Voice==slice.Voice).Any())
                occurrences.Add(occu);
            return occurrences;
        }


  
      
 }
}

