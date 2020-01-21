// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GenerateEqualsAndGetHashCodeFromMembers
{
    [ExportLanguageService(typeof(IGenerateEqualsAndGetHashCodeService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateEqualsAndGetHashCodeService : AbstractGenerateEqualsAndGetHashCodeService
    {
        [ImportingConstructor]
        public CSharpGenerateEqualsAndGetHashCodeService()
        {
        }

        protected override bool TryWrapWithUnchecked(ImmutableArray<SyntaxNode> statements, out ImmutableArray<SyntaxNode> wrappedStatements)
        {
            wrappedStatements = ImmutableArray.Create<SyntaxNode>(
                SyntaxFactory.CheckedStatement(SyntaxKind.UncheckedStatement,
                    SyntaxFactory.Block(statements.OfType<StatementSyntax>())));
            return true;
        }
    }
}
