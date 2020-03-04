﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.AddPackage
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpAddSpecificPackageCodeFixProvider : AbstractAddSpecificPackageCodeFixProvider
    {
        private const string CS8179 = nameof(CS8179); // Predefined type 'System.ValueTuple`2' is not defined or imported

        [ImportingConstructor]
        public CSharpAddSpecificPackageCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS8179);

        protected override string GetAssemblyName(string id)
        {
            switch (id)
            {
                case CS8179: return "System.ValueTuple";
            }

            return null;
        }
    }
}
