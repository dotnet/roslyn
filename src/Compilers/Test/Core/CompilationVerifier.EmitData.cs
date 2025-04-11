// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader.Tools;
using Roslyn.Test.Utilities;
using Xunit;

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
