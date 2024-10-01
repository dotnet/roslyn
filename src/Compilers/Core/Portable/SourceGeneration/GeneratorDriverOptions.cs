// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Options passed to a <see cref="GeneratorDriver"/> during creation.
    /// </summary>
    public readonly struct GeneratorDriverOptions
    {
        public readonly IncrementalGeneratorOutputKind DisabledOutputs;

        public readonly bool TrackIncrementalGeneratorSteps;

        /// <summary>
        /// Absolute path to directory that generated source file paths are rooted with.
        /// Usually the project's output directory unless <see cref="CommandLineArguments.GeneratedFilesOutputDirectory"/> is specified.
        /// </summary>
        public string? BaseDirectory { get; }

        [Obsolete("Use other overload")]
        public GeneratorDriverOptions(IncrementalGeneratorOutputKind disabledOutputs)
            : this(disabledOutputs, false)
        {
        }

        [Obsolete("Use other overload")]
        public GeneratorDriverOptions(IncrementalGeneratorOutputKind disabledOutputs, bool trackIncrementalGeneratorSteps)
        {
            DisabledOutputs = disabledOutputs;
            TrackIncrementalGeneratorSteps = trackIncrementalGeneratorSteps;
        }

        /// <summary>
        /// Creates <see cref="GeneratorDriverOptions"/>.
        /// </summary>
        /// <param name="baseDirectory">Absolute path to the base directory used for file paths of generated files.</param>
        /// <param name="disabledOutputs"></param>
        /// <param name="trackIncrementalGeneratorSteps"></param>
        /// <exception cref="ArgumentException"><paramref name="baseDirectory"/> is not an absolute path.</exception>
        public GeneratorDriverOptions(string baseDirectory, IncrementalGeneratorOutputKind disabledOutputs = IncrementalGeneratorOutputKind.None, bool trackIncrementalGeneratorSteps = false)
        {
            if (!PathUtilities.IsAbsolute(baseDirectory))
            {
                throw new ArgumentException(nameof(baseDirectory), CodeAnalysisResources.AbsolutePathExpected);
            }

            DisabledOutputs = disabledOutputs;
            TrackIncrementalGeneratorSteps = trackIncrementalGeneratorSteps;
            BaseDirectory = baseDirectory;
        }
    }
}
