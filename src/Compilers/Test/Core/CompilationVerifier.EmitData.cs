// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed partial class CompilationVerifier
    {
        private sealed class EmitData(
            ModuleData emittedModule,
            ImmutableArray<ModuleData> modules,
            ImmutableArray<Diagnostic> diagnostics,
            CompilationTestData testData)
        {
            public ModuleData EmittedModule { get; } = emittedModule;
            public ImmutableArray<ModuleData> Modules { get; } = modules;
            public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
            internal CompilationTestData TestData { get; } = testData;

            internal ImmutableArray<byte> EmittedAssemblyData => EmittedModule.Image;
            internal ImmutableArray<byte> EmittedAssemblyPdb => EmittedModule.Pdb;

            public override string ToString() => EmittedModule.FullName;
        }
    }
}
