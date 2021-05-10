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
            retObj= RemovePatternsThatHappenLessThanNtimes(retObj, 5);
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
                    if (IsPattern1PartOfPattern2(pat1, pat2) && patterns[pat1].Count <= patterns[pat2].Count + 3)
                        patternsToRemove.Add(pat1);
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


        /// <summary>
        /// Melodic patterns are modeled as a List<(int,int)> where the first int is the number of ticks to the next note and the second int is the 
        /// change in pitch from the current note
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static async Task GetPatternsOfSongSimplificationOld(Song song, IDriver driver, int simplification = 1)
        {
            IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
            var voices = song.SongSimplifications[simplification].Notes.NonPercussionVoices();

            await AddSongSimplificationAndVoicesNodes(session, song, 1);

            var candidates = GetPatternCandidates(song, simplification);

            foreach (var candidateGroup in candidates)
            {
                foreach (var pitchBeat in candidateGroup.Item1)
                    await AddPitchNodes(session, pitchBeat);
                //   await AddFollowedByEdges(session, candidateGroup.Item1);

                foreach (var rythmBeat in candidateGroup.Item2)
                    await AddRythmNodes(session, rythmBeat);
                //      await AddFollowedByEdges(session, candidateGroup.Item2);
            }

            //    await GetPitchPatterns(session);

            await session.CloseAsync();
        }

        private static List<(List<PitchPattern>, List<RythmPattern>)> GetPatternCandidates(Song song, int simplification = 1)
        {
            var retObj = new List<(List<PitchPattern>, List<RythmPattern>)>();
            for (var n=1; n < 6; n++)
            {
                var (pitchCandidates, rythmCandidates) = GetPatternCandidatesOfNBeats(song, simplification, n);
                if (pitchCandidates.Count > 0 || rythmCandidates.Count>0) retObj.Add((pitchCandidates, rythmCandidates));
            }
            return retObj;
        }

        /// <summary>
        /// Returns the first and end tick of the intervals of n beats
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        private static List<(long, long)> GetIntervalsOfNbeats(Song song, int n)
        {
            var retObj = new List<(long, long)>();
            (long, long) currentInterval = (0, 0);
            for (int barCounter = 0; barCounter < song.Bars.Count; barCounter++)
            {
                var thisBar = song.Bars[barCounter];
                // We are interested only in n that divide a bar in intervals with the same number of beats
                if (thisBar.TimeSignature.Numerator % n != 0) continue;

                long thisBarBeatTicks = 96 * 4 / thisBar.TimeSignature.Denominator;
                var endTickOfBar = thisBar.TicksFromBeginningOfSong + thisBarBeatTicks * thisBar.TimeSignature.Numerator;
                if (currentInterval.Item2 == 0)
                {
                    currentInterval.Item2 = thisBarBeatTicks * n;
                    retObj.Add(currentInterval);
                }
                var endOfPreviousInterval = currentInterval.Item2;

                while (endOfPreviousInterval < endTickOfBar)
                {
                    currentInterval = (endOfPreviousInterval, endOfPreviousInterval + thisBarBeatTicks * n);
                    endOfPreviousInterval = currentInterval.Item2;
                    retObj.Add(currentInterval);
                }
            }
            return retObj;
        }

        private static (List<PitchPattern>, List<RythmPattern>) GetPatternCandidatesOfNBeats(Song song, int simplification, int n)
        {
            var retObjPitch = new List<PitchPattern>();
            var retObjRythm = new List<RythmPattern>();
            var voices = song.SongSimplifications[simplification].Notes.NonPercussionVoices();

            foreach (var voice in voices)
            {
                PitchPattern currentPitchNode = null;
                PitchPattern nextPitchNode = null;
                RythmPattern currentRythmNode = null;
                RythmPattern nextRythmNode = null;
                var intervals = GetIntervalsOfNbeats(song, n);
                for (var i = 0; i < intervals.Count - 1; i++)
                {
                    var thisInterval = intervals[i];
                    var nextInterval = intervals[i + 1];
                    if (currentPitchNode == null)
                    {
                        currentPitchNode = GetPitchPatternCandidateForTicksInterval(thisInterval.Item1, thisInterval.Item2, song, voice);
                    }
                    if (currentRythmNode == null)
                    {
                        currentRythmNode = GetRythmBeatForTicksInterval(thisInterval.Item1, thisInterval.Item2, song, voice);
                    }

                    nextPitchNode = GetPitchPatternCandidateForTicksInterval(nextInterval.Item1, nextInterval.Item2, song, voice);
                    currentPitchNode.NextPitchPattern = nextPitchNode;
                    retObjPitch.Add(currentPitchNode);
                    currentPitchNode = nextPitchNode;

                    nextRythmNode = GetRythmBeatForTicksInterval(nextInterval.Item1, nextInterval.Item2, song, voice);
                    currentRythmNode.NextRythmPattern = nextRythmNode;
                    retObjRythm.Add(currentRythmNode);
                    currentRythmNode = nextRythmNode;
                    if (i == intervals.Count - 1)
                    {
                        retObjPitch.Add(currentPitchNode);
                        currentRythmNode = nextRythmNode;
                    }
                }

            }
            return (retObjPitch, retObjRythm);
        }


        /// <summary>
        /// Returns 1 note node for each note in the interval startTick-EndTick and 1 edge between 2 consecutive notes
        /// The first note has a RelativePitch of 0 and the number of ticks between the start of the interval and the start of the note
        /// The last note has no outgoing edge
        /// </summary>
        /// <param name="startTick"></param>
        /// <param name="endTick"></param>
        /// <param name="song"></param>
        /// <param name="voice"></param>
        /// <param name="simplification"></param>
        /// <returns></returns>
        private static RythmPattern GetRythmBeatForTicksInterval(long startTick, long endTick, Song song, int voice, int simplification = 1)
        {
            if (song.SongStats.NumberOfTicks < startTick) return null;
            var notes = song.SongSimplifications[simplification].Notes
                .Where(x => x.Voice == voice && x.StartSinceBeginningOfSongInTicks >= startTick && x.StartSinceBeginningOfSongInTicks < endTick)
                .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z => z.Pitch)
                .ToList();

            return new RythmPattern(notes, startTick, voice);
        }

        private static PitchPattern GetPitchPatternCandidateForTicksInterval(long startTick, long endTick, Song song, int voice, int simplification = 1)
        {
            if (song.SongStats.NumberOfTicks < startTick) return null;
            var notes = song.SongSimplifications[simplification].Notes
                .Where(x => x.Voice == voice && x.StartSinceBeginningOfSongInTicks >= startTick && x.StartSinceBeginningOfSongInTicks < endTick)
                .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z => z.Pitch)
                .ToList();

            var previousNotes = song.SongSimplifications[simplification].Notes
                .Where(x => x.Voice == voice && x.StartSinceBeginningOfSongInTicks < startTick)
                .OrderByDescending(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z => z.Pitch)
                .ToList();
            var previousNote = previousNotes.Count > 0 ? previousNotes[0] : null;

            return new PitchPattern(notes, startTick, voice, previousNote);
        }



        private async static Task AddSongSimplificationAndVoicesNodes(IAsyncSession session, Song song, int simplification = 1)
        {
            var voices = song.SongSimplifications[simplification].Notes.NonPercussionVoices();
            try
            {
                var id = $"{song.Id}_{simplification}";
                var command = @$" MERGE (s:Song {{SongId: {song.Id}, Name: '{song.Name}'}})-[h:HasSimplification]->(ss:SongSimplification {{Simplification: {simplification}}})";
                foreach (var v in voices)
                {
                    command += @$"
                            MERGE (ss)-[:HasVoice]->(:Voice {{Voice: {v}}})";
                }
                IResultCursor cursor = await session.RunAsync(command);
                await cursor.ConsumeAsync();
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception was raised trying to add a PatternFinder node");
            }
            return;
        }


        private async static Task AddPitchNodes(IAsyncSession session, PitchPattern pitchPattern)
        {
            // Check if PitchBeat alreay exists
            var matchPitchBeat = $@"MATCH (pp:PitchPattern {{Name: '{pitchPattern.Name}'}})
                                RETURN pp";

            var cursor = await session.RunAsync(matchPitchBeat);
            if (!await cursor.FetchAsync())
            {
                // It doesn't exist, create
                var insertPitchBeat = $@"CREATE (:PitchPattern {{Name: '{pitchPattern.Name}', QtyNotes: {pitchPattern.QtyNotes}, 
                                            Range: '{pitchPattern.Range}', TotalDeltaPitch: '{pitchPattern.TotalDeltaPitch}', IsMonotone: {pitchPattern.IsMonotone}}})";
                var currentStep = pitchPattern.FirstPitchStep;
                var isFirstStep = true;
                while (currentStep != null)
                {
                    if (isFirstStep) 
                        insertPitchBeat += $@"-[:ImplementedBy]->";
                    else
                        insertPitchBeat += $@"-[:FollowedBy]->";
                    insertPitchBeat += $@"(:PitchStep {{DeltaPitch: '{currentStep.DeltaPitch.ToString()}'}})" ;
                    currentStep = currentStep.NextPitchStep;
                }
                cursor = await session.RunAsync(insertPitchBeat);
                await cursor.ConsumeAsync();
            }
            var addOccurrenceNode = $@"MERGE (:Occurrence {{Tick: {pitchPattern.Tick}}}) 
                                        WITH 1 as neo4jSucks
                                        MATCH (pp:PitchPattern {{Name: '{pitchPattern.Name}'}}), (o:Occurrence {{Tick: {pitchPattern.Tick}}}) 
                                        CREATE (o)-[:Played]-> (pp)";
            cursor = await session.RunAsync(addOccurrenceNode);
            await cursor.ConsumeAsync();
        }
        private async static Task AddRythmNodes(IAsyncSession session, RythmPattern rythmPattern)
        {
            // Check if PitchBeat alreay exists
            var matchPitchPattern = $@"MATCH (rb:RythmPattern {{Name: '{rythmPattern.Name}'}})
                                RETURN rb";

            var cursor = await session.RunAsync(matchPitchPattern);
            if (!await cursor.FetchAsync())
            {
                // It doesn't exist, create
                var insertRythmPattern = $@"CREATE (:RythmPattern {{Name: '{rythmPattern.Name}', QtyNotes: {rythmPattern.QtyNotes}, IsUniform: {rythmPattern.IsUniform}}})";
                var currentStep = rythmPattern.FirstRythmStep;
                var isFirstStep = true;
                while (currentStep != null)
                {
                    if (isFirstStep)
                        insertRythmPattern += $@"-[:ImplementedBy]->";
                    else
                        insertRythmPattern += $@"-[:FollowedBy]->";
                    insertRythmPattern += $@"(:RythmStep {{DeltaTicks: {currentStep.DeltaTicks}, Volume: {(int)currentStep.Volume}}})";
                    currentStep = currentStep.NextRythmStep;
                }
                cursor = await session.RunAsync(insertRythmPattern);
                await cursor.ConsumeAsync();
            }
            var addOccurrenceNode = $@"MERGE (:Occurrence {{Tick: {rythmPattern.Tick}}}) 
                                        WITH 1 as neo4jSucks
                                        MATCH (rb:RythmPattern {{Name: '{rythmPattern.Name}'}}), (o:Occurrence {{Tick: {rythmPattern.Tick}}}) 
                                        CREATE (o)-[:Played]-> (rb)";
            cursor = await session.RunAsync(addOccurrenceNode);
            await cursor.ConsumeAsync();

        }

        private async static Task AddFollowedByEdges(IAsyncSession session, List<PitchPattern> pitchBeats)
        {
            foreach (PitchPattern pitchBeat in pitchBeats)
            {
                var addEdge = $@"MATCH (n1:PitchPattern {{Name: '{pitchBeat.Name}'}}), (n2:PitchPattern {{Name: '{pitchBeat.NextPitchPattern.Name}'}}) 
                                WITH n1, n2
                                CREATE (n1)-[:FollowedBy {{Tick: {pitchBeat.NextPitchPattern.Tick}, Voice: {pitchBeat.Voice}}}]->(n2)";
                var cursor = await session.RunAsync(addEdge);
                await cursor.ConsumeAsync();
            }
        }
        private async static Task AddFollowedByEdges(IAsyncSession session, List<RythmPattern> rythmBeats)
        {
            foreach (RythmPattern rythmBeat in rythmBeats)
            {
                var addEdge = $@"MATCH (n1:RythmPattern {{Name: '{rythmBeat.Name}'}}), (n2:RythmPattern {{Name: '{rythmBeat.NextRythmPattern.Name}'}}) 
                                WITH n1, n2
                                CREATE (n1)-[:FollowedBy {{Tick: {rythmBeat.NextRythmPattern.Tick}, Voice: {rythmBeat.Voice}}}]->(n2)";
                var cursor = await session.RunAsync(addEdge);
                await cursor.ConsumeAsync();
            }
        }

        private async static Task<List<PitchPattern>> GetPitchPatterns(IAsyncSession session)
        {
            // Remove PitchBeats that have less than 3 edges
            var deleteSingles = $@"match (n:PitchPattern)-[r]->() with n,count(r) as rel where rel < 3 detach delete(n)
                                    WITH 1 as neo4jSucks
                                    match (n:RythmPattern)-[r]->() with n,count(r) as rel where rel < 3 detach delete(n)";
            var cursor = await session.RunAsync(deleteSingles);
            await cursor.ConsumeAsync();

            // Get PitchBeats connected by 4 or more edges with the same direction
            //var getPairs = "match (m:PitchBeat)-[r]->(n:PitchBeat) with m, n, count(r) as rel where rel > 2 return m,n";

            //cursor = await session.RunAsync(getPairs);
            //var records = await cursor.ToListAsync();
            //await cursor.ConsumeAsync();
            //foreach (IRecord r in records)
            //{
            //    var firstNodeName = ((INode)r.Values["m"]).Properties["Name"];
            //    var secondNodeName = ((INode)r.Values["n"]).Properties["Name"];
            //    var pitchPattern = new PitchPattern {  }
            //}
            return null;
        }
        private async static Task AddBeatPattern(IAsyncSession session, BeatPattern beatPattern, long songSimplificationId, int voice, long tick)
        {
            try
            {
                var currentNode = beatPattern.FirstNote;
                string findChainCommand = "";
                string insertCommand = @$"MATCH (ss)
                                            WHERE ID(ss) = {songSimplificationId}
                                            WITH ss
                                            MERGE ";
                var isFirstNode = true;
                while (currentNode != null)
                {
                    findChainCommand += isFirstNode ?
                        @$"(ss)-[e]->(p:Pattern)-[:ConsistsOf]->(:Note {{TicksFromStart: {currentNode.TicksFromStart}}})" :
                        @$"(:Note {{TicksFromStart: {currentNode.TicksFromStart}}})";
                    insertCommand += isFirstNode ?
                          @$"(ss)-[:HasPattern {{Voice: {voice}, Tick: {tick}}}]->(p:Pattern {{Name:'{beatPattern.Name}'}})-[:ConsistsOf]->(:Note {{TicksFromStart: {currentNode.TicksFromStart}}})" :
                        @$"(:Note {{TicksFromStart: {currentNode.TicksFromStart}}})";
                    var nextNode = currentNode.Edge?.NextNote;
                    if (nextNode != null)
                    {

                        findChainCommand += $@"-[:MovesTo {{DeltaTicks:{currentNode.Edge.DeltaTicks}, DeltaPitch: '{currentNode.Edge.DeltaPitch.ToString()}'}}]->";
                        insertCommand += $@"-[:MovesTo {{DeltaTicks:{currentNode.Edge.DeltaTicks}, DeltaPitch: '{currentNode.Edge.DeltaPitch.ToString()}'}}]->";
                    }
                    currentNode = currentNode.Edge != null ? currentNode.Edge.NextNote : null;
                    isFirstNode = false;
                }
                findChainCommand = $"MATCH {findChainCommand} WHERE ID(ss) = {songSimplificationId} RETURN p";
                var cursor = await session.RunAsync(findChainCommand);
                if (await cursor.FetchAsync())
                {
                    var patternNode = cursor.Current.Values.FirstOrDefault().Value as INode;

                    await cursor.ConsumeAsync();
                    var updateCommand = @$"MATCH (p)
                                           WHERE ID(p) = {patternNode.Id}
                                           MATCH (ss)
                                           WHERE ID(ss) = {songSimplificationId}
                                           CREATE (ss)-[:HasPattern {{Voice: {voice}, Tick: {tick}}}]->(p)";
                    cursor = await session.RunAsync(updateCommand);
                    await cursor.ConsumeAsync();
                }
                else
                {
                    cursor = await session.RunAsync(insertCommand);
                    await cursor.ConsumeAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception was raised trying to add a Note node");
            }
        }
    }
}

