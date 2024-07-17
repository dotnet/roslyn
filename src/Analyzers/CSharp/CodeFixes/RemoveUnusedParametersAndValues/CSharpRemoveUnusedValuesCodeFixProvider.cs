// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedValues), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpRemoveUnusedValuesCodeFixProvider()
    : AbstractRemoveUnusedValuesCodeFixProvider<ExpressionSyntax, StatementSyntax, BlockSyntax,
        ExpressionStatementSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax,
        ForEachStatementSyntax, SwitchSectionSyntax, SwitchLabelSyntax, CatchClauseSyntax, CatchClauseSyntax>
{
    protected override ISyntaxFormatting GetSyntaxFormatting()
        => CSharpSyntaxFormatting.Instance;

    protected override BlockSyntax WrapWithBlockIfNecessary(IEnumerable<StatementSyntax> statements)
        => Block(statements);

    protected override SyntaxToken GetForEachStatementIdentifier(ForEachStatementSyntax node)
        => node.Identifier;

    protected override LocalDeclarationStatementSyntax? GetCandidateLocalDeclarationForRemoval(VariableDeclaratorSyntax declarator)
        => declarator.Parent?.Parent as LocalDeclarationStatementSyntax;

    protected override SyntaxNode? TryUpdateNameForFlaggedNode(SyntaxNode node, SyntaxToken newName)
    {
        switch (node.Kind())
        {
            case SyntaxKind.IdentifierName:
                var identifierName = (IdentifierNameSyntax)node;
                return identifierName.WithIdentifier(newName.WithTriviaFrom(identifierName.Identifier));

            case SyntaxKind.VariableDeclarator:
                var variableDeclarator = (VariableDeclaratorSyntax)node;
                if (newName.ValueText == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName &&
                    variableDeclarator.Initializer?.Value is ImplicitObjectCreationExpressionSyntax implicitObjectCreation &&
                    variableDeclarator.Parent is VariableDeclarationSyntax parent)
                {
                    // If we are generating a discard on the left of an initialization with an implicit object creation on the right,
                    // then we need to replace the implicit object creation with an explicit one.
                    // For example: 'TypeName v = new();' must be changed to '_ = new TypeName();'
                    var objectCreationNode = ObjectCreationExpression(
                        newKeyword: implicitObjectCreation.NewKeyword,
                        type: parent.Type,
                        argumentList: implicitObjectCreation.ArgumentList,
                        initializer: implicitObjectCreation.Initializer);
                    variableDeclarator = variableDeclarator.WithInitializer(variableDeclarator.Initializer.WithValue(objectCreationNode));
                }

                return variableDeclarator.WithIdentifier(newName.WithTriviaFrom(variableDeclarator.Identifier));

            case SyntaxKind.SingleVariableDesignation:
                return newName.ValueText == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName
                    ? DiscardDesignation().WithTriviaFrom(node)
                    : SingleVariableDesignation(newName).WithTriviaFrom(node);

            case SyntaxKind.CatchDeclaration:
                var catchDeclaration = (CatchDeclarationSyntax)node;
                return catchDeclaration.WithIdentifier(newName.WithTriviaFrom(catchDeclaration.Identifier));

            case SyntaxKind.VarPattern:
                return node.IsParentKind(SyntaxKind.Subpattern)
                    ? DiscardPattern().WithTriviaFrom(node)
                    : DiscardDesignation();

            default:
                Debug.Fail($"Unexpected node kind for local/parameter declaration or reference: '{node.Kind()}'");
                return null;
        }
    }

    protected override SyntaxNode? TryUpdateParentOfUpdatedNode(SyntaxNode parent, SyntaxNode newNameNode, SyntaxEditor editor, ISyntaxFacts syntaxFacts, SemanticModel semanticModel)
    {
        if (newNameNode.IsKind(SyntaxKind.DiscardDesignation))
        {
            var triviaToAppend = newNameNode.GetLeadingTrivia().AddRange(newNameNode.GetTrailingTrivia());

            // 1) `... is MyType variable` -> `... is MyType`
            // 2) `... is MyType /*1*/ variable /*2*/` -> `... is MyType /*1*/  /*2*/`
            if (parent is DeclarationPatternSyntax declarationPattern &&
                parent.SyntaxTree.Options.LanguageVersion() >= LanguageVersion.CSharp9)
            {
                var trailingTrivia = declarationPattern.Type.GetTrailingTrivia().AddRange(triviaToAppend);
                return TypePattern(declarationPattern.Type).WithTrailingTrivia(trailingTrivia);
            }

            // 1) `... is { } variable` -> `... is { }`
            // 2) `... is { } /*1*/ variable /*2*/` -> `... is { } /*1*/  /*2*/`
            if (parent is RecursivePatternSyntax recursivePattern)
            {
                var withoutDesignation = recursivePattern.WithDesignation(null);
                return withoutDesignation.WithAppendedTrailingTrivia(triviaToAppend);
            }

            // 1) `... is [] variable` -> `... is []`
            // 2) `... is [] /*1*/ variable /*2*/` -> `... is [] /*1*/  /*2*/`
            if (parent is ListPatternSyntax listPattern)
            {
                var withoutDesignation = listPattern.WithDesignation(null);
                return withoutDesignation.WithAppendedTrailingTrivia(triviaToAppend);
            }
        }
        else if (parent is AssignmentExpressionSyntax assignment &&
            assignment.Right is ImplicitObjectCreationExpressionSyntax implicitObjectCreation &&
            newNameNode is IdentifierNameSyntax { Identifier.ValueText: AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName } &&
            semanticModel.GetTypeInfo(implicitObjectCreation).Type is { } type)
        {
            // If we are generating a discard on the left of an assignment with an implicit object creation on the right,
            // then we need to replace the implicit object creation with an explicit one.
            // For example: 'v = new();' must be changed to '_ = new TypeOfV();'
            var objectCreationNode = ObjectCreationExpression(
                newKeyword: implicitObjectCreation.NewKeyword,
                type: type.GenerateTypeSyntax(allowVar: false),
                argumentList: implicitObjectCreation.ArgumentList,
                initializer: implicitObjectCreation.Initializer);
            return assignment.Update((ExpressionSyntax)newNameNode, assignment.OperatorToken, objectCreationNode);
        }

        return null;
    }

    protected override SyntaxNode ComputeReplacementNode(SyntaxNode originalOldNode, SyntaxNode changedOldNode, SyntaxNode proposedReplacementNode)
    {
        // Check for the following change: `{ ... } variable` -> `{ ... }`
        // Since the internals of this patterns might be changed during previous iterations
        // we apply the same change (remove `variable` declaration) to the `changedOldNode`
        if (originalOldNode is RecursivePatternSyntax originalOldRecursivePattern &&
            proposedReplacementNode is RecursivePatternSyntax proposedReplacementRecursivePattern &&
            proposedReplacementRecursivePattern.IsEquivalentTo(originalOldRecursivePattern.WithDesignation(null)))
        {
            proposedReplacementNode = ((RecursivePatternSyntax)changedOldNode).WithDesignation(null)
                                                                              .WithTrailingTrivia(proposedReplacementNode.GetTrailingTrivia());
        }

        // Check for the following change: `[...] variable` -> `[...]`
        // Since the internals of this patterns might be changed during previous iterations
        // we apply the same change (remove `variable` declaration) to the `changedOldNode`
        if (originalOldNode is ListPatternSyntax originalOldListPattern &&
            proposedReplacementNode is ListPatternSyntax proposedReplacementListPattern &&
            proposedReplacementListPattern.IsEquivalentTo(originalOldListPattern.WithDesignation(null)))
        {
            proposedReplacementNode = ((ListPatternSyntax)changedOldNode).WithDesignation(null)
                                                                         .WithTrailingTrivia(proposedReplacementNode.GetTrailingTrivia());
        }

        return proposedReplacementNode.WithAdditionalAnnotations(Formatter.Annotation);
    }

    protected override void InsertAtStartOfSwitchCaseBlockForDeclarationInCaseLabelOrClause(SwitchSectionSyntax switchCaseBlock, SyntaxEditor editor, LocalDeclarationStatementSyntax declarationStatement)
    {
        var firstStatement = switchCaseBlock.Statements.FirstOrDefault();
        if (firstStatement != null)
        {
            editor.InsertBefore(firstStatement, declarationStatement);
        }
        else
        {
            // Switch section without any statements is an error case.
            // Insert before containing switch statement.
            editor.InsertBefore(switchCaseBlock.GetRequiredParent(), declarationStatement);
        }
    }

    protected override SyntaxNode GetReplacementNodeForCompoundAssignment(
        SyntaxNode originalCompoundAssignment,
        SyntaxNode newAssignmentTarget,
        SyntaxEditor editor,
        ISyntaxFactsService syntaxFacts)
    {
        // 1. Compound assignment is changed to simple assignment.
        // For example, "x += MethodCall();", where assignment to 'x' is redundant
        // is replaced with "_ = MethodCall();" or "var unused = MethodCall();
        //
        // 2. Null coalesce assignment is changed to assignment with null coalesce
        // expression on the right.
        // For example, "x ??= MethodCall();", where assignment to 'x' is redundant
        // is replaced with "_ = x ?? MethodCall();" or "var unused = x ?? MethodCall();
        //
        // 3. However, if the node is not parented by an expression statement then we
        // don't generate an assignment, but just the expression.
        // For example, "return x += MethodCall();" is replaced with "return x + MethodCall();"
        // and "return x ??= MethodCall();" is replaced with "return x ?? MethodCall();"

        if (originalCompoundAssignment is not AssignmentExpressionSyntax assignmentExpression)
        {
            Debug.Fail($"Unexpected kind for originalCompoundAssignment: {originalCompoundAssignment.Kind()}");
            return originalCompoundAssignment;
        }

        var leftOfAssignment = assignmentExpression.Left;
        var rightOfAssignment = assignmentExpression.Right;

        if (originalCompoundAssignment.Parent.IsKind(SyntaxKind.ExpressionStatement))
        {
            if (!originalCompoundAssignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
            {
                // Case 1. Simple compound assignment parented by an expression statement.
                return editor.Generator.AssignmentStatement(newAssignmentTarget, rightOfAssignment);
            }
            else
            {
                // Case 2. Null coalescing compound assignment parented by an expression statement.
                // Remove leading trivia from 'leftOfAssignment' as it should have been moved to 'newAssignmentTarget'.
                leftOfAssignment = leftOfAssignment.WithoutLeadingTrivia();
                return editor.Generator.AssignmentStatement(newAssignmentTarget,
                    BinaryExpression(SyntaxKind.CoalesceExpression, leftOfAssignment, rightOfAssignment));
            }
        }
        else
        {
            // Case 3. Compound assignment not parented by an expression statement.
            var mappedBinaryExpressionKind = originalCompoundAssignment.Kind().MapCompoundAssignmentKindToBinaryExpressionKind();
            if (mappedBinaryExpressionKind == SyntaxKind.None)
            {
                return originalCompoundAssignment;
            }

            return BinaryExpression(mappedBinaryExpressionKind, leftOfAssignment, rightOfAssignment);
        }
    }

    protected override SyntaxNode GetReplacementNodeForVarPattern(SyntaxNode originalVarPattern, SyntaxNode newNameNode)
    {
        if (originalVarPattern is not VarPatternSyntax pattern)
            throw ExceptionUtilities.Unreachable();

        // If the replacement node is DiscardDesignationSyntax
        // then we need to just change the incoming var's pattern designation
        if (newNameNode is DiscardDesignationSyntax discardDesignation)
        {
            return pattern.WithDesignation(discardDesignation.WithTriviaFrom(pattern.Designation));
        }

        // Otherwise just return new node as a replacement.
        // This would be the default behavior if there was no special case described above
        return newNameNode;
    }
}
