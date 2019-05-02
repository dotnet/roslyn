// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.OrderModifiers;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpOrderModifiersCodeFixProvider : AbstractOrderModifiersCodeFixProvider
    {
        private const string CS0267 = nameof(CS0267); // The 'partial' modifier can only appear immediately before 'class', 'struct', 'interface', or 'void'

#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpOrderModifiersCodeFixProvider()
            : base(CSharpSyntaxFactsService.Instance, CSharpCodeStyleOptions.PreferredModifierOrder, CSharpOrderModifiersHelper.Instance)
        {
        }

        protected override ImmutableArray<string> FixableCompilerErrorIds { get; } = ImmutableArray.Create(CS0267);
    }
}
