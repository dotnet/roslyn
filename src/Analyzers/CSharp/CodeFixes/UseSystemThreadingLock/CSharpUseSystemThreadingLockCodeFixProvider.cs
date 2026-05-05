// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.UseSystemThreadingLock;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseSystemThreadingLock), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpUseSystemThreadingLockCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseSystemThreadingLockDiagnosticId];

    public override FixAllProvider? GetFixAllProvider()
#if CODE_STYLE
        => WellKnownFixAllProviders.BatchFixer;
#else
        => new CSharpUseSystemThreadingLockFixAllProvider();

#endif

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Location.FindNode(cancellationToken) is not VariableDeclaratorSyntax variableDeclarator)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpAnalyzersResources.Use_System_Threading_Lock,
                    cancellationToken => UseSystemThreadingLockAsync(document, semanticModel: null, variableDeclarator, cancellationToken),
                    nameof(CSharpAnalyzersResources.Use_primary_constructor)),
                diagnostic);
        }
    }

    private static async Task<Solution> UseSystemThreadingLockAsync(
        Document document,
        SemanticModel? semanticModel,
        VariableDeclaratorSyntax variableDeclarator,
        CancellationToken cancellationToken)
    {
        var solutionEditor = new SolutionEditor(document.Project.Solution);

        await UseSystemThreadingLockAsync(
            solutionEditor, document, semanticModel, variableDeclarator, cancellationToken).ConfigureAwait(false);

        return solutionEditor.GetChangedSolution();
    }

    private static async Task UseSystemThreadingLockAsync(
        SolutionEditor solutionEditor,
        Document document,
        SemanticModel? semanticModel,
        VariableDeclaratorSyntax variableDeclarator,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        semanticModel ??= await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel.Compilation;

        var lockType = compilation.GetBestTypeByMetadataName("System.Threading.Lock");
        if (lockType is null)
            return;

        if (variableDeclarator.Parent is not VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } variableDeclaration)
            return;

        if (semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken) is not IFieldSymbol field)
            return;

        var editor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var lockTypeExpression = generator
            .TypeExpression(lockType, addImport: true)
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);

        // Replace the return type, and initializer type in the field declaration itself.
        editor.ReplaceNode(
            variableDeclaration.Type,
            lockTypeExpression.WithTriviaFrom(variableDeclaration.Type));

        if (variableDeclarator.Initializer?.Value.WalkDownParentheses() is ObjectCreationExpressionSyntax objectCreationExpression)
        {
            editor.ReplaceNode(
                objectCreationExpression.Type,
                lockTypeExpression.WithTriviaFrom(objectCreationExpression.Type));
        }

        // Now, go find all the references to the field.  If any of them are initialized to a new value, then update
        // that as well.
        var fieldReferences = await SymbolFinder.FindReferencesAsync(field, solution, cancellationToken).ConfigureAwait(false);

        // Group these by document, so we can process a document all at once.  Limit to just documents from our starting
        // project as FindReferencesAsync will find references in all flavors of the starting project.
        var groups = fieldReferences
            .Where(r => Equals(r.Definition, field))
            .SelectMany(r => r.Locations)
            .Where(loc => loc.Document.Project == document.Project)
            .GroupBy(loc => loc.Document);
        foreach (var group in groups)
        {
            var groupDocument = group.Key;
            var groupDocumentEditor = await solutionEditor.GetDocumentEditorAsync(groupDocument.Id, cancellationToken).ConfigureAwait(false);

            foreach (var reference in group)
            {
                if (reference.IsImplicit)
                    continue;

                // Currently, the only case we know we want to fixup is if the field is being initialized to a new
                // value.  If the analyzer is updated to handle more cases, this will need to be updated as well.
                if (reference.Location.FindNode(cancellationToken) is not IdentifierNameSyntax node)
                    continue;

                ExpressionSyntax expression = node;
                if (expression.Parent is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name == expression)
                {
                    expression = memberAccess;
                }

                if (expression.Parent is not AssignmentExpressionSyntax assignment ||
                    assignment.Left != expression)
                {
                    continue;
                }

                if (assignment.Right.WalkDownParentheses() is not ObjectCreationExpressionSyntax objectCreation)
                    continue;

                groupDocumentEditor.ReplaceNode(
                    objectCreation.Type,
                    lockTypeExpression.WithTriviaFrom(objectCreation.Type));
            }
        }
    }
}
