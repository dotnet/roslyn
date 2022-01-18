// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal partial class EditAndContinueTest
    {
        internal sealed class GenerationInfo
        {
            public readonly CSharpCompilation Compilation;
            public readonly MetadataReader MetadataReader;
            public readonly EmitBaseline Baseline;
            public readonly Action<GenerationVerifier> Verifier;

            public GenerationInfo(CSharpCompilation compilation, MetadataReader reader, EmitBaseline baseline, Action<GenerationVerifier> verifier)
            {
                Compilation = compilation;
                MetadataReader = reader;
                Baseline = baseline;
                Verifier = verifier;
            }
        }
    }
}
