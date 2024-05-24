// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal partial class EditAndContinueTest<TSelf> : IDisposable
    {
        internal sealed class GenerationInfo
        {
            public readonly Compilation Compilation;
            public readonly MetadataReader MetadataReader;
            public readonly EmitBaseline Baseline;
            public readonly Action<GenerationVerifier> Verifier;

            /// <summary>
            /// Only available for baseline generation.
            /// </summary>
            public readonly CompilationVerifier? CompilationVerifier;

            /// <summary>
            /// Not available for baseline generation.
            /// </summary>
            public readonly CompilationDifference? CompilationDifference;

            public GenerationInfo(Compilation compilation, MetadataReader reader, CompilationDifference? diff, CompilationVerifier? compilationVerifier, EmitBaseline baseline, Action<GenerationVerifier> verifier)
            {
                Debug.Assert(diff is null ^ compilationVerifier is null);

                Compilation = compilation;
                MetadataReader = reader;
                Baseline = baseline;
                Verifier = verifier;
                CompilationDifference = diff;
                CompilationVerifier = compilationVerifier;
            }
        }
    }
}
