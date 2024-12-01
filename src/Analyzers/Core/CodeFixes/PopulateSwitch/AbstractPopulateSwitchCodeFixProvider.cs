// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch;

internal abstract class AbstractPopulateSwitchCodeFixProvider<
    TSwitchOperation,
    TSwitchSyntax,
    TSwitchArmSyntax,
    TMemberAccessExpression>
    : SyntaxEditorBasedCodeFixProvider
    where TSwitchOperation : IOperation
    where TSwitchSyntax : SyntaxNode
    where TSwitchArmSyntax : SyntaxNode
    where TMemberAccessExpression : SyntaxNode
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

    protected AbstractPopulateSwitchCodeFixProvider(string diagnosticId)
        => FixableDiagnosticIds = [diagnosticId];

    protected abstract ITypeSymbol GetSwitchType(TSwitchOperation switchStatement);
    protected abstract ICollection<ISymbol> GetMissingEnumMembers(TSwitchOperation switchOperation);
    protected abstract bool HasNullSwitchArm(TSwitchOperation switchOperation);

    protected abstract TSwitchArmSyntax CreateSwitchArm(SyntaxGenerator generator, Compilation compilation, TMemberAccessExpression caseLabel);
    protected abstract TSwitchArmSyntax CreateNullSwitchArm(SyntaxGenerator generator, Compilation compilation);
    protected abstract TSwitchArmSyntax CreateDefaultSwitchArm(SyntaxGenerator generator, Compilation compilation);
    protected abstract int InsertPosition(TSwitchOperation switchOperation);
    protected abstract TSwitchSyntax InsertSwitchArms(SyntaxGenerator generator, TSwitchSyntax switchNode, int insertLocation, List<TSwitchArmSyntax> newArms);

    protected abstract void FixOneDiagnostic(
        Document document, SyntaxEditor editor, SemanticModel semanticModel,
        bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
        bool hasMissingCases, bool hasMissingDefaultCase,
        TSwitchSyntax switchNode, TSwitchOperation switchOperation);

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var properties = diagnostic.Properties;
        var missingCases = bool.Parse(properties[PopulateSwitchStatementHelpers.MissingCases]!);
        var missingDefaultCase = bool.Parse(properties[PopulateSwitchStatementHelpers.MissingDefaultCase]!);

        Debug.Assert(missingCases || missingDefaultCase);

        var document = context.Document;
        if (missingCases)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    AnalyzersResources.Add_missing_cases,
                    c => FixAsync(document, diagnostic,
                        addCases: true, addDefaultCase: false,
                        cancellationToken: c),
                    nameof(AnalyzersResources.Add_missing_cases)),
                context.Diagnostics);
        }

        if (missingDefaultCase)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixesResources.Add_default_case,
                    c => FixAsync(document, diagnostic,
                        addCases: false, addDefaultCase: true,
                        cancellationToken: c),
                    nameof(CodeFixesResources.Add_default_case)),
                context.Diagnostics);
        }

        if (missingCases && missingDefaultCase)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixesResources.Add_both,
                    c => FixAsync(document, diagnostic,
                        addCases: true, addDefaultCase: true,
                        cancellationToken: c),
                    nameof(CodeFixesResources.Add_both)),
                context.Diagnostics);
        }

        return Task.CompletedTask;
    }

    private Task<Document> FixAsync(
        Document document, Diagnostic diagnostic,
        bool addCases, bool addDefaultCase,
        CancellationToken cancellationToken)
    {
        return FixAllAsync(document, [diagnostic],
            addCases, addDefaultCase, cancellationToken);
    }

    private Task<Document> FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        bool addCases, bool addDefaultCase,
        CancellationToken cancellationToken)
    {
        return FixAllWithEditorAsync(document,
            editor => FixWithEditorAsync(document, editor, diagnostics, addCases, addDefaultCase, cancellationToken),
            cancellationToken);
    }

    private async Task FixWithEditorAsync(
        Document document, SyntaxEditor editor, ImmutableArray<Diagnostic> diagnostics,
        bool addCases, bool addDefaultCase,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            await FixOneDiagnosticAsync(
                document, editor, diagnostic, addCases, addDefaultCase,
                diagnostics.Length == 1, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task FixOneDiagnosticAsync(
        Document document, SyntaxEditor editor, Diagnostic diagnostic,
        bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
        CancellationToken cancellationToken)
    {
        var hasMissingCases = bool.Parse(diagnostic.Properties[PopulateSwitchStatementHelpers.MissingCases]!);
        var hasMissingDefaultCase = bool.Parse(diagnostic.Properties[PopulateSwitchStatementHelpers.MissingDefaultCase]!);

        var switchLocation = diagnostic.AdditionalLocations[0];
        var switchNode = switchLocation.FindNode(getInnermostNodeForTie: true, cancellationToken) as TSwitchSyntax;
        if (switchNode == null)
            return;

        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        // https://github.com/dotnet/roslyn/issues/40505
        var switchStatement = (TSwitchOperation)model.GetOperation(switchNode, cancellationToken)!;

        FixOneDiagnostic(
            document, editor, model, addCases, addDefaultCase, onlyOneDiagnostic,
            hasMissingCases, hasMissingDefaultCase, switchNode, switchStatement);
    }

    protected TSwitchSyntax UpdateSwitchNode(
        SyntaxEditor editor, SemanticModel semanticModel,
        bool addCases, bool addDefaultCase,
        bool hasMissingCases, bool hasMissingDefaultCase,
        TSwitchSyntax switchNode, TSwitchOperation switchOperation)
    {
        var enumType = GetSwitchType(switchOperation);
        var isNullable = false;

        if (enumType.IsNullable(out var underlyingType))
        {
            isNullable = true;
            enumType = underlyingType;
        }

        var generator = editor.Generator;

        var newArms = new List<TSwitchArmSyntax>();

        if (hasMissingCases && addCases)
        {
            var missingArms =
                from e in GetMissingEnumMembers(switchOperation)
                let caseLabel = (TMemberAccessExpression)generator.MemberAccessExpression(generator.TypeExpression(enumType), e.Name).WithAdditionalAnnotations(Simplifier.Annotation)
                select CreateSwitchArm(generator, semanticModel.Compilation, caseLabel);

            newArms.AddRange(missingArms);

            if (isNullable && !HasNullSwitchArm(switchOperation))
                newArms.Add(CreateNullSwitchArm(generator, semanticModel.Compilation));
        }

        if (hasMissingDefaultCase && addDefaultCase)
        {
            // Always add the default clause at the end.
            newArms.Add(CreateDefaultSwitchArm(generator, semanticModel.Compilation));
        }

        var insertLocation = InsertPosition(switchOperation);

        var newSwitchNode = InsertSwitchArms(generator, switchNode, insertLocation, newArms)
            .WithAdditionalAnnotations(Formatter.Annotation);
        return newSwitchNode;
    }

    protected static void AddMissingBraces(
        Document document,
        ref SyntaxNode root,
        ref TSwitchSyntax switchNode)
    {
        // Parsing of the switch may have caused imbalanced braces.  i.e. the switch
        // may have consumed a brace that was intended for a higher level construct.
        // So balance the tree first, then do the switch replacement.
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        syntaxFacts.AddFirstMissingCloseBrace(
            root, switchNode, out var newRoot, out var newSwitchNode);

        root = newRoot;
        switchNode = newSwitchNode;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        // If the user is performing a fix-all, then fix up all the issues we see. i.e.
        // add missing cases and missing 'default' cases for any switches we reported an
        // issue on.
        return FixWithEditorAsync(document, editor, diagnostics,
            addCases: true, addDefaultCase: true,
            cancellationToken: cancellationToken);
    }
}
