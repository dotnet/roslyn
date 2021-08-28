﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GenerateDefaultConstructors
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateDefaultConstructors), Shared]
    internal class CSharpGenerateDefaultConstructorsCodeFixProvider : AbstractGenerateDefaultConstructorCodeFixProvider
    {
        private const string CS1729 = nameof(CS1729); // 'B' does not contain a constructor that takes 0 arguments CSharpConsoleApp3   C:\Users\cyrusn\source\repos\CSharpConsoleApp3\CSharpConsoleApp3\Program.cs	1	Active
        private const string CS7036 = nameof(CS7036); // There is no argument given that corresponds to the required formal parameter 's' of 'B.B(string)'

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateDefaultConstructorsCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS1729, CS7036);
    }
}
