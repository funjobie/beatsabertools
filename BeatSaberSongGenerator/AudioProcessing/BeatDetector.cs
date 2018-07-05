using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberSongGenerator.Objects;
using Commons.DataProcessing;
using Commons.Extensions;
using Commons.Mathematics;
using Commons.Physics;
using NWaves.Transforms;
using NWaves.Windows;

namespace BeatSaberSongGenerator.AudioProcessing
{
    public class BeatDetectorResult
    {
        public BeatDetectorResult(
            double beatsPerMinute, 
            List<Beat> detectedBeats, 
            List<SongIntensity> songIntensities)
        {
            BeatsPerMinute = beatsPerMinute;
            DetectedBeats = detectedBeats;
            SongIntensities = songIntensities;
        }

        public double BeatsPerMinute { get; }
        public List<Beat> DetectedBeats { get; }
        public List<SongIntensity> SongIntensities { get; }
    }
    public class BeatDetector
    {
        public BeatDetector()
        {
        }

        /// <summary>
        /// Takes an audio signal, detects the beats and returns a list of all times (in seconds) where a beat is located
        /// </summary>
        /// <param name="signal">Expected to be normalized to -1 to 1</param>
        /// <param name="sampleRate">Sample rate of signal</param>
        /// <returns>Sample indices of beats</returns>
        public BeatDetectorResult DetectBeats(IList<float> signal, int sampleRate)
        {
            var stftWindowSize = 4096;
            var stepSize = 1024;
            
            var stft = new Stft(windowSize: stftWindowSize, hopSize: stepSize, window: WindowTypes.Hamming);
            var spectrogram = stft.Spectrogram(signal.ToArray());
            var windowPositions = SequenceGeneration
                .Linspace(stftWindowSize / 2.0, signal.Count-stftWindowSize/2.0, spectrogram.Count)
                .ToList();

            var focusedFrequency = DetermineFocusedFrequency(sampleRate, spectrogram, windowPositions);

            var beatCandidates = GetBeatCandidates(spectrogram, focusedFrequency, windowPositions);

            var strengthFilteredBeats = MergeBeatsByStrength(sampleRate, beatCandidates);

            var alignedBeats = MicroAlignBeats(signal, strengthFilteredBeats, stepSize);

            var songIntensity = GetSongIntensity(spectrogram, windowPositions, sampleRate, stepSize);
            var bpm = 60*strengthFilteredBeats.Count() / (signal.Count / (double)sampleRate);

            return new BeatDetectorResult(bpm, alignedBeats, songIntensity);
        }

        private List<int> DetermineFocusedFrequency(int sampleRate, List<float[]> spectrogram, List<double> windowPositions)
        {
            //Rather then taking the the beats across all frequencies, here one frequency is identified which is then focused on.
            //The focus is determined by how strong it varied across +- 0.5 seconds.
            //Although this often picks the wrong one (noisy fast over slower occasional ones) it serves as a base line for aligned beats
            //(a kind of lowest common denominator)
            //if nothing is found, it is saved as -1.
            var focusedFrequencies = new List<int>();
            var secondsToConsiderMostImportant = 0.5f;
            var beatIndexesToConsiderMostImportant = secondsToConsiderMostImportant * sampleRate;
            for (var timeIndex = 0; timeIndex < spectrogram.Count(); ++timeIndex)
            {
                var startTimeIndex = windowPositions.FindIndex(x => windowPositions[timeIndex] - x < beatIndexesToConsiderMostImportant);
                var endTimeIndex = windowPositions.FindLastIndex(x => x - windowPositions[timeIndex] < beatIndexesToConsiderMostImportant);
                var currentFrequencyMax = 0.0;
                var currentMaxFrequencyIndex = -1;
                for (var frequency = 0; frequency < spectrogram[0].Count(); ++frequency)
                {
                    var currentFrequencyStrength = 0.0;
                    float currentValue = spectrogram[startTimeIndex][frequency];
                    for (var i = startTimeIndex; i <= endTimeIndex; ++i)
                    {
                        var newValue = spectrogram[i][frequency];
                        currentFrequencyStrength += Math.Abs(newValue - currentValue);
                        currentValue = newValue;
                    }
                    if (currentFrequencyStrength > currentFrequencyMax)
                    {
                        currentMaxFrequencyIndex = frequency;
                        currentFrequencyMax = currentFrequencyStrength;
                    }
                }
                focusedFrequencies.Add(currentMaxFrequencyIndex);
            }
            return focusedFrequencies;
        }

        private List<Beat> GetBeatCandidates(List<float[]> spectrogram, List<int> focusedFrequency, List<double> windowPositions)
        {
            //go through the spectogram and identify beats, which are points where the strength increases a lot over a short time
            var beatCandidates = new List<Beat>();
            var minimumIntensity = 0.75f;
            var requiredDelta = 0.75f;
            for (int timeIndex = 1; timeIndex < spectrogram.Count() - 1; ++timeIndex)
            {
                var frequency = focusedFrequency[timeIndex];
                if (frequency == -1) continue;

                var intensityNow = spectrogram[timeIndex][frequency];
                if (intensityNow > minimumIntensity)
                {
                    var intensityBefore = spectrogram[timeIndex - 1][frequency];
                    if (intensityNow > intensityBefore + requiredDelta)
                    {
                        var candidate = new Beat();
                        candidate.SampleIndex = (int)windowPositions[timeIndex];

                        //don't take just this frequency's strength. rather take the strength of all of them.
                        //this ensures that 'better' beats are preferred.
                        //example: focused is a noisy frequency that has a beat each 0.1 seconds.
                        //however every 0.5 second is a more powerfull, actual beat present (which is visible across many frequencies).
                        //this one would have then a higher strength, which allows the strength filtering to prefer it over the other ones.
                        for (int frequencyIndex2 = 0; frequencyIndex2 < spectrogram[0].Count(); ++frequencyIndex2)
                        {
                            var diffOfFreq2 = spectrogram[timeIndex][frequencyIndex2] - spectrogram[timeIndex - 1][frequencyIndex2];
                            if (diffOfFreq2 > 0)
                                candidate.Strength += diffOfFreq2;
                        }

                        beatCandidates.Add(candidate);
                    }
                }
            }
            return beatCandidates;
        }

        private List<Beat> MergeBeatsByStrength(int sampleRate, List<Beat> beatCandidates)
        {
            //due to the focus on very noisy frequencies, there are a lot of beat candidates remaining.
            //in fast sections this could still be ~20 beats per second.
            //instead of reporting all of them, merge the beats via their strength:
            //stronger beats absorb their surrounding weak beats
            //that way, the important beats remain.
            var secondsToMerge = 0.125f;
            var beatIndexesToMerge = secondsToMerge * sampleRate;
            var strengthFilteredBeats = new List<Beat>();
            while (beatCandidates.Count() > 0)
            {
                var strongestBeat = beatCandidates.MaximumItem(x => x.Strength);
                beatCandidates.Remove(strongestBeat);
                var beatsToDelete = new List<Beat>();
                for (int i = 0; i < beatCandidates.Count(); ++i)
                {
                    if (Math.Abs(beatCandidates[i].SampleIndex - strongestBeat.SampleIndex) < beatIndexesToMerge)
                    {
                        strongestBeat.Strength += beatCandidates[i].Strength;
                        beatsToDelete.Add(beatCandidates[i]);
                    }
                }
                strengthFilteredBeats.Add(strongestBeat);
                for (int i = 0; i < beatsToDelete.Count(); ++i)
                    beatCandidates.Remove(beatsToDelete[i]);
            }

            strengthFilteredBeats.Sort((a, b) => (a.SampleIndex.CompareTo(b.SampleIndex)));
            return strengthFilteredBeats;
        }

        private List<Beat> MicroAlignBeats(IList<float> signal, List<Beat> strengthFilteredBeats, int stepSizeOfOriginalSearch)
        {
            //original found beats are detected with a window size of 1024, resulting in a precision of 1024/44100=23ms
            //this is sometimes big enough to feel being 'off-beat'
            //perform a more precise search with smaller window to find the best place to put the beat
            //128/44100=3ms
            var alignedBeats = new List<Beat>();

            var stftWindowSize = 1024; //not sure about this number
            var stepSize = 128;

            var stft = new Stft(windowSize: stftWindowSize, hopSize: stepSize, window: WindowTypes.Hamming);
            var spectrogram = stft.Spectrogram(signal.ToArray());
            var windowPositions = SequenceGeneration
                .Linspace(stftWindowSize / 2.0, signal.Count - stftWindowSize / 2.0, spectrogram.Count)
                .ToList();

            foreach(var beat in strengthFilteredBeats)
            {
                var alignedBeat = new Beat();
                alignedBeat.Strength = beat.Strength;
                var searchSampleIndexStart = Math.Max(0,beat.SampleIndex - (stepSizeOfOriginalSearch / 2));
                var searchSampleIndexEnd = Math.Min(signal.Count, beat.SampleIndex + (stepSizeOfOriginalSearch / 2));
                var searchIndexStart = windowPositions.FindIndex(x => x >= searchSampleIndexStart);
                var searchIndexEnd = windowPositions.FindLastIndex(x => x < searchSampleIndexEnd);

                var bestMatchingIndex = searchIndexStart;
                var strengthOfBestMatch = 0.0;
                for(int timeIndex = searchIndexStart + 1; timeIndex <= searchIndexEnd; ++timeIndex)
                {
                    var strengthOfTimeIndex = 0.0;
                    for(int frequency = 0; frequency < spectrogram[0].Length; ++frequency)
                    {
                        var diff = spectrogram[timeIndex][frequency] - spectrogram[timeIndex - 1][frequency];
                        if(diff>0)
                        {
                            strengthOfTimeIndex += diff;
                        }
                    }
                    if(strengthOfTimeIndex > strengthOfBestMatch)
                    {
                        bestMatchingIndex = timeIndex;
                        strengthOfBestMatch = strengthOfTimeIndex;
                    }
                }
                alignedBeat.SampleIndex = (int)windowPositions[bestMatchingIndex];
                alignedBeats.Add(alignedBeat);
            }

            return alignedBeats;
        }

        private List<SongIntensity> GetSongIntensity(List<float[]> spectrogram, List<double> windowPositions, int sampleRate, int stepSize)
        {
            var fftMagnitudeIncreaseSeries = ComputeFftMagnitudeIncreaseSeries(spectrogram, windowPositions);

            var thresholdWindowSize = (int) Math.Floor(1.5 * sampleRate / stepSize); // Corresponds to 1.5 seconds
            var dynamicThreshold = ComputeDynamicThreshold(
                fftMagnitudeIncreaseSeries.Select(p => p.Y).ToList(),
                thresholdWindowSize, 2, 4);
            var maxValue = fftMagnitudeIncreaseSeries.Max(p => p.Y);
            var movingAverageWindowSize = 1.0 * sampleRate;
            var averagedSignal = fftMagnitudeIncreaseSeries
                .MedianFilter(movingAverageWindowSize)
                .ToList();
            var averagedSignalMax = averagedSignal.Max(p => p.Y);
            return averagedSignal.Select(p => new SongIntensity((int) p.X, p.Y / averagedSignalMax)).ToList();
        }

        private static List<Point2D> ComputeFftMagnitudeIncreaseSeries(List<float[]> spectrogram, List<double> windowPositions)
        {
            const double IncreaseThreshold = 1e-5;

            var fftMagnitudeIncreaseSeries = new List<Point2D>();
            for (int stepIdx = 1; stepIdx < spectrogram.Count; stepIdx++)
            {
                var windowCenterSample = windowPositions[stepIdx];
                var previousFft = spectrogram[stepIdx - 1];
                var currentFft = spectrogram[stepIdx];

                var magnitudeIncreaseCount = 0;
                for (int frequencyBinIdx = 0; frequencyBinIdx < currentFft.Length; frequencyBinIdx++)
                {
                    var isIncrease = currentFft[frequencyBinIdx] - previousFft[frequencyBinIdx] > IncreaseThreshold;
                    if (isIncrease)
                        magnitudeIncreaseCount++;
                }

                fftMagnitudeIncreaseSeries.Add(new Point2D(windowCenterSample, magnitudeIncreaseCount));
            }

            return fftMagnitudeIncreaseSeries;
        }

        private List<double> ComputeDynamicThreshold(IList<double> signal, int windowSize, int minPeakCount, int maxPeakCount)
        {
            var thresholdPoints = new List<Point2D>();
            var startIdx = 0;
            while (startIdx+windowSize <= signal.Count)
            {
                var valuesInWindow = signal.SubArray(startIdx, windowSize);
                var orderedValues = valuesInWindow.OrderByDescending(x => x).ToList();
                var minPeakValue = orderedValues[minPeakCount-1];
                var maxPeakValue = orderedValues[maxPeakCount-1];
                var combinedThreshold = 0.8 * minPeakValue + 0.2 * maxPeakValue;
                thresholdPoints.Add(new Point2D(startIdx + windowSize/2, combinedThreshold));
                startIdx += windowSize / 2;
            }
            thresholdPoints.Add(new Point2D(double.NegativeInfinity, thresholdPoints.First().Y));
            thresholdPoints.Add(new Point2D(double.PositiveInfinity, thresholdPoints.Last().Y));
            var continuousThreshold = new ContinuousLine2D(thresholdPoints);
            var dynamicThreshold = Enumerable.Range(0, signal.Count)
                .Select(idx => continuousThreshold.ValueAtX(idx))
                .ToList();
            return dynamicThreshold;
        }
    }

    public class Beat
    {
        public int SampleIndex { get; set; }
        public double Strength { get; set; }
    }
}
