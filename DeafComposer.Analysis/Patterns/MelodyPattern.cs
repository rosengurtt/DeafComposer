using DeafComposer.Models.Entities;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeafComposer.Models.Helpers;
using Serilog;

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
        public static async Task<List<List<(int, int)>>> GetPatterns(Song song, IDriver driver, int simplification = 1)
        {
            IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
            var voices = song.SongSimplifications[simplification].Notes.NonPercussionVoices();

            foreach (var voice in voices)
            {
                var patternFinderId = $"{song.Id}_{simplification}_{voice}";
                var id = await AddPatternFinderNode(session, song, voice, 1);
                foreach (var bar in song.Bars)
                {
                    for (var i = 0; i < bar.TimeSignature.Numerator; i++)
                    {
                        var beatTicks = 96 * 4 / bar.TimeSignature.Denominator;
                        var startTick = bar.TicksFromBeginningOfSong + i * beatTicks;
                        var endTick = startTick + beatTicks;
                        var chain = GetGraphForTicksInterval(patternFinderId, startTick, endTick, song, voice);
                        await AddChain(session, chain, id);
                    }
                }
            }
            await session.CloseAsync();
            return null;
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
        private static noteNode GetGraphForTicksInterval(string patternFinderId, long startTick, long endTick, Song song, int voice, int simplification = 1)
        {           
            if (song.SongStats.NumberOfTicks < startTick) return null;
            var notes = song.SongSimplifications[simplification].Notes
                .Where(x => x.Voice==voice && x.StartSinceBeginningOfSongInTicks >= startTick && x.StartSinceBeginningOfSongInTicks < endTick)
                .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z=>z.Pitch)
                .ToList();
            if (notes.Count == 0) return null;
            noteNode currentNode = null;
            noteNode startNode = null;

            for (var i = 0; i < notes.Count; i++)
            {
                if (i == 0)
                {
                    currentNode = new noteNode
                    {
                        PatternFinderId = patternFinderId,
                        RelativePitch = 0,
                        TicksFromStart = (int)(notes[i].StartSinceBeginningOfSongInTicks - startTick),
                        PlaysWith = new List<noteNode>()
                    };
                    startNode = currentNode;
                }
                // find notes played together with currentNote
                var j = 0;
                while (i + j + 1 < notes.Count && notes[i].StartSinceBeginningOfSongInTicks == notes[i + j + 1].StartSinceBeginningOfSongInTicks) j++;
                // any notes between notes[i] and notes[i + j] start together with notes[i]
                // notes[i+j+1] is the first note that starts after notes[i] 

                // If there are notes starting together with notes[i] add playsWith relationships
                //for (var k = 0; k < j; k++)
                //{
                //    var node2 = new noteNode
                //    {
                //        PatternFinderId = patternFinderId,
                //        RelativePitch = ConvertFromSemitonesToScaleSteps(notes[i + k + 1].Pitch - notes[0].Pitch),
                //        TicksFromStart = (int)(notes[i + k + 1].StartSinceBeginningOfSongInTicks - startTick)
                //    };
                //    currentNode.PlaysWith.Add(node2);
                //}
                if (i + j + 1 < notes.Count)
                {
                    var nextNode = new noteNode
                    {
                        PatternFinderId = patternFinderId,
                        RelativePitch = ConvertFromSemitonesToScaleSteps(notes[i + j + 1].Pitch - notes[0].Pitch),
                        TicksFromStart = (int)(notes[i + j + 1].StartSinceBeginningOfSongInTicks - startTick),
                        PlaysWith = new List<noteNode>()
                    };
                    var edgi = new edge
                    {
                        PatternFinderId = patternFinderId,
                        DeltaPitch = ConvertFromSemitonesToScaleSteps(notes[i + j + 1].Pitch - notes[i].Pitch),
                        DeltaTicks = notes[i + j + 1].StartSinceBeginningOfSongInTicks - notes[i].StartSinceBeginningOfSongInTicks,
                        NextNote = nextNode
                    };
                    currentNode.Edge = edgi;
                    currentNode = nextNode;
                }

                i += j;
            }
            currentNode.Edge = null;
            return startNode;
        }
        private async static Task<long> AddPatternFinderNode(IAsyncSession session, Song song, int voice, int qtyBeats, int simplification = 1)
        {
            long IdToReturn = 0;
            try
            {
                var id = $"{song.Id}_{simplification}_{voice}";
                var command = @$"
                    CREATE (p:PatternFinder {{
                    PatternFinderId: '{id}', 
                    SongId: {song.Id},
                    Simplification: {simplification}, 
                    Voice: {voice}, 
                    QtyBeats: {qtyBeats}}})
                    RETURN p";
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
        private async static Task AddChain(IAsyncSession session, noteNode n1, long patternFinderId)
        {
            try
            {                
                var currentNode = n1;
                string findChainCommand = "";
                var isFirstNode = true;
                while (currentNode != null)
                {
                    findChainCommand += isFirstNode? 
                        @$"(n:Note {{TicksFromStart: {currentNode.TicksFromStart}}})": 
                        @$"(:Note {{TicksFromStart: {currentNode.TicksFromStart}}})";
                    var nextNode = currentNode.Edge?.NextNote;
                    if (nextNode != null)
                    {
                        findChainCommand += $@"-[:MovesTo {{DeltaTicks:{currentNode.Edge.DeltaTicks}, DeltaPitch: '{currentNode.Edge.DeltaPitch.ToString()}'}}]->";
                    }
                    currentNode = currentNode.Edge != null ? currentNode.Edge.NextNote : null;
                    isFirstNode = false;
                }
                var cursor = await session.RunAsync($"MATCH {findChainCommand} RETURN n");
                if (await cursor.FetchAsync())
                {
                    // The pattern already exists, increment the Quantity property in the first node
                    var startNode = cursor.Current.Values.FirstOrDefault().Value as INode;
                    long quant = (long)(startNode.Properties["Quantity"] as long?) + 1;
                    long nodeId = startNode.Id;
                    await cursor.ConsumeAsync();
                    var updateCommand = @$"MATCH (n)
                                           WHERE ID(n) = {nodeId}
                                           SET n.Quantity = {quant}";
                    cursor = await session.RunAsync(updateCommand);
                    await cursor.ConsumeAsync();
                }
                else
                {
                    findChainCommand = findChainCommand.Replace("(n:Note {", "(n: Note {Quantity:1, ");
                     var createCommand = @$"
                                            MATCH (p)
                                            WHERE ID(p) = {patternFinderId}
                                            WITH p
                                            MERGE (p)-[:ImplementedBy]->{findChainCommand}";
                    cursor = await session.RunAsync(createCommand);
                    await cursor.ConsumeAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception was raised trying to add a Note node");
            }
        }
        private static Interval ConvertFromSemitonesToScaleSteps(int input)
        {
           switch (input)
            {
                case 0:
                    return Interval.unison;
                case 1:
                case 2:
                    return Interval.secUp;
                case -1:
                case -2:
                    return Interval.secDown;
                case 3:
                case 4:
                    return Interval.thirdUp;
                case -3:
                case -4:
                    return Interval.thirdDown;
                case 5:
                    return Interval.fourthUp;
                case -5:
                    return Interval.fourthDown;
                case 6:
                    return Interval.tritoneUp;
                case -6:
                    return Interval.tritoneDown;
                case 7:
                    return Interval.fifthUp;
                case -7:
                    return Interval.fifthDown;
                case 8:
                case 9:
                    return Interval.sixthUp;
                case -8:
                case -9:
                    return Interval.sixthDown;
                case 10:
                case 11:
                    return Interval.sevUp;
                case 12:
                    return Interval.octUp;
                case -12:
                    return Interval.octDown;
                case 13:
                case 14:
                    return Interval.ninthUp;
                case -13:
                case -14:
                    return Interval.ninthDown;
                case 15:
                case 16:
                    return Interval.third2Up;
                case -15:
                case -16:
                    return Interval.third2Down;
                case 17:
                    return Interval.elevenUp;
                case -17:
                    return Interval.elevenDown;
                case 18:
                    return Interval.tritone2Up;
                case -18:
                    return Interval.tritone2Down;
                case 19:
                    return Interval.quinta2Up;
                case -19:
                    return Interval.fifth2Down;
                case 20:
                case 21:
                    return Interval.treceavaUp;
                case -20:
                case -21:
                    return Interval.treceavaDown;
                case 22:
                case 23:
                    return Interval.sev2Up;
                case -22:
                case -23:
                    return Interval.sev2Down;
                case 24:
                    return Interval.octUp;
                case -24:
                    return Interval.octDown;
            }
            return Interval.other;
        }
        public enum Interval
        {
            unison = 0,
            secUp = 2,
            thirdUp = 3,
            tritoneUp,
            fourthUp = 4,
            fifthUp = 5,
            sixthUp = 6,
            sevUp = 7,
            octUp = 8,
            ninthUp = 9,
            third2Up = 10,
            elevenUp = 11,
            tritone2Up,
            quinta2Up = 12,
            treceavaUp = 13,
            sev2Up = 14,
            oct2Up = 15,
            secDown = -2,
            thirdDown = -3,
            tritoneDown,
            fourthDown = -4,
            fifthDown =- 5,
            sixthDown = -6,
            sevDown = -7,
            octDown = -8,
            ninthDown = -9,
            third2Down = -10,
            elevenDown = -11,
            tritone2Down,
            fifth2Down = -12,
            treceavaDown = -13,
            sev2Down = -14,
            oct2Down = -15,
            other
        }

        public class noteNode
        {
            public string PatternFinderId  { get; set; }
            public Interval RelativePitch { get; set; }
            public int TicksFromStart { get; set; }
            public edge Edge { get; set; }
            public List<noteNode> PlaysWith { get; set; }
        }


        public class edge
        {
            public string PatternFinderId { get; set; }
            public long DeltaTicks { get; set; }
            public Interval DeltaPitch { get; set; }
            public noteNode NextNote { get; set; }
        }


        public class patterFinderNode
        {
            public string PatternFinderId { get; set; }
            public long SongId { get; set; }
            public int Simplification { get; set; }
            public int Voice { get; set; }
            public int QtyBeats { get; set; }
        }


    }
}

/*
Nodes
-----

PatternFinder - SongId, Simplification, Voice, QtyBeats
Note - RelativePitch, Ticks

Edges
-----

MovesTo - Ticks, PitchChange




*/
