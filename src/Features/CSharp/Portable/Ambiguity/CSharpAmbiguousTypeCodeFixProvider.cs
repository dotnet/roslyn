// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AmbiguityCodeFixProvider;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Ambiguity
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AliasAmbiguousType), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
    internal class CSharpAmbiguousTypeCodeFixProvider : AbstractAmbiguousTypeCodeFixProvider
    {
        /// <summary>
        /// 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        /// </summary>
        private const string CS0104 = nameof(CS0104);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS0104);

        protected override SyntaxNode GetAliasDirective(string typeName, ISymbol symbol)
            => SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals(typeName),
                                            SyntaxFactory.IdentifierName(symbol.ToNameDisplayString()));
    }
}
