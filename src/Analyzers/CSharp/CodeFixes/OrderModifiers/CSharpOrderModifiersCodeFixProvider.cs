﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.OrderModifiers;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.OrderModifiers), Shared]
    internal sealed class CSharpOrderModifiersCodeFixProvider : AbstractOrderModifiersCodeFixProvider
    {
        private const string CS0267 = nameof(CS0267); // The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or 'void'

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpOrderModifiersCodeFixProvider()
            : base(CSharpSyntaxFacts.Instance, CSharpOrderModifiersHelper.Instance)
        {
        }

        protected override CodeStyleOption2<string> GetCodeStyleOption(AnalyzerOptionsProvider options)
            => ((CSharpAnalyzerOptionsProvider)options).PreferredModifierOrder;

        protected override ImmutableArray<string> FixableCompilerErrorIds { get; } = ImmutableArray.Create(CS0267);
    }
}
