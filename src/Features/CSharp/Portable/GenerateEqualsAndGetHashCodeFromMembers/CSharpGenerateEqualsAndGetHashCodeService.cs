// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GenerateEqualsAndGetHashCodeFromMembers;

[ExportLanguageService(typeof(IGenerateEqualsAndGetHashCodeService), LanguageNames.CSharp), Shared]
internal class CSharpGenerateEqualsAndGetHashCodeService : AbstractGenerateEqualsAndGetHashCodeService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpGenerateEqualsAndGetHashCodeService()
    {
    }

    protected override bool TryWrapWithUnchecked(ImmutableArray<SyntaxNode> statements, out ImmutableArray<SyntaxNode> wrappedStatements)
    {
        wrappedStatements = [SyntaxFactory.CheckedStatement(SyntaxKind.UncheckedStatement,
                SyntaxFactory.Block(statements.OfType<StatementSyntax>()))];
        return true;
    }
}
