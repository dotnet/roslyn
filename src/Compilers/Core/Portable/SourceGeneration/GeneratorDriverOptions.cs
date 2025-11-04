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
        /// Absolute path to directory that generated source file paths are rooted with, or null to use relative paths for the generated files.
        /// Usually the project's output directory unless <see cref="CommandLineArguments.GeneratedFilesOutputDirectory"/> is specified.
        /// </summary>
        public string? BaseDirectory { get; }

        /// <summary>
        /// The name of the project this generator driver is for.
        /// </summary>
        /// <remarks>
        /// Only used for telemetry purposes.
        /// </remarks>
        public string? ProjectName { get; }

        public GeneratorDriverOptions(IncrementalGeneratorOutputKind disabledOutputs)
            : this(disabledOutputs, false)
        {
        }

        public GeneratorDriverOptions(IncrementalGeneratorOutputKind disabledOutputs, bool trackIncrementalGeneratorSteps)
        {
            DisabledOutputs = disabledOutputs;
            TrackIncrementalGeneratorSteps = trackIncrementalGeneratorSteps;
        }

        /// <summary>
        /// Creates <see cref="GeneratorDriverOptions"/>.
        /// </summary>
        /// <param name="disabledOutputs"></param>
        /// <param name="trackIncrementalGeneratorSteps"></param>
        /// <param name="baseDirectory">Absolute path to the base directory used for file paths of generated files.</param>
        /// <exception cref="ArgumentException"><paramref name="baseDirectory"/> is not an absolute path.</exception>
        public GeneratorDriverOptions(IncrementalGeneratorOutputKind disabledOutputs = IncrementalGeneratorOutputKind.None, bool trackIncrementalGeneratorSteps = false, string? baseDirectory = null, string? projectName = null)
        {
            if (baseDirectory != null && !PathUtilities.IsAbsolute(baseDirectory))
            {
                throw new ArgumentException(message: CodeAnalysisResources.AbsolutePathExpected, nameof(baseDirectory));
            }

            DisabledOutputs = disabledOutputs;
            TrackIncrementalGeneratorSteps = trackIncrementalGeneratorSteps;
            BaseDirectory = baseDirectory;
            ProjectName = projectName;
        }

        // 5.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        public GeneratorDriverOptions(IncrementalGeneratorOutputKind disabledOutputs, bool trackIncrementalGeneratorSteps, string? baseDirectory)
            : this(disabledOutputs, trackIncrementalGeneratorSteps, baseDirectory, projectName: null)
        {
        }
    }
}
