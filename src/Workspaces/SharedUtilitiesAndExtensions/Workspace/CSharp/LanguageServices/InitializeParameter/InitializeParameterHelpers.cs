// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class InitializeParameterHelpers
{
    public static Argument<ExpressionSyntax> GetArgument(ArgumentSyntax argument)
        => new(argument.GetRefKind(), argument.NameColon?.Name.Identifier.ValueText, argument.Expression);

    public static async Task<Solution> AddAssignmentForPrimaryConstructorAsync(
        Document document,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        CancellationToken cancellationToken)
    {
        var project = document.Project;
        var solution = project.Solution;

        var solutionEditor = new SolutionEditor(solution);
        var initializer = EqualsValueClause(IdentifierName(parameter.Name.EscapeIdentifier()));

        // We're assigning the parameter to a field/prop.  Convert all existing references to this primary constructor
        // parameter (within this type) to refer to the field/prop now instead.
        await UpdateParameterReferencesAsync(
            solutionEditor, parameter, fieldOrProperty, cancellationToken).ConfigureAwait(false);

        // We're updating an exiting field/prop.
        if (fieldOrProperty is IPropertySymbol property)
        {
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var initializeParameterService = document.GetRequiredLanguageService<IInitializeParameterService>();
            var isThrowNotImplementedProperty = initializeParameterService.IsThrowNotImplementedProperty(
                compilation, property, cancellationToken);

            foreach (var syntaxRef in property.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(cancellationToken) is PropertyDeclarationSyntax propertyDeclaration)
                {
                    var editingDocument = solution.GetRequiredDocument(propertyDeclaration.SyntaxTree);
                    var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);

                    // If the user had a property that has 'throw NotImplementedException' in it, then remove those throws.
                    var newPropertyDeclaration = isThrowNotImplementedProperty ? RemoveThrowNotImplemented(propertyDeclaration) : propertyDeclaration;
                    editor.ReplaceNode(
                        propertyDeclaration,
                        newPropertyDeclaration.WithoutTrailingTrivia()
                            .WithSemicolonToken(SemicolonToken.WithTrailingTrivia(newPropertyDeclaration.GetTrailingTrivia()))
                            .WithInitializer(initializer));
                    break;
                }
            }
        }
        else if (fieldOrProperty is IFieldSymbol field)
        {
            foreach (var syntaxRef in field.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(cancellationToken) is VariableDeclaratorSyntax variableDeclarator)
                {
                    var editingDocument = solution.GetRequiredDocument(variableDeclarator.SyntaxTree);
                    var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);
                    editor.ReplaceNode(
                        variableDeclarator,
                        variableDeclarator.WithInitializer(initializer));
                    break;
                }
            }
        }

        return solutionEditor.GetChangedSolution();
    }

    public static async Task UpdateParameterReferencesAsync(
        SolutionEditor solutionEditor,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        CancellationToken cancellationToken)
    {
        var solution = solutionEditor.OriginalSolution;
        var namedType = parameter.ContainingType;
        var documents = namedType.DeclaringSyntaxReferences
            .Select(r => solution.GetRequiredDocument(r.SyntaxTree))
            .ToImmutableHashSet();

        var references = await SymbolFinder.FindReferencesAsync(parameter, solution, documents, cancellationToken).ConfigureAwait(false);
        var groups = references.SelectMany(static r => r.Locations.Where(loc => !loc.IsImplicit)).GroupBy(static loc => loc.Document);

        foreach (var group in groups)
        {
            var editor = await solutionEditor.GetDocumentEditorAsync(group.Key.Id, cancellationToken).ConfigureAwait(false);

            // We may hit a location multiple times due to how we do FAR for linked symbols, but each linked symbol is
            // allowed to report the entire set of references it think it is compatible with.  So ensure we're hitting
            // each location only once.
            foreach (var location in group.Distinct(LinkedFileReferenceLocationEqualityComparer.Instance))
            {
                var node = location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                if (node is IdentifierNameSyntax { Parent: not NameColonSyntax } identifierName &&
                    identifierName.Identifier.ValueText == parameter.Name)
                {
                    // we may have things like `new MyType(x: ...)` we don't want to update `x` there to 'X'
                    // just because we're generating a new property 'X' for the parameter to be assigned to.
                    editor.ReplaceNode(
                        identifierName,
                        IdentifierName(fieldOrProperty.Name.EscapeIdentifier()).WithTriviaFrom(identifierName));
                }
            }
        }
    }

    public static bool IsFunctionDeclaration(SyntaxNode node)
        => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;

    public static SyntaxNode GetBody(SyntaxNode functionDeclaration)
        => functionDeclaration switch
        {
            BaseMethodDeclarationSyntax methodDeclaration => (SyntaxNode?)methodDeclaration.Body ?? methodDeclaration.ExpressionBody!,
            LocalFunctionStatementSyntax localFunction => (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody!,
            AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.Body,
            _ => throw ExceptionUtilities.UnexpectedValue(functionDeclaration),
        };

    private static SyntaxToken? TryGetSemicolonToken(SyntaxNode functionDeclaration)
        => functionDeclaration switch
        {
            BaseMethodDeclarationSyntax methodDeclaration => methodDeclaration.SemicolonToken,
            LocalFunctionStatementSyntax localFunction => localFunction.SemicolonToken,
            AnonymousFunctionExpressionSyntax _ => null,
            _ => throw ExceptionUtilities.UnexpectedValue(functionDeclaration),
        };

    public static bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        => compilation.ClassifyConversion(source: source, destination: destination).IsImplicit;

    public static SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement)
        => blockStatement?.Syntax is BlockSyntax block
            ? block.Statements.LastOrDefault()
            : blockStatement?.Syntax;

    public static void InsertStatement(
        SyntaxEditor editor,
        SyntaxNode functionDeclaration,
        bool returnsVoid,
        SyntaxNode? statementToAddAfterOpt,
        StatementSyntax statement)
    {
        var body = GetBody(functionDeclaration);

        if (IsExpressionBody(body))
        {
            var semicolonToken = TryGetSemicolonToken(functionDeclaration) ?? SemicolonToken;

            if (!TryConvertExpressionBodyToStatement(body, semicolonToken, !returnsVoid, out var convertedStatement))
            {
                return;
            }

            // Add the new statement as the first/last statement of the new block 
            // depending if we were asked to go after something or not.
            editor.SetStatements(functionDeclaration, statementToAddAfterOpt == null
                ? [statement, convertedStatement]
                : [convertedStatement, statement]);
        }
        else if (body is BlockSyntax block)
        {
            // Look for the statement we were asked to go after.
            var indexToAddAfter = block.Statements.IndexOf(s => s == statementToAddAfterOpt);
            if (indexToAddAfter >= 0)
            {
                // If we find it, then insert the new statement after it.
                editor.InsertAfter(block.Statements[indexToAddAfter], statement);
            }
            else if (block.Statements.Count > 0)
            {
                // Otherwise, if we have multiple statements already, then insert ourselves
                // before the first one.
                editor.InsertBefore(block.Statements[0], statement);
            }
            else
            {
                // Otherwise, we have no statements in this block.  Add the new statement
                // as the single statement the block will have.
                Debug.Assert(block.Statements.Count == 0);
                editor.ReplaceNode(block, (currentBlock, _) => ((BlockSyntax)currentBlock).AddStatements(statement));
            }

            // If the block was on a single line before, the format it so that the formatting
            // engine will update it to go over multiple lines. Otherwise, we can end up in
            // the strange state where the { and } tokens stay where they were originally,
            // which will look very strange like:
            //
            //          a => {
            //              if (...) {
            //              } };
            if (CSharpSyntaxFacts.Instance.IsOnSingleLine(block, fullSpan: false))
            {
                editor.ReplaceNode(
                    block,
                    (currentBlock, _) => currentBlock.WithAdditionalAnnotations(Formatter.Annotation));
            }
        }
        else
        {
            editor.SetStatements(functionDeclaration, ImmutableArray.Create(statement));
        }
    }

    // either from an expression lambda or expression bodied member
    public static bool IsExpressionBody(SyntaxNode body)
        => body is ExpressionSyntax or ArrowExpressionClauseSyntax;

    public static bool TryConvertExpressionBodyToStatement(
        SyntaxNode body,
        SyntaxToken semicolonToken,
        bool createReturnStatementForExpression,
        [NotNullWhen(true)] out StatementSyntax? statement)
    {
        Debug.Assert(IsExpressionBody(body));

        return body switch
        {
            // If this is a => method, then we'll have to convert the method to have a block body.
            ArrowExpressionClauseSyntax arrowClause => arrowClause.TryConvertToStatement(semicolonToken, createReturnStatementForExpression, out statement),
            // must be an expression lambda
            ExpressionSyntax expression => expression.TryConvertToStatement(semicolonToken, createReturnStatementForExpression, out statement),
            _ => throw ExceptionUtilities.UnexpectedValue(body),
        };
    }

    public static SyntaxNode? GetAccessorBody(IMethodSymbol accessor, CancellationToken cancellationToken)
    {
        var node = accessor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        if (node is AccessorDeclarationSyntax accessorDeclaration)
            return accessorDeclaration.ExpressionBody ?? (SyntaxNode?)accessorDeclaration.Body;

        // `int Age => ...;`
        if (node is ArrowExpressionClauseSyntax arrowExpression)
            return arrowExpression;

        return null;
    }

    public static SyntaxNode RemoveThrowNotImplemented(SyntaxNode node)
        => node is PropertyDeclarationSyntax propertyDeclaration ? RemoveThrowNotImplemented(propertyDeclaration) : node;

    public static PropertyDeclarationSyntax RemoveThrowNotImplemented(PropertyDeclarationSyntax propertyDeclaration)
    {
        if (propertyDeclaration.ExpressionBody != null)
        {
            var result = propertyDeclaration
                .WithExpressionBody(null)
                .WithSemicolonToken(default)
                .AddAccessorListAccessors(SyntaxFactory
                    .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SemicolonToken))
                .WithTrailingTrivia(propertyDeclaration.SemicolonToken.TrailingTrivia)
                .WithAdditionalAnnotations(Formatter.Annotation);
            return result;
        }

        if (propertyDeclaration.AccessorList != null)
        {
            var accessors = propertyDeclaration.AccessorList.Accessors.Select(RemoveThrowNotImplemented);
            return propertyDeclaration.WithAccessorList(
                propertyDeclaration.AccessorList.WithAccessors([.. accessors]));
        }

        return propertyDeclaration;
    }

    private static AccessorDeclarationSyntax RemoveThrowNotImplemented(AccessorDeclarationSyntax accessorDeclaration)
    {
        var result = accessorDeclaration
            .WithExpressionBody(null)
            .WithBody(null)
            .WithSemicolonToken(SemicolonToken);

        return result.WithTrailingTrivia(accessorDeclaration.Body?.GetTrailingTrivia() ?? accessorDeclaration.SemicolonToken.TrailingTrivia);
    }
}
