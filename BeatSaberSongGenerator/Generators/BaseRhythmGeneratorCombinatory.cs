using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberSongGenerator.AudioProcessing;
using BeatSaberSongGenerator.Generators;
using BeatSaberSongGenerator.Objects;

namespace BeatSaberSongGenerator.Generators
{
    public class BaseRhythmGeneratorCombinatory
    {
        private Random rand = new Random();
        private NoteCandidatesStateMachine noteMachine = new NoteCandidatesStateMachine();
        private RhythmStyleProcessorFactory styleProcessorFactory = new RhythmStyleProcessorFactory();
        
        public IList<Note> Generate(List<Beat> beats, out List<Obstacle> obstacles, AudioMetadata audioMetadata)
        {

            var notes = new List<Note>();
            obstacles = new List<Obstacle>();

            var currentStyle = styleProcessorFactory.GetNewStyle(RhythmStyleProcessorFactory.RythmStyle.Regular, audioMetadata);
            var lastLeftNote = new Note(0.0f, Hand.Left, CutDirection.Down, HorizontalPosition.CenterLeft, VerticalPosition.Middle);
            var lastRightNote = new Note(0.0f, Hand.Right, CutDirection.Down, HorizontalPosition.CenterRight, VerticalPosition.Middle);
            var timeLast = 0.0f;

            for (int beatIndex = 0; beatIndex < beats.Count; ++beatIndex)
            {
                var timeNow = (float)(audioMetadata.BeatDetectorResult.BeatsPerMinute * (double)beats[beatIndex].SampleIndex / (double)audioMetadata.SampleRate / 60.0);

                //no notes in first 4 sec
                if (beats[beatIndex].SampleIndex / audioMetadata.SampleRate < 4)
                    continue;

                if (rand.NextDouble() < currentStyle.ChangeProbability())
                    currentStyle = styleProcessorFactory.GetNewStyle((RhythmStyleProcessorFactory.RythmStyle)rand.Next(0, (int)RhythmStyleProcessorFactory.RythmStyle.Last), audioMetadata);

                var leftCandidates = noteMachine.GetLeftCandidates(noteMachine.ToDirectionEnum(lastLeftNote.CutDirection), 
                    noteMachine.ToPositionEnum(lastLeftNote.HorizontalPosition, lastLeftNote.VerticalPosition));
                var rightCandidates = noteMachine.GetRightCandidates(noteMachine.ToDirectionEnum(lastRightNote.CutDirection), 
                    noteMachine.ToPositionEnum(lastRightNote.HorizontalPosition, lastRightNote.VerticalPosition));

                if (leftCandidates.CandidateStrings == null || rightCandidates.CandidateStrings == null)
                    throw new Exception("expected to have all candidate combinations filled");

                var leftCandidateNotes = ExpandCandidates(leftCandidates, timeNow, Hand.Left);
                var rightCandidateNotes = ExpandCandidates(rightCandidates, timeNow, Hand.Right);

                if(currentStyle.ApplyVisibilityFilter())
                {
                    leftCandidateNotes = VisibilityFilter(leftCandidateNotes, notes, timeNow, (float)(audioMetadata.BeatDetectorResult.BeatsPerMinute * currentStyle.VisibilityFilterLength() / 60.0));
                    rightCandidateNotes = VisibilityFilter(rightCandidateNotes, notes, timeNow, (float)(audioMetadata.BeatDetectorResult.BeatsPerMinute * currentStyle.VisibilityFilterLength() / 60.0));
                }

                currentStyle.Filter(leftCandidateNotes, rightCandidateNotes, lastLeftNote, lastRightNote, 
                    (timeNow - timeLast) * 60.0f / (float)(audioMetadata.BeatDetectorResult.BeatsPerMinute), 
                    out var filteredLeftCandidateNotes, out var filteredRightCandidateNotes);
                currentStyle.Choose(filteredLeftCandidateNotes, filteredRightCandidateNotes, lastLeftNote, lastRightNote, out var nextLeftNote, out var nextRightNote, out var additionalNotes, out var nextObstacles);

                if(nextLeftNote != null)
                {
                    notes.Add(nextLeftNote);
                    lastLeftNote = nextLeftNote;
                    timeLast = timeNow;
                }
                if(nextRightNote != null)
                {
                    notes.Add(nextRightNote);
                    lastRightNote = nextRightNote;
                    timeLast = timeNow;
                }
                if(additionalNotes != null)
                {
                    notes.AddRange(additionalNotes);
                }
                if(nextObstacles != null)
                {
                    obstacles.AddRange(nextObstacles);
                }
            }

            return notes;
        }

        private List<Note> VisibilityFilter(List<Note> candidateNotes, List<Note> notes, float timeNow, float restriction)
        {
            //filter out all candidates that have the same position as notes within the last x seconds
            var notesWithinRestriction = notes.FindAll(x => timeNow - x.Time < restriction);

            var filteredCandidates = new List<Note>();
            foreach(var candidate in candidateNotes)
            {
                var valid = true;
                foreach(var note in notesWithinRestriction)
                {
                    if (note.HorizontalPosition == candidate.HorizontalPosition && note.VerticalPosition == candidate.VerticalPosition)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                    filteredCandidates.Add(candidate);
            }
            return filteredCandidates;
        }

        private List<Note> ExpandCandidates(Candidates candidates, float timeNow, Hand hand)
        {
            var result = new List<Note>();
            if (candidates.CandidateStrings == null || candidates.CandidateStrings.Count() != 8) return result;
            for(int i = 0; i < 8; ++i)
            {
                var cutDirection = ToCutDirection(i, hand);
                var occurances = candidates.CandidateStrings[i];
                if (occurances.Count() != 12) throw new Exception("expected 12 numbers");
                for (int column = 0; column < 4; ++column)
                {
                    for (int row = 0; row < 3; ++row)
                    {
                        int digit;
                        if (int.TryParse(occurances.Substring(row * 4 + column, 1), out digit))
                        {
                            digit = ConvertPatternToFrequency(digit);
                            for (int d = 0; d < digit; ++d)
                            {
                                var newNote = new Note(timeNow, hand, cutDirection, (HorizontalPosition)column, ConvertRowToVerticalPosition(row));
                                result.Add(newNote);
                            }
                        }
                        else throw new Exception("did not find a digit in occurance string");
                    }
                }
            }
            return result;
        }

        private VerticalPosition ConvertRowToVerticalPosition(int row)
        {
            if (row == 0) return VerticalPosition.Top;
            if (row == 1) return VerticalPosition.Middle;
            if (row == 2) return VerticalPosition.Bottom;
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        private int ConvertPatternToFrequency(int digit)
        {
            //for now simply forwarding, so that 9 is 9 times more frequent than 1.
            //could be adjusted so that the scale is different
            return digit;
        }

        private CutDirection ToCutDirection(int i, Hand hand)
        {
            if (hand == Hand.Left)
            {
                switch (i)
                {
                    case 0: return CutDirection.Up;
                    case 1: return CutDirection.UpRight;
                    case 2: return CutDirection.Right;
                    case 3: return CutDirection.DownRight;
                    case 4: return CutDirection.Down;
                    case 5: return CutDirection.DownLeft;
                    case 6: return CutDirection.Left;
                    case 7: return CutDirection.UpLeft;
                }
            }
            else if(hand == Hand.Right)
            {
                switch (i)
                {
                    case 0: return CutDirection.Up;
                    case 1: return CutDirection.UpLeft;
                    case 2: return CutDirection.Left;
                    case 3: return CutDirection.DownLeft;
                    case 4: return CutDirection.Down;
                    case 5: return CutDirection.DownRight;
                    case 6: return CutDirection.Right;
                    case 7: return CutDirection.UpRight;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(i));
        }
    }
}