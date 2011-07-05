﻿using System.Collections.Generic;
using ILNumerics;
using ILNumerics.BuiltInFunctions;

namespace WaveletStudio.WaveLib
{
    /// <summary>
    /// Discreet Wavelet Transform and its inverse
    /// </summary>
    public static class Dwt
    {        
        /// <summary>
        /// Multilevel 1-D Discreete Wavelet Transform
        /// </summary>
        /// <param name="signal">The signal. Example: new Signal(5, 6, 7, 8, 1, 2, 3, 4)</param>
        /// <param name="motherWavelet">The mother wavelet to be used. Example: CommonMotherWavelets.GetWaveletFromName("DB4")</param>
        /// <param name="level">The depth-level to perform the DWT</param>
        /// <param name="extensionMode">Signal extension mode</param>
        /// <returns></returns>
        public static List<DecompositionLevel> ExecuteDwt(Signal signal, MotherWavelet motherWavelet, int level, SignalExtension.ExtensionMode extensionMode = SignalExtension.ExtensionMode.SymmetricHalfPoint)
        {
            var levels = new List<DecompositionLevel>();
            
            var approximation = signal.Samples.C;
            var details = signal.Samples.C;
            
            for (var i = 1; i <= level; i++)
            {
                var extensionSize = motherWavelet.Filters.DecompositionLowPassFilter.Length - 1;
                
                approximation = SignalExtension.Extend(approximation, extensionMode, extensionSize);
                details = SignalExtension.Extend(details, extensionMode, extensionSize);

                approximation = Convolve(approximation, motherWavelet.Filters.DecompositionLowPassFilter);
                approximation = DownSample(approximation);

                details = Convolve(details, motherWavelet.Filters.DecompositionHighPassFilter);
                details = DownSample(details);
                
                
                levels.Add(new DecompositionLevel
                               {
                                   Approximation = approximation,
                                   Details = details
                               });
                details = approximation.C;
            }
            return levels;
        }

        /// <summary>
        /// Multilevel inverse discrete 1-D wavelet transform
        /// </summary>
        /// <param name="decompositionLevels">The decomposition levels of the DWT</param>
        /// <param name="motherWavelet">The mother wavelet to be used. Example: CommonMotherWavelets.GetWaveletFromName("DB4") </param>
        /// <param name="level">The depth-level to perform the DWT</param>
        /// <returns></returns>
        public static ILArray<double> ExecuteIDwt(List<DecompositionLevel> decompositionLevels, MotherWavelet motherWavelet, int level = 0)
        {
            if (level == 0 || level > decompositionLevels.Count)
            {
                level = decompositionLevels.Count;
            }
            var approximation = decompositionLevels[level-1].Approximation.C;
            var details = decompositionLevels[level - 1].Details.C;

            for (var i = level - 1; i >= 0; i--)
            {
                approximation = UpSample(approximation);
                approximation = Convolve(approximation, motherWavelet.Filters.ReconstructionLowPassFilter, true, -1);

                details = UpSample(details);
                details = Convolve(details, motherWavelet.Filters.ReconstructionHighPassFilter, true, -1);

                //sum approximation with details
                approximation = ILMath.add(approximation, details);

                if (i <= 0) 
                    continue;
                if (approximation.Length > decompositionLevels[i-1].Details.Length)
                {
                    approximation = SignalExtension.Deextend(approximation, decompositionLevels[i - 1].Details.Length);
                }
                details = decompositionLevels[i - 1].Details;
            }

            return approximation;
        }

        /// <summary>
        /// Convolves vectors input and filter.
        /// </summary>
        /// <param name="input">The input signal</param>
        /// <param name="filter">The filter</param>
        /// <param name="returnOnlyValid">True to return only the middle of the array</param>
        /// <param name="margin">Margin to be used if returnOnlyValid is set to true</param>
        /// <returns></returns>
        public static ILArray<double> Convolve(ILArray<double> input, ILArray<double> filter, bool returnOnlyValid = true, int margin = 0)
        {
            if (input.Length < filter.Length)
            {
                var auxSignal = input.C;
                input = filter.C;
                filter = auxSignal;
            }
            var result = new double[input.Length + filter.Length - 1];
            for (var i = 0; i < input.Length; i++)
            {
                for (var j = 0; j < filter.Length; j++)
                {
                    result[i + j] = result[i + j] + input.GetValue(i) * filter.GetValue(j);
                }
            }

            if (returnOnlyValid)
            {
                var size = input.Length - filter.Length + 1;
                var padding = (result.Length - size) / 2;
                return new ILArray<double>(result)[string.Format("{0}:1:{1}", padding + margin, padding + size - 1 - margin)];
            }
            return new ILArray<double>(result);
        }
        
        /// <summary>
        /// Decreases the sampling rate of the input by keeping every odd sample starting with the first sample.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static ILArray<double> DownSample(ILArray<double> input)
        {
            var size = input.Length/2;
            var result = new double[size];
            var j = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (i%2 == 0) 
                    continue;
                result[j] = input.GetValue(i);
                j++;
            }
            return new ILArray<double>(result);
        }

        /// <summary>
        /// Increases the sampling rate of the input by inserting n-1 zeros between samples. 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static ILArray<double> UpSample(ILArray<double> input)
        {
            if (input.IsEmpty)
            {
                return ILMath.empty();
            }
            var size = input.Length * 2;
            var result = new double[size-1];
            for (var i = 0; i < input.Length; i++)
            {
                result[i*2] = input.GetValue(i);
            }
            return new ILArray<double>(result);            
        }

        
    }
}
