// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseType;

internal abstract class AbstractUseTypeCodeRefactoringProvider : CodeRefactoringProvider
{
    protected abstract string Title { get; }
    protected abstract Task HandleDeclarationAsync(Document document, SyntaxEditor editor, TypeSyntax type, CancellationToken cancellationToken);
    protected abstract TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);
    protected abstract TypeStyleResult AnalyzeTypeName(TypeSyntax typeName, SemanticModel semanticModel, CSharpSimplifierOptions options, CancellationToken cancellationToken);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
        {
            return;
        }

        var declaration = await GetDeclarationAsync(context).ConfigureAwait(false);
        if (declaration == null)
        {
            return;
        }

        Debug.Assert(declaration.Kind() is SyntaxKind.VariableDeclaration or SyntaxKind.ForEachStatement or SyntaxKind.DeclarationExpression);

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var declaredType = FindAnalyzableType(declaration, semanticModel, cancellationToken);
        if (declaredType == null)
        {
            return;
        }

        var simplifierOptions = (CSharpSimplifierOptions)await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);
        var typeStyle = AnalyzeTypeName(declaredType, semanticModel, simplifierOptions, cancellationToken);
        if (typeStyle.IsStylePreferred && typeStyle.Notification.Severity != ReportDiagnostic.Suppress)
        {
            // the analyzer would handle this.  So we do not.
            return;
        }

        if (!typeStyle.CanConvert())
        {
            return;
        }

        context.RegisterRefactoring(
            CodeAction.Create(
                Title,
                c => UpdateDocumentAsync(document, declaredType, c),
                Title),
            declaredType.Span);
    }

    private static async Task<SyntaxNode> GetDeclarationAsync(CodeRefactoringContext context)
    {
        // We want to provide refactoring for changing the Type of newly introduced variables in following cases:
        // - DeclarationExpressionSyntax: `"42".TryParseInt32(out var number)`
        // - VariableDeclarationSyntax: General field / variable declaration statement `var number = 42`
        // - ForEachStatementSyntax: The variable that gets introduced by foreach `foreach(var number in numbers)`
        //
        // In addition to providing the refactoring when the whole node (i.e. the node that introduces the new variable) in question is selected 
        // we also want to enable it when only the type node is selected because this refactoring changes the type. We still have to make sure 
        // we're only working on TypeNodes for in above-mentioned situations.
        //
        // For foreach we need to guard against selecting just the expression because it is also of type `TypeSyntax`.

        var declNode = await context.TryGetRelevantNodeAsync<DeclarationExpressionSyntax>().ConfigureAwait(false);
        if (declNode != null)
            return declNode;

        var variableNode = await context.TryGetRelevantNodeAsync<VariableDeclarationSyntax>().ConfigureAwait(false);
        if (variableNode != null)
            return variableNode;

        // `ref var` is a bit of an interesting construct.  'ref' looks like a modifier, but is actually a
        // type-syntax.  Ensure the user can get the feature anywhere on this construct
        var type = await context.TryGetRelevantNodeAsync<TypeSyntax>().ConfigureAwait(false);
        var typeParent = type?.Parent;
        if (typeParent is RefTypeSyntax refType)
            type = refType;

        if (type?.Parent is VariableDeclarationSyntax)
            return type.Parent;

        var foreachStatement1 = await context.TryGetRelevantNodeAsync<ForEachStatementSyntax>().ConfigureAwait(false);
        if (foreachStatement1 != null)
            return foreachStatement1;

        if (type?.Parent is DeclarationExpressionSyntax or VariableDeclarationSyntax)
            return type.Parent;

        if (type?.Parent is ForEachStatementSyntax foreachStatement2 &&
            foreachStatement2.Type == type)
        {
            return foreachStatement2;
        }

        return null;
    }

    private async Task<Document> UpdateDocumentAsync(Document document, TypeSyntax type, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        await HandleDeclarationAsync(document, editor, type, cancellationToken).ConfigureAwait(false);

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }
}
