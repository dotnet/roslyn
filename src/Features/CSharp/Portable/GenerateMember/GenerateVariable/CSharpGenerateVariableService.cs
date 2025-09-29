// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateMember.GenerateVariable;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateVariable;

using static SyntaxFactory;

[ExportLanguageService(typeof(IGenerateVariableService), LanguageNames.CSharp), Shared]
internal sealed partial class CSharpGenerateVariableService :
    AbstractGenerateVariableService<CSharpGenerateVariableService, SimpleNameSyntax, ExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpGenerateVariableService()
    {
    }

    protected override bool IsExplicitInterfaceGeneration(SyntaxNode node)
        => node is PropertyDeclarationSyntax;

    protected override bool IsIdentifierNameGeneration(SyntaxNode node)
        => node is IdentifierNameSyntax identifierName && !IsProbablySyntacticConstruct(identifierName.Identifier);

    private static bool IsProbablySyntacticConstruct(SyntaxToken token)
    {
        // Technically all C# contextual keywords are valid member names.
        // However some of them start various syntactic constructs
        // and we don't want to show "Generate <member name>" codefix for them:
        // 1. "from" starts LINQ expression
        // 2. "nameof" is probably nameof(some_name)
        // 3. "async" can start a delegate declaration
        // 4. "await" starts await expression
        // 5. "var" is used in constructions like "var x = ..."
        // The list can be expanded in the future if necessary
        // This method tells if the given SyntaxToken is one of the cases above
        var contextualKind = SyntaxFacts.GetContextualKeywordKind(token.ValueText);

        return contextualKind is SyntaxKind.FromKeyword or
                              SyntaxKind.NameOfKeyword or
                              SyntaxKind.AsyncKeyword or
                              SyntaxKind.AwaitKeyword or
                              SyntaxKind.VarKeyword;
    }

    protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

    protected override bool TryInitializeExplicitInterfaceState(
        SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken,
        out SyntaxToken identifierToken, out IPropertySymbol propertySymbol, out INamedTypeSymbol typeToGenerateIn)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)node;
        identifierToken = propertyDeclaration.Identifier;

        if (propertyDeclaration.ExplicitInterfaceSpecifier != null)
        {
            var semanticModel = document.SemanticModel;
            propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken);
            if (propertySymbol != null && !propertySymbol.ExplicitInterfaceImplementations.Any())
            {
                var info = semanticModel.GetTypeInfo(propertyDeclaration.ExplicitInterfaceSpecifier.Name, cancellationToken);
                typeToGenerateIn = info.Type as INamedTypeSymbol;
                return typeToGenerateIn != null;
            }
        }

        identifierToken = default;
        propertySymbol = null;
        typeToGenerateIn = null;
        return false;
    }

    protected override bool TryInitializeIdentifierNameState(
        SemanticDocument document, SimpleNameSyntax identifierName, CancellationToken cancellationToken,
        out SyntaxToken identifierToken, out ExpressionSyntax simpleNameOrMemberAccessExpression, out bool isInExecutableBlock, out bool isConditionalAccessExpression)
    {
        identifierToken = identifierName.Identifier;
        if (identifierToken.ValueText != string.Empty &&
            !IsProbablyGeneric(identifierName, cancellationToken))
        {
            if (identifierName.Parent is MemberAccessExpressionSyntax memberAccessExpression &&
                memberAccessExpression.Name == identifierName)
            {
                simpleNameOrMemberAccessExpression = memberAccessExpression;
            }
            else if (identifierName.Parent.Parent is ConditionalAccessExpressionSyntax conditionalAccessExpression &&
                conditionalAccessExpression.WhenNotNull == identifierName.Parent)
            {
                simpleNameOrMemberAccessExpression = conditionalAccessExpression;
            }
            else if (identifierName.Parent is MemberBindingExpressionSyntax memberBindingExpression &&
                identifierName.Parent.Parent is AssignmentExpressionSyntax assignmentExpression &&
                assignmentExpression.Left == memberBindingExpression)
            {
                simpleNameOrMemberAccessExpression = memberBindingExpression;
            }
            else
            {
                simpleNameOrMemberAccessExpression = identifierName;
            }

            isConditionalAccessExpression = identifierName.Parent.Parent is ConditionalAccessExpressionSyntax;

            // If we're being invoked, then don't offer this, offer generate method instead.
            // Note: we could offer to generate a field with a delegate type.  However, that's
            // very esoteric and probably not what most users want.
            if (!IsLegal(document, simpleNameOrMemberAccessExpression, cancellationToken))
            {
                isInExecutableBlock = false;
                isConditionalAccessExpression = false;
                return false;
            }

            var block = identifierName.GetAncestor<BlockSyntax>();
            isInExecutableBlock = block != null && !block.OverlapsHiddenPosition(cancellationToken);
            return true;
        }

        identifierToken = default;
        simpleNameOrMemberAccessExpression = null;
        isInExecutableBlock = false;
        isConditionalAccessExpression = false;
        return false;
    }

    private static bool IsProbablyGeneric(SimpleNameSyntax identifierName, CancellationToken cancellationToken)
    {
        if (identifierName.IsKind(SyntaxKind.GenericName))
        {
            return true;
        }

        // We might have something of the form:   Goo < Bar.
        // In this case, we would want to generate offer a member called 'Goo'.  however, if we have
        // something like "Goo < string >" then that's clearly something generic and we don't want
        // to offer to generate a member there.
        var localRoot = identifierName.GetAncestor<StatementSyntax>() ??
                        identifierName.GetAncestor<MemberDeclarationSyntax>() ??
                        identifierName.SyntaxTree.GetRoot(cancellationToken);

        // In order to figure this out (without writing our own parser), we just try to parse out a
        // type name here.  If we get a generic name back, without any errors, then we'll assume the
        // user really is typing a generic name, and thus should not get recommendations to create a
        // variable.
        var localText = localRoot.ToString();
        var startIndex = identifierName.Span.Start - localRoot.Span.Start;

        var parsedType = ParseTypeName(localText, startIndex, consumeFullText: false);

        return parsedType.IsKind(SyntaxKind.GenericName) && !parsedType.ContainsDiagnostics;
    }

    private static bool IsLegal(
        SemanticDocument document,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        // TODO(cyrusn): Consider supporting this at some point.  It is difficult because we'd
        // need to replace the identifier typed with the fully qualified name of the field we
        // were generating.
        if (expression.IsParentKind(SyntaxKind.AttributeArgument))
        {
            return false;
        }

        if (expression.IsParentKind(SyntaxKind.ConditionalAccessExpression))
        {
            return true;
        }

        if (expression.IsParentKind(SyntaxKind.IsPatternExpression))
        {
            return true;
        }

        if (expression.Parent is (kind: SyntaxKind.NameColon or SyntaxKind.ExpressionColon) &&
            expression.Parent.IsParentKind(SyntaxKind.Subpattern))
        {
            return true;
        }

        if (expression.IsParentKind(SyntaxKind.ConstantPattern))
        {
            return true;
        }

        return expression.CanReplaceWithLValue(document.SemanticModel, cancellationToken);
    }

    protected override bool TryConvertToLocalDeclaration(ITypeSymbol type, SyntaxToken identifierToken, SemanticModel semanticModel, CancellationToken cancellationToken, out SyntaxNode newRoot)
    {
        var token = identifierToken;
        var node = identifierToken.Parent as IdentifierNameSyntax;
        if (node.IsLeftSideOfAssignExpression() && node.Parent.IsParentKind(SyntaxKind.ExpressionStatement))
        {
            var assignExpression = (AssignmentExpressionSyntax)node.Parent;
            var expressionStatement = (StatementSyntax)assignExpression.Parent;

            var declarationStatement = LocalDeclarationStatement(
                VariableDeclaration(
                    type.GenerateTypeSyntax(),
                    [VariableDeclarator(token, null, EqualsValueClause(
                        assignExpression.OperatorToken, assignExpression.Right))]));
            declarationStatement = declarationStatement.WithAdditionalAnnotations(Formatter.Annotation);

            var root = token.GetAncestor<CompilationUnitSyntax>();
            newRoot = root.ReplaceNode(expressionStatement, declarationStatement);

            return true;
        }

        newRoot = null;
        return false;
    }
}
