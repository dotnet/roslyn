﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

using static SyntaxFactory;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseCollectionInitializerDiagnosticAnalyzer :
    AbstractUseCollectionInitializerDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        InvocationExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseCollectionInitializerAnalyzer>
{
    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
        => CSharpUseCollectionInitializerAnalyzer.Allocate();

    protected override bool AreCollectionInitializersSupported(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp3;

    protected override bool AreCollectionExpressionsSupported(Compilation compilation)
        => compilation.LanguageVersion().SupportsCollectionExpressions();

    protected override bool CanUseCollectionExpression(
        SemanticModel semanticModel,
        BaseObjectCreationExpressionSyntax objectCreationExpression,
        INamedTypeSymbol? expressionType,
        ImmutableArray<CollectionMatch<SyntaxNode>> preMatches,
        bool allowSemanticsChange,
        CancellationToken cancellationToken,
        out bool changesSemantics)
    {
        // Synthesize the final collection expression we would replace this object-creation with.  That will allow us to
        // determine if we end up calling the right overload in cases of overloaded methods.
        var replacement = CollectionExpression(SeparatedList(
            GetMatchElements(preMatches).Concat(GetInitializerElements(objectCreationExpression.Initializer))));

        return UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
            semanticModel, objectCreationExpression, replacement, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out changesSemantics);

        static IEnumerable<CollectionElementSyntax> GetMatchElements(ImmutableArray<CollectionMatch<SyntaxNode>> preMatches)
        {
            foreach (var match in preMatches)
            {
                if (match.Node is ExpressionSyntax expression)
                    yield return match.UseSpread ? SpreadElement(expression) : ExpressionElement(expression);
            }
        }

        static IEnumerable<CollectionElementSyntax> GetInitializerElements(InitializerExpressionSyntax? initializer)
        {
            if (initializer != null)
            {
                foreach (var expression in initializer.Expressions)
                    yield return ExpressionElement(expression);
            }
        }
    }
}
