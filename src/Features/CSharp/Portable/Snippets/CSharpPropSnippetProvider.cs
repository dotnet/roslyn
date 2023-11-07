// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpPropSnippetProvider : AbstractCSharpAutoPropertySnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpPropSnippetProvider()
        {
        }

        public override string Identifier => "prop";

        public override string Description => FeaturesResources.property_;

        protected override AccessorDeclarationSyntax? GenerateSetAccessorDeclaration(CSharpSyntaxContext syntaxContext, SyntaxGenerator generator)
        {
            // Having a property with `set` accessor in a readonly struct leads to a compiler error.
            // So if user executes snippet inside a readonly struct the right thing to do is to not generate `set` accessor at all
            if (syntaxContext.ContainingTypeDeclaration is StructDeclarationSyntax structDeclaration &&
                structDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                return null;
            }

            return base.GenerateSetAccessorDeclaration(syntaxContext, generator);
        }
    }
}
