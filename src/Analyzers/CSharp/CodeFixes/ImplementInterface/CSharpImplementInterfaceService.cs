// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface;

[ExportLanguageService(typeof(IImplementInterfaceService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpImplementInterfaceService() : AbstractImplementInterfaceService<TypeDeclarationSyntax>
{
    protected override SyntaxGeneratorInternal SyntaxGeneratorInternal
        => CSharpSyntaxGeneratorInternal.Instance;

    protected override string ToDisplayString(IMethodSymbol disposeImplMethod, SymbolDisplayFormat format)
        => SymbolDisplay.ToDisplayString(disposeImplMethod, format);

    protected override bool AllowDelegateAndEnumConstraints(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp7_3;

    protected override bool IsTypeInInterfaceBaseList(SyntaxNode? type)
        => type?.Parent is BaseTypeSyntax { Parent: BaseListSyntax } baseTypeParent && baseTypeParent.Type == type;

    protected override void AddInterfaceTypes(TypeDeclarationSyntax typeDeclaration, ArrayBuilder<SyntaxNode> result)
    {
        if (typeDeclaration.BaseList != null)
        {
            foreach (var baseType in typeDeclaration.BaseList.Types)
                result.Add(baseType.Type);
        }
    }

    protected override bool TryInitializeState(
        Document document, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken,
        [NotNullWhen(true)] out SyntaxNode? classOrStructDecl,
        [NotNullWhen(true)] out INamedTypeSymbol? classOrStructType,
        out ImmutableArray<INamedTypeSymbol> interfaceTypes)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            if (node is TypeSyntax interfaceNode && interfaceNode.Parent is BaseTypeSyntax baseType &&
                baseType.Parent is BaseListSyntax baseList &&
                baseType.Type == interfaceNode)
            {
                if (baseList.GetRequiredParent() is TypeDeclarationSyntax(kind:
                        SyntaxKind.ClassDeclaration or
                        SyntaxKind.StructDeclaration or
                        SyntaxKind.RecordDeclaration or
                        SyntaxKind.RecordStructDeclaration) typeDeclaration)
                {
                    var interfaceSymbolInfo = model.GetSymbolInfo(interfaceNode, cancellationToken);
                    if (interfaceSymbolInfo.CandidateReason != CandidateReason.WrongArity)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (interfaceSymbolInfo.GetAnySymbol() is INamedTypeSymbol interfaceType && interfaceType.TypeKind == TypeKind.Interface)
                        {
                            classOrStructDecl = typeDeclaration;
                            classOrStructType = model.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);
                            interfaceTypes = [interfaceType];

                            return interfaceTypes != null && classOrStructType != null;
                        }
                    }
                }
            }
        }

        classOrStructDecl = null;
        classOrStructType = null;
        interfaceTypes = default;
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
        var disposeStatement = (StatementSyntax)AddComment(
            string.Format(CodeFixesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, disposeMethodDisplayString),
            g.ExpressionStatement(g.InvocationExpression(
                g.IdentifierName(nameof(IDisposable.Dispose)),
                g.Argument(DisposingName, RefKind.None, g.FalseLiteralExpression()))));

        var methodDecl = SyntaxFactory.DestructorDeclaration(classType.Name).AddBodyStatements(disposeStatement);

        return AddComment(
            string.Format(CodeFixesResources.TODO_colon_override_finalizer_only_if_0_has_code_to_free_unmanaged_resources, disposeMethodDisplayString),
            methodDecl);
    }
}
