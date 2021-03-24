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

namespace DeafComposer.Analysis.Patterns
{
    public static class MelodyPattern
    {
        /// <summary>
        /// Melodic patterns are modeled as a List<(int,int)> where the first int is the number of ticks to the next note and the second int is the 
        /// change in pitch from the current note
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static async Task Get1beatPatternsOfSongSimplification(Song song, IDriver driver, int simplification = 1)
        {
            IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
            var voices = song.SongSimplifications[simplification].Notes.NonPercussionVoices();

            var songSimplificationId = await AddSongSimplificationNode(session, song, 1);

            foreach (var voice in voices)
            {
                foreach (var bar in song.Bars)
                {
                    for (var i = 0; i < bar.TimeSignature.Numerator; i++)
                    {
                        var beatTicks = 96 * 4 / bar.TimeSignature.Denominator;
                        var startTick = bar.TicksFromBeginningOfSong + i * beatTicks;
                        var endTick = startTick + beatTicks;
                        var beatPattern = GetBeatPatternForTicksInterval(startTick, endTick, song, voice);
                        if (beatPattern != null)
                            await AddBeatPattern(session, beatPattern, songSimplificationId, voice, startTick);
                    }
                }
            }
            await session.CloseAsync();
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
        private static BeatPattern GetBeatPatternForTicksInterval(long startTick, long endTick, Song song, int voice, int simplification = 1)
        {           
            if (song.SongStats.NumberOfTicks < startTick) return null;
            var notes = song.SongSimplifications[simplification].Notes
                .Where(x => x.Voice==voice && x.StartSinceBeginningOfSongInTicks >= startTick && x.StartSinceBeginningOfSongInTicks < endTick)
                .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z=>z.Pitch)
                .ToList();
            if (notes.Count == 0) return null;

            return new BeatPattern(notes);           
        }
        private async static Task<long> AddSongSimplificationNode(IAsyncSession session, Song song, int simplification = 1)
        {
            long IdToReturn = 0;
            try
            {
                var id = $"{song.Id}_{simplification}";
                var command = @$"
                    CREATE (ss:SongSimplification {{
                    SongId: {song.Id},
                    Name: '{song.Name}',
                    Simplification: {simplification}}})
                    RETURN ss";
                IResultCursor cursor = await session.RunAsync(command);
                if (await cursor.FetchAsync())
                {
                    // The pattern already exists, increment the Quantity property in the first node
                    var createdNode = cursor.Current.Values.FirstOrDefault().Value as INode;
                    IdToReturn = createdNode.Id;
                }
                await cursor.ConsumeAsync();
                return IdToReturn;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception was raised trying to add a PatternFinder node");
            }
            return 0;
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

