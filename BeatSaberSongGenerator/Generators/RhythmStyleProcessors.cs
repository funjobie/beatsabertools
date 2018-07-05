using BeatSaberSongGenerator.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongGenerator.Generators
{
    class RhythmStyleProcessorFactory
    {
        public enum RythmStyle
        {
            Regular, //one left, one right, one left, ...
            Chains, //3-5 notes from one side, then hand switch
            FeverTime, //one hand side gets notes meant for both, but in any direction
            DoublesSame, // both positions next to each other, in same direction
            SimpleObstacle, //one/two obstacles, otherwise regular
            Last //last enum value without meaning
        }

        public abstract class IRythmStyleProcessor
        {
            //how likely is it that for the next beat another style is chosen?
            public virtual double ChangeProbability() { return 0.1; }

            //should candidates be filtered for visibility (=same x/y position) before being provided?
            public virtual bool ApplyVisibilityFilter() { return true; }

            //how many seconds should the visibility filter use (if enabled)?
            public virtual double VisibilityFilterLength() { return 0.75; }

            //filter out candidates that do not fit the desired style
            public abstract void Filter(List<Note> leftCandidateNotes, List<Note> rightCandidateNotes, Note lastLeftNote, Note lastRightNote, float secondsSinceLastNote,
                out List<Note> filteredLeftCandidateNotes,
                out List<Note> filteredRightCandidateNotes);

            //choose which note to show now and which obstacles to add
            public abstract void Choose(List<Note> filteredLeftCandidateNotes, List<Note> filteredRightCandidateNotes, Note lastLeftNote, Note lastRightNote, 
                out Note nextLeftNote, 
                out Note nextRightNote, 
                out List<Note> additionalNotes,
                out List<Obstacle> obstacle);
        }


        public IRythmStyleProcessor GetNewStyle(RythmStyle rythmStyle, AudioMetadata audioMetadata)
        {
            if (rythmStyle == RythmStyle.Regular) return new RegularRythmStyleProcessor();
            if (rythmStyle == RythmStyle.Chains) return new ChainRythmStyleProcessor();
            if (rythmStyle == RythmStyle.FeverTime) return new FeverRythmStyleProcessor();
            if (rythmStyle == RythmStyle.DoublesSame) return new DoublesSameRythmStyleProcessor();
            if (rythmStyle == RythmStyle.SimpleObstacle) return new SimpleObstacleRythmStyleProcessor(audioMetadata);
            throw new ArgumentOutOfRangeException(nameof(rythmStyle));
        }

        private class RegularRythmStyleProcessor : IRythmStyleProcessor
        {
            private int counter = -1;
            private Random rand = new Random();
            public RegularRythmStyleProcessor()
            {
            }

            public override void Filter(List<Note> leftCandidateNotes, List<Note> rightCandidateNotes, Note lastLeftNote, Note lastRightNote, float secondsSinceLastNote, out List<Note> filteredLeftCandidateNotes, out List<Note> filteredRightCandidateNotes)
            {
                if (counter == -1) //never run before, so initalize to be a different side then last time (if they were not simultanious)
                {
                    if (lastLeftNote.Time < lastRightNote.Time) //right was last, therefore next should be left.
                        counter = 1;
                    else
                        counter = 0;
                }
                counter++;
                counter = counter % 2;
                if (counter % 2 == 0)
                {
                    filteredLeftCandidateNotes = leftCandidateNotes;
                    filteredRightCandidateNotes = new List<Note>();
                }
                else
                {
                    filteredLeftCandidateNotes = new List<Note>();
                    filteredRightCandidateNotes = rightCandidateNotes;
                }
            }

            public override void Choose(List<Note> filteredLeftCandidateNotes, List<Note> filteredRightCandidateNotes, Note lastLeftNote, Note lastRightNote, 
                out Note nextLeftNote, 
                out Note nextRightNote, 
                out List<Note> additionalNotes,
                out List<Obstacle> obstacle)
            {
                additionalNotes = null;
                obstacle = null;
                if (filteredLeftCandidateNotes.Count > 0)
                    nextLeftNote = filteredLeftCandidateNotes[rand.Next(0, filteredLeftCandidateNotes.Count)];
                else
                    nextLeftNote = null;
                if (filteredRightCandidateNotes.Count > 0)
                    nextRightNote = filteredRightCandidateNotes[rand.Next(0, filteredRightCandidateNotes.Count)];
                else
                    nextRightNote = null;
            }
        }

        private class ChainRythmStyleProcessor : IRythmStyleProcessor
        {
            private Random rand = new Random();
            private int chainTargetLength;
            private int currentChainLength = -1;
            private Hand hand;
            private bool halvedOutput = true;

            public ChainRythmStyleProcessor()
            {
                chainTargetLength = rand.Next(3, 6);
            }

            public override void Filter(List<Note> leftCandidateNotes, List<Note> rightCandidateNotes, Note lastLeftNote, Note lastRightNote, float secondsSinceLastNote, out List<Note> filteredLeftCandidateNotes, out List<Note> filteredRightCandidateNotes)
            {
                if (secondsSinceLastNote > 1) halvedOutput = false;
                else
                {
                    if (halvedOutput)
                    {
                        filteredLeftCandidateNotes = new List<Note>();
                        filteredRightCandidateNotes = new List<Note>();
                        halvedOutput = false;
                        return;
                    }
                    else
                    {
                        halvedOutput = true;
                    }
                }
                if (currentChainLength == -1) //never run before, so start the new chain with the hand that didn't do the last note (if they were not simultanious)
                {
                    if (lastLeftNote.Time < lastRightNote.Time) //right was last, therefore next should be left.
                        hand = Hand.Left;
                    else
                        hand = Hand.Right;
                }

                ++currentChainLength;
                if (currentChainLength == chainTargetLength)
                {
                    currentChainLength = 0;
                    if (hand == Hand.Left)
                        hand = Hand.Right;
                    else
                        hand = Hand.Left;
                }

                if (hand == Hand.Left)
                {
                    filteredLeftCandidateNotes = leftCandidateNotes;
                    filteredRightCandidateNotes = new List<Note>();
                }
                else
                {
                    filteredLeftCandidateNotes = new List<Note>();
                    filteredRightCandidateNotes = rightCandidateNotes;
                }
            }

            public override void Choose(List<Note> filteredLeftCandidateNotes, List<Note> filteredRightCandidateNotes, Note lastLeftNote, Note lastRightNote, 
                out Note nextLeftNote, 
                out Note nextRightNote,
                out List<Note> additionalNotes, 
                out List<Obstacle> obstacle)
            {
                additionalNotes = null;
                obstacle = null;
                if (filteredLeftCandidateNotes.Count > 0)
                    nextLeftNote = filteredLeftCandidateNotes[rand.Next(0, filteredLeftCandidateNotes.Count)];
                else
                    nextLeftNote = null;
                if (filteredRightCandidateNotes.Count > 0)
                    nextRightNote = filteredRightCandidateNotes[rand.Next(0, filteredRightCandidateNotes.Count)];
                else
                    nextRightNote = null;
            }
        }

        private class FeverRythmStyleProcessor : IRythmStyleProcessor
        {
            Random rand = new Random();
            private Hand hand;
            private bool halvedOutput = true;

            public FeverRythmStyleProcessor()
            {
                if (rand.Next(0, 2) == 0) hand = Hand.Left;
                else hand = Hand.Right;
            }

            public override double ChangeProbability()
            {
                return 0.2;
            }

            public override void Filter(List<Note> leftCandidateNotes, List<Note> rightCandidateNotes, Note lastLeftNote, Note lastRightNote, float secondsSinceLastNote, out List<Note> filteredLeftCandidateNotes, out List<Note> filteredRightCandidateNotes)
            {
                if (secondsSinceLastNote > 1) halvedOutput = false;
                else
                {
                    if (halvedOutput)
                    {
                        filteredLeftCandidateNotes = new List<Note>();
                        filteredRightCandidateNotes = new List<Note>();
                        halvedOutput = false;
                        return;
                    }
                    else
                    {
                        halvedOutput = true;
                    }
                }
                filteredLeftCandidateNotes = leftCandidateNotes;
                filteredRightCandidateNotes = rightCandidateNotes;
            }

            public override void Choose(List<Note> filteredLeftCandidateNotes, List<Note> filteredRightCandidateNotes, Note lastLeftNote, Note lastRightNote, 
                out Note nextLeftNote, 
                out Note nextRightNote,
                out List<Note> additionalNotes,
                out List<Obstacle> obstacle)
            {
                additionalNotes = null;
                obstacle = null;
                Note nextLeftNoteTmp = null;
                if (filteredLeftCandidateNotes.Count > 0)
                {
                    nextLeftNote = filteredLeftCandidateNotes[rand.Next(0, filteredLeftCandidateNotes.Count)];
                    nextLeftNote.Hand = hand;
                    nextLeftNote.CutDirection = CutDirection.Any;
                    nextLeftNoteTmp = nextLeftNote;
                }
                else
                    nextLeftNote = null;
                if (filteredRightCandidateNotes.Count > 0)
                {
                    //try finding a candidate that is close by so that the player can reach both in one swing
                    nextRightNote = filteredRightCandidateNotes.Find(x => (
                        Math.Abs((int)nextLeftNoteTmp.HorizontalPosition - (int)x.HorizontalPosition)
                      + Math.Abs((int)nextLeftNoteTmp.VerticalPosition - (int)x.VerticalPosition)
                      ) == 1);

                    if (nextRightNote == null) nextLeftNote = null;
                    else
                    {
                        nextRightNote.Hand = hand;
                        nextRightNote.CutDirection = CutDirection.Any;
                    }
                }
                else
                    nextRightNote = null;
            }
        }

        private class DoublesSameRythmStyleProcessor : IRythmStyleProcessor
        {
            private Random rand = new Random();
            private bool halvedOutput = true;

            public override void Filter(List<Note> leftCandidateNotes, List<Note> rightCandidateNotes, Note lastLeftNote, Note lastRightNote, float secondsSinceLastNote, out List<Note> filteredLeftCandidateNotes, out List<Note> filteredRightCandidateNotes)
            {
                if (secondsSinceLastNote > 1) halvedOutput = false;
                else
                {
                    if (halvedOutput)
                    {
                        filteredLeftCandidateNotes = new List<Note>();
                        filteredRightCandidateNotes = new List<Note>();
                        halvedOutput = false;
                        return;
                    }
                    else
                    {
                        halvedOutput = true;
                    }
                }
                filteredLeftCandidateNotes = leftCandidateNotes;
                filteredRightCandidateNotes = rightCandidateNotes;
            }

            public override void Choose(List<Note> filteredLeftCandidateNotes, List<Note> filteredRightCandidateNotes, Note lastLeftNote, Note lastRightNote, 
                out Note nextLeftNote, 
                out Note nextRightNote,
                out List<Note> additionalNotes, 
                out List<Obstacle> obstacle)
            {
                additionalNotes = null;
                obstacle = null;
                var tempLeftList = new List<Note>();
                tempLeftList.AddRange(filteredLeftCandidateNotes);
                var tempRightList = new List<Note>();
                tempRightList.AddRange(filteredRightCandidateNotes);
                var randomizedLeftList = new List<Note>();
                var randomizedRightList = new List<Note>();
                while (tempLeftList.Count > 0)
                {
                    Note note = tempLeftList[rand.Next(tempLeftList.Count)];
                    tempLeftList.Remove(note);
                    randomizedLeftList.Add(note);
                }
                while (tempRightList.Count > 0)
                {
                    Note note = tempRightList[rand.Next(tempRightList.Count)];
                    tempRightList.Remove(note);
                    randomizedRightList.Add(note);
                }
                foreach (var noteA in randomizedLeftList)
                {
                    foreach (var noteB in randomizedRightList)
                    {
                        if (noteA.CutDirection == noteB.CutDirection &&
                            !(noteA.HorizontalPosition == noteB.HorizontalPosition && noteA.VerticalPosition == noteB.VerticalPosition))
                        {
                            if (noteA.CutDirection == CutDirection.Up || noteA.CutDirection == CutDirection.Down)
                            {
                                if (noteA.VerticalPosition == noteB.VerticalPosition &&
                                    Math.Abs((int)noteA.HorizontalPosition - (int)noteB.HorizontalPosition) == 1)
                                {
                                    nextLeftNote = noteA;
                                    nextRightNote = noteB;
                                    return;
                                }
                            }
                            if (noteA.CutDirection == CutDirection.Right || noteA.CutDirection == CutDirection.Left)
                            {
                                if (noteA.HorizontalPosition == noteB.HorizontalPosition &&
                                    Math.Abs((int)noteA.VerticalPosition - (int)noteB.VerticalPosition) == 1)
                                {
                                    nextLeftNote = noteA;
                                    nextRightNote = noteB;
                                    return;
                                }
                            }
                        }
                    }
                }
                nextLeftNote = null;
                nextRightNote = null;
            }
        }

        private class SimpleObstacleRythmStyleProcessor : RegularRythmStyleProcessor
        {
            private bool started = false;
            private bool obstacleRegistered = false;
            private bool endReached = false;

            private AudioMetadata audioMetadata;
            private Random rand = new Random();
            private Obstacle obstacleA = null;
            private Obstacle obstacleB = null;
            private int obstaclePositions;

            public override double ChangeProbability()
            {
                if (endReached)
                    return 1.0;
                else
                    return 0.0;
            }

            public override bool ApplyVisibilityFilter()
            {
                return true;
            }

            public override double VisibilityFilterLength()
            {
                return 0.25;
            }

            public SimpleObstacleRythmStyleProcessor(AudioMetadata newAudioMetadata)
            {
                audioMetadata = newAudioMetadata;
                float targetLengthInSec = 3.0f + ((float)rand.NextDouble() * 5.0f); // in seconds
                float targetLength = targetLengthInSec * (float)audioMetadata.BeatDetectorResult.BeatsPerMinute / 60.0f; // in bpm
                obstaclePositions = rand.Next(0, 6);
                if (obstaclePositions == 0)
                {
                    obstacleA = new Obstacle();
                    obstacleA.HorizontalPosition = HorizontalPosition.Left;
                    obstacleA.Width = 1;
                    obstacleA.Type = ObstableType.WallFullHeight;
                }
                if (obstaclePositions == 1)
                {
                    obstacleA = new Obstacle();
                    obstacleA.HorizontalPosition = HorizontalPosition.Left;
                    obstacleA.Width = 2;
                    obstacleA.Type = ObstableType.WallFullHeight;
                }
                if (obstaclePositions == 2)
                {
                    obstacleA = new Obstacle();
                    obstacleA.HorizontalPosition = HorizontalPosition.Right;
                    obstacleA.Width = 1;
                    obstacleA.Type = ObstableType.WallFullHeight;
                }
                if (obstaclePositions == 3)
                {
                    obstacleA = new Obstacle();
                    obstacleA.HorizontalPosition = HorizontalPosition.CenterRight;
                    obstacleA.Width = 2;
                    obstacleA.Type = ObstableType.WallFullHeight;
                }
                if (obstaclePositions == 4)
                {
                    obstacleA = new Obstacle();
                    obstacleA.Type = ObstableType.WallHalfHeight;
                    obstacleA.Width = 4;
                }
                if (obstaclePositions == 5)
                {
                    obstacleA = new Obstacle();
                    obstacleA.HorizontalPosition = HorizontalPosition.Left;
                    obstacleA.Width = 1;
                    obstacleA.Type = ObstableType.WallFullHeight;

                    obstacleB = new Obstacle();
                    obstacleB.HorizontalPosition = HorizontalPosition.Right;
                    obstacleB.Width = 1;
                    obstacleB.Type = ObstableType.WallFullHeight;
                }
                obstacleA.Duration = targetLength;
                if (obstacleB != null)
                    obstacleB.Duration = targetLength;
            }

            public override void Filter(List<Note> leftCandidateNotes, List<Note> rightCandidateNotes, Note lastLeftNote, Note lastRightNote, float secondsSinceLastNote, out List<Note> filteredLeftCandidateNotes, out List<Note> filteredRightCandidateNotes)
            {
                if (!started)
                {
                    //leave a small gap to last note
                    if (secondsSinceLastNote < 0.5)
                    {
                        filteredLeftCandidateNotes = new List<Note>();
                        filteredRightCandidateNotes = new List<Note>();
                    }
                    else if (leftCandidateNotes.Count > 0)
                    {
                        started = true;
                        float startingTime = leftCandidateNotes[0].Time;
                        if (obstacleA != null)
                            obstacleA.Time = startingTime;
                        if (obstacleB != null)
                            obstacleB.Time = startingTime;
                    }
                }
                else
                {
                    if (leftCandidateNotes.Count > 0)
                    {
                        if (leftCandidateNotes[0].Time > obstacleA.Time + obstacleA.Duration)
                            endReached = true;
                    }
                }

                if (started && !endReached)
                {
                    base.Filter(leftCandidateNotes, rightCandidateNotes, lastLeftNote, lastRightNote, secondsSinceLastNote, out filteredLeftCandidateNotes, out filteredRightCandidateNotes);
                    //additionally filter out those within an obstacle

                    Predicate<Note> filter = null;
                    if (obstaclePositions == 0)
                        filter = new Predicate<Note>(x => x.HorizontalPosition == HorizontalPosition.Left
                            || (x.HorizontalPosition == HorizontalPosition.CenterLeft
                                && !(x.CutDirection == CutDirection.Up || x.CutDirection == CutDirection.Down || x.CutDirection == CutDirection.Any)));
                    else if (obstaclePositions == 1)
                        filter = new Predicate<Note>(x => x.HorizontalPosition == HorizontalPosition.Left || x.HorizontalPosition == HorizontalPosition.CenterLeft
                            || x.CutDirection == CutDirection.UpRight
                            || x.CutDirection == CutDirection.Right
                            || x.CutDirection == CutDirection.DownRight
                            || x.CutDirection == CutDirection.DownLeft
                            || x.CutDirection == CutDirection.Left
                            || x.CutDirection == CutDirection.UpLeft
                            || (x.HorizontalPosition == HorizontalPosition.CenterRight && x.Hand == Hand.Right)
                            || (x.HorizontalPosition == HorizontalPosition.Right && x.Hand == Hand.Left)
                        );
                    if (obstaclePositions == 2)
                        filter = new Predicate<Note>(x => x.HorizontalPosition == HorizontalPosition.Right
                            || (x.HorizontalPosition == HorizontalPosition.CenterRight
                                && !(x.CutDirection == CutDirection.Up || x.CutDirection == CutDirection.Down || x.CutDirection == CutDirection.Any)));
                    else if (obstaclePositions == 3)
                        filter = new Predicate<Note>(x => x.HorizontalPosition == HorizontalPosition.Right || x.HorizontalPosition == HorizontalPosition.CenterRight
                            || x.CutDirection == CutDirection.UpRight
                            || x.CutDirection == CutDirection.Right
                            || x.CutDirection == CutDirection.DownRight
                            || x.CutDirection == CutDirection.DownLeft
                            || x.CutDirection == CutDirection.Left
                            || x.CutDirection == CutDirection.UpLeft
                            || (x.HorizontalPosition == HorizontalPosition.Left && x.Hand == Hand.Right)
                            || (x.HorizontalPosition == HorizontalPosition.CenterLeft && x.Hand == Hand.Left)
                        );
                    else if (obstaclePositions == 4)
                        filter = new Predicate<Note>(x => x.VerticalPosition == VerticalPosition.Top
                            || x.CutDirection == CutDirection.Up
                            || x.CutDirection == CutDirection.UpRight
                            || x.CutDirection == CutDirection.DownRight
                            || x.CutDirection == CutDirection.Down
                            || x.CutDirection == CutDirection.DownLeft
                            || x.CutDirection == CutDirection.UpLeft);
                    else if (obstaclePositions == 5)
                        filter = new Predicate<Note>(x => x.HorizontalPosition == HorizontalPosition.Left || x.HorizontalPosition == HorizontalPosition.Right
                            || x.CutDirection == CutDirection.UpRight
                            || x.CutDirection == CutDirection.Right
                            || x.CutDirection == CutDirection.DownRight
                            || x.CutDirection == CutDirection.DownLeft
                            || x.CutDirection == CutDirection.Left
                            || x.CutDirection == CutDirection.UpLeft
                            || (x.HorizontalPosition == HorizontalPosition.CenterLeft && x.Hand == Hand.Right)
                            || (x.HorizontalPosition == HorizontalPosition.CenterRight && x.Hand == Hand.Left)
                            );

                    if (filteredLeftCandidateNotes != null)
                        filteredLeftCandidateNotes.RemoveAll(filter);
                    if (filteredRightCandidateNotes != null)
                        filteredRightCandidateNotes.RemoveAll(filter);
                }
                else
                {
                    filteredLeftCandidateNotes = new List<Note>();
                    filteredRightCandidateNotes = new List<Note>();
                }
            }

            public override void Choose(List<Note> filteredLeftCandidateNotes, List<Note> filteredRightCandidateNotes, Note lastLeftNote, Note lastRightNote, 
                out Note nextLeftNote, 
                out Note nextRightNote,
                out List<Note> additionalNotes, 
                out List<Obstacle> obstacle)
            {
                obstacle = null;
                if (started && !obstacleRegistered)
                {
                    obstacleRegistered = true;
                    obstacle = new List<Obstacle>();
                    if (obstacleA != null)
                        obstacle.Add(obstacleA);
                    if (obstacleB != null)
                        obstacle.Add(obstacleB);
                }
                base.Choose(filteredLeftCandidateNotes, filteredRightCandidateNotes, lastLeftNote, lastRightNote, out nextLeftNote, out nextRightNote, out additionalNotes, out var dummy);
            }
        }
    }
}
