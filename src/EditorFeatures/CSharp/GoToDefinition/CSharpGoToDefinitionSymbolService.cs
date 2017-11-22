// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToDefinitionSymbolService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToDefinitionSymbolService : AbstractGoToDefinitionSymbolService
    {
        protected override ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation)
        {
            return symbol;
        }

        protected override async Task<int?> GetDeclarationPositionIfOverride(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = await CodeAnalysis.Shared.Extensions.SyntaxTreeExtensions.GetTouchingTokenAsync(syntaxTree, position, cancellationToken)
                .ConfigureAwait(false);

            if (token.Kind() == SyntaxKind.OverrideKeyword && token.Parent is MemberDeclarationSyntax member)
            {
                return member.GetNameToken().SpanStart;
            }

            return null;
        }
    }
}
