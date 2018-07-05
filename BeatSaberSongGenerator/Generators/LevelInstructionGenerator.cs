using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberSongGenerator.AudioProcessing;
using BeatSaberSongGenerator.Objects;
using Commons;
using Commons.Extensions;
using Commons.Mathematics;

namespace BeatSaberSongGenerator.Generators
{
    public class LevelInstructionGenerator
    {
        private readonly SongGeneratorSettings settings;
        private readonly LightEffectGenerator lightEffectGenerator;
        private readonly BaseRhythmGeneratorCombinatory baseRhythmGenerator;

        public LevelInstructionGenerator(SongGeneratorSettings settings)
        {
            this.settings = settings;
            lightEffectGenerator = new LightEffectGenerator();
            baseRhythmGenerator = new BaseRhythmGeneratorCombinatory();
        }

        public LevelInstructions Generate(Difficulty difficulty, AudioMetadata audioMetadata)
        {
            var events = lightEffectGenerator.Generate(audioMetadata);
            var notes = GenerateModifiedBaseRhythm(difficulty, audioMetadata, out var obstacles);
            return new LevelInstructions
            {
                Version = "1.5.0",
                BeatsPerMinute = (float) audioMetadata.BeatDetectorResult.BeatsPerMinute,
                BeatsPerBar = 4, //meaning is unclear
                NoteJumpSpeed = 10,
                Shuffle = 0,
                ShufflePeriod = 0.5f,
                Events = events,
                Notes = notes,
                Obstacles = obstacles
            };
        }

        private IList<Note> GenerateModifiedBaseRhythm(Difficulty difficulty, AudioMetadata audioMetadata, out List<Obstacle> obstacles)
        {
            List<Beat> beats = audioMetadata.BeatDetectorResult.DetectedBeats;

            TimeSpan timeBetweenNotes = DetermineTimeBetweenNotes(difficulty);
            beats = FilterNotesByDifficulty(beats, timeBetweenNotes, audioMetadata.SampleRate);

            var notes = baseRhythmGenerator.Generate(beats, out obstacles, audioMetadata);
            return notes;
        }

        List<Beat> FilterNotesByDifficulty(List<Beat> originalBeats, TimeSpan timeBetweenNotes, int sampleRate)
        {
            List<Beat> beats = new List<Beat>();
            beats.AddRange(originalBeats);
            float secondsToMerge = (float)timeBetweenNotes.TotalSeconds;
            float beatIndexesToMerge = secondsToMerge * sampleRate;
            List<Beat> strengthFilteredBeats = new List<Beat>();
            while (beats.Count() > 0)
            {
                Beat strongestBeat = beats.MaximumItem(x => x.Strength);
                beats.Remove(strongestBeat);
                List<Beat> beatsToDelete = new List<Beat>();
                for (int i = 0; i < beats.Count(); ++i)
                {
                    if (Math.Abs(beats[i].SampleIndex - strongestBeat.SampleIndex) < beatIndexesToMerge)
                    {
                        strongestBeat.Strength += beats[i].Strength;
                        beatsToDelete.Add(beats[i]);
                    }
                }
                strengthFilteredBeats.Add(strongestBeat);
                for (int i = 0; i < beatsToDelete.Count(); ++i)
                    beats.Remove(beatsToDelete[i]);
            }
            strengthFilteredBeats.Sort((a, b) => (a.SampleIndex.CompareTo(b.SampleIndex)));
            return strengthFilteredBeats;
        }

        private TimeSpan DetermineTimeBetweenNotes(Difficulty difficulty)
        {
            double multiplier;
            switch (difficulty)
            {
                case Difficulty.Easy:
                    multiplier = 1.5;
                    break;
                case Difficulty.Normal:
                    multiplier = 1.0;
                    break;
                case Difficulty.Hard:
                    multiplier = 0.5;
                    break;
                case Difficulty.Expert:
                    multiplier = 0.3;
                    break;
                case Difficulty.ExpertPlus:
                    multiplier = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
            }
            return TimeSpan.FromSeconds(multiplier * (1-settings.SkillLevel));
        }
    }
}