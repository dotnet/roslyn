// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface;

[ExportLanguageService(typeof(IImplementInterfaceService), LanguageNames.CSharp), Shared]
internal class CSharpImplementInterfaceService : AbstractImplementInterfaceService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpImplementInterfaceService()
    {
    }

    protected override string ToDisplayString(IMethodSymbol disposeImplMethod, SymbolDisplayFormat format)
        => SymbolDisplay.ToDisplayString(disposeImplMethod, format);

    protected override bool AllowDelegateAndEnumConstraints(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp7_3;

    protected override bool TryInitializeState(
        Document document, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken,
        out SyntaxNode classOrStructDecl, out INamedTypeSymbol classOrStructType, out IEnumerable<INamedTypeSymbol> interfaceTypes)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            if (node is TypeSyntax interfaceNode && interfaceNode.Parent is BaseTypeSyntax baseType &&
                baseType.IsParentKind(SyntaxKind.BaseList) &&
                baseType.Type == interfaceNode)
            {
                if (interfaceNode.Parent.Parent?.Parent.Kind() is
                        SyntaxKind.ClassDeclaration or
                        SyntaxKind.StructDeclaration or
                        SyntaxKind.RecordDeclaration or
                        SyntaxKind.RecordStructDeclaration)
                {
                    var interfaceSymbolInfo = model.GetSymbolInfo(interfaceNode, cancellationToken);
                    if (interfaceSymbolInfo.CandidateReason != CandidateReason.WrongArity)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (interfaceSymbolInfo.GetAnySymbol() is INamedTypeSymbol interfaceType && interfaceType.TypeKind == TypeKind.Interface)
                        {
                            classOrStructDecl = interfaceNode.Parent.Parent.Parent as TypeDeclarationSyntax;
                            classOrStructType = model.GetDeclaredSymbol(classOrStructDecl, cancellationToken) as INamedTypeSymbol;
                            interfaceTypes = [interfaceType];

                            return interfaceTypes != null && classOrStructType != null;
                        }
                    }
                }
            }
        }

        classOrStructDecl = null;
        classOrStructType = null;
        interfaceTypes = null;
        return false;
    }

    protected override bool CanImplementImplicitly => true;

    protected override bool HasHiddenExplicitImplementation => true;

    protected override SyntaxNode AddCommentInsideIfStatement(SyntaxNode ifStatement, SyntaxTriviaList trivia)
    {
        return ifStatement.ReplaceToken(
            ifStatement.GetLastToken(),
            ifStatement.GetLastToken().WithPrependedLeadingTrivia(trivia));
    }

    protected override SyntaxNode CreateFinalizer(
        SyntaxGenerator g, INamedTypeSymbol classType, string disposeMethodDisplayString)
    {
        // ' Do not change this code...
        // Dispose(false)
        var disposeStatement = (StatementSyntax)AddComment(g,
            string.Format(FeaturesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, disposeMethodDisplayString),
            g.ExpressionStatement(g.InvocationExpression(
                g.IdentifierName(nameof(IDisposable.Dispose)),
                g.Argument(DisposingName, RefKind.None, g.FalseLiteralExpression()))));

        var methodDecl = SyntaxFactory.DestructorDeclaration(classType.Name).AddBodyStatements(disposeStatement);

        return AddComment(g,
            string.Format(FeaturesResources.TODO_colon_override_finalizer_only_if_0_has_code_to_free_unmanaged_resources, disposeMethodDisplayString),
            methodDecl);
    }
}
