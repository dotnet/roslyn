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
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter;

using static InitializeParameterHelpersCore;

internal abstract class AbstractAddParameterCheckCodeRefactoringProvider<
    TTypeDeclarationSyntax,
    TParameterSyntax,
    TStatementSyntax,
    TExpressionSyntax,
    TBinaryExpressionSyntax,
    TSimplifierOptions> : AbstractInitializeParameterCodeRefactoringProvider<
        TTypeDeclarationSyntax,
        TParameterSyntax,
        TStatementSyntax,
        TExpressionSyntax>
    where TTypeDeclarationSyntax : SyntaxNode
    where TParameterSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
    where TBinaryExpressionSyntax : TExpressionSyntax
    where TSimplifierOptions : SimplifierOptions
{
    protected abstract bool CanOffer(SyntaxNode body);
    protected abstract bool PrefersThrowExpression(TSimplifierOptions options);
    protected abstract string EscapeResourceString(string input);
    protected abstract TStatementSyntax CreateParameterCheckIfStatement(TExpressionSyntax condition, TStatementSyntax ifTrueStatement, TSimplifierOptions options);

    protected override async Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(
        Document document,
        SyntaxNode functionDeclaration,
        IMethodSymbol methodSymbol,
        IBlockOperation? blockStatementOpt,
        ImmutableArray<SyntaxNode> listOfParameterNodes,
        TextSpan parameterSpan,
        CancellationToken cancellationToken)
    {
        // List to keep track of the valid parameters
        var listOfParametersOrdinals = new List<int>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var parameterNode in listOfParameterNodes)
        {
            var parameter = (IParameterSymbol)semanticModel.GetRequiredDeclaredSymbol(parameterNode, cancellationToken);
            if (ParameterValidForNullCheck(document, parameter, semanticModel, blockStatementOpt, cancellationToken))
                listOfParametersOrdinals.Add(parameter.Ordinal);
        }

        // Min 2 parameters to offer the refactoring
        if (listOfParametersOrdinals.Count < 2)
            return [];

        // Great.  The list has parameters that need null checks. Offer to add null checks for all.
        return [CodeAction.Create(
            FeaturesResources.Add_null_checks_for_all_parameters,
            c => UpdateDocumentForRefactoringAsync(document, blockStatementOpt, listOfParametersOrdinals, parameterSpan, c),
            nameof(FeaturesResources.Add_null_checks_for_all_parameters))];
    }

    protected override async Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(
        Document document,
        TParameterSyntax parameterSyntax,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol methodSymbol,
        IBlockOperation? blockStatementOpt,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Only should provide null-checks for reference types and nullable types.
        if (!ParameterValidForNullCheck(document, parameter, semanticModel, blockStatementOpt, cancellationToken))
            return [];

        var simplifierOptions = (TSimplifierOptions)await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Great.  There was no null check.  Offer to add one.
        using var result = TemporaryArray<CodeAction>.Empty;
        result.Add(CodeAction.Create(
            FeaturesResources.Add_null_check,
            cancellationToken => AddNullCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatementOpt, simplifierOptions, cancellationToken),
            nameof(FeaturesResources.Add_null_check)));

        // Also, if this was a string, offer to add the special checks to string.IsNullOrEmpty and
        // string.IsNullOrWhitespace.
        if (parameter.Type.SpecialType == SpecialType.System_String)
        {
            result.Add(CodeAction.Create(
                FeaturesResources.Add_string_IsNullOrEmpty_check,
                cancellationToken => AddStringCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatementOpt, nameof(string.IsNullOrEmpty), simplifierOptions, cancellationToken),
                nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)));

            result.Add(CodeAction.Create(
                FeaturesResources.Add_string_IsNullOrWhiteSpace_check,
                cancellationToken => AddStringCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatementOpt, nameof(string.IsNullOrWhiteSpace), simplifierOptions, cancellationToken),
                nameof(FeaturesResources.Add_string_IsNullOrWhiteSpace_check)));
        }

        return result.ToImmutableAndClear();
    }

    private async Task<Document> UpdateDocumentForRefactoringAsync(
        Document document,
        IBlockOperation? blockStatementOpt,
        List<int> listOfParametersOrdinals,
        TextSpan parameterSpan,
        CancellationToken cancellationToken)
    {
        TSimplifierOptions? lazySimplifierOptions = null;

        foreach (var index in listOfParametersOrdinals)
        {
            // Updates functionDeclaration and uses it to get the first valid ParameterNode using the ordinals (index).
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var firstParameterNode = (TParameterSyntax)root.FindNode(parameterSpan);
            var functionDeclaration = firstParameterNode.FirstAncestorOrSelf<SyntaxNode>(IsFunctionDeclaration);
            if (functionDeclaration == null)
                continue;

            var generator = SyntaxGenerator.GetGenerator(document);
            var parameterNodes = (IReadOnlyList<TParameterSyntax>)generator.GetParameters(functionDeclaration);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var (parameterSyntax, parameter) = GetParameterAtOrdinal(index, parameterNodes, semanticModel, cancellationToken);
            if (parameter == null)
                continue;
            Contract.ThrowIfNull(parameterSyntax);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            if (!CanOfferRefactoring(functionDeclaration, semanticModel, syntaxFacts, cancellationToken, out blockStatementOpt))
                continue;

            lazySimplifierOptions ??= (TSimplifierOptions)await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);

            // If parameter is a string, default check would be IsNullOrEmpty. This is because IsNullOrEmpty is more
            // commonly used in this regard according to telemetry and UX testing.
            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                document = await AddStringCheckAsync(document, parameter, functionDeclaration, (IMethodSymbol)parameter.ContainingSymbol, blockStatementOpt, nameof(string.IsNullOrEmpty), lazySimplifierOptions, cancellationToken).ConfigureAwait(false);
                continue;
            }

            // For all other parameters, add null check - updates document
            document = await AddNullCheckAsync(document, parameter, functionDeclaration,
                (IMethodSymbol)parameter.ContainingSymbol, blockStatementOpt, lazySimplifierOptions, cancellationToken).ConfigureAwait(false);
        }

        return document;
    }

    private static (TParameterSyntax?, IParameterSymbol?) GetParameterAtOrdinal(int index, IReadOnlyList<TParameterSyntax> parameterNodes, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        foreach (var parameterNode in parameterNodes)
        {
            var parameter = (IParameterSymbol)semanticModel.GetRequiredDeclaredSymbol(parameterNode, cancellationToken);
            if (index == parameter.Ordinal)
                return (parameterNode, parameter);
        }

        return default;
    }

    private static bool ContainsNullCoalesceCheck(
        ISyntaxFactsService syntaxFacts, SemanticModel semanticModel,
        IOperation statement, IParameterSymbol parameter,
        CancellationToken cancellationToken)
    {
        // Look for anything in this statement of the form "p ?? throw ...".
        // If so, we'll consider this parameter checked for null and we can stop immediately.

        var syntax = statement.Syntax;
        foreach (var coalesceNode in syntax.DescendantNodes().OfType<TBinaryExpressionSyntax>())
        {
            var operation = semanticModel.GetOperation(coalesceNode, cancellationToken);
            if (operation is ICoalesceOperation coalesceExpression)
            {
                if (IsParameterReference(coalesceExpression.Value, parameter) &&
                    syntaxFacts.IsThrowExpression(coalesceExpression.WhenNull.Syntax))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsIfNullCheck(IOperation statement, IParameterSymbol parameter)
    {
        if (statement is IConditionalOperation ifStatement)
        {
            var condition = ifStatement.Condition;
            condition = condition.UnwrapImplicitConversion();

            if (condition is IBinaryOperation binaryOperator)
            {
                // Look for code of the form "if (p == null)" or "if (null == p)"
                if (IsNullCheck(binaryOperator.LeftOperand, binaryOperator.RightOperand, parameter) ||
                    IsNullCheck(binaryOperator.RightOperand, binaryOperator.LeftOperand, parameter))
                {
                    return true;
                }
            }
            else if (condition is IIsPatternOperation isPatternOperation &&
                     isPatternOperation.Pattern is IConstantPatternOperation constantPattern)
            {
                // Look for code of the form "if (p is null)"
                if (IsNullCheck(constantPattern.Value, isPatternOperation.Value, parameter))
                    return true;
            }
            else if (parameter.Type.SpecialType == SpecialType.System_String &&
                     IsStringCheck(condition, parameter))
            {
                return true;
            }
        }

        return false;
    }

    protected bool ParameterValidForNullCheck(Document document, IParameterSymbol parameter, SemanticModel semanticModel,
        IBlockOperation? blockStatementOpt, CancellationToken cancellationToken)
    {
        if (parameter.Type.IsReferenceType)
        {
            // Don't add null checks to things explicitly declared nullable
            if (parameter.Type.NullableAnnotation == NullableAnnotation.Annotated)
                return false;
        }
        else if (!parameter.Type.IsNullable())
        {
            return false;
        }

        if (parameter.RefKind == RefKind.Out)
            return false;

        if (parameter.IsDiscard)
            return false;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // Look for an existing "if (p == null)" statement, or "p ?? throw" check.  If we already
        // have one, we don't want to offer to generate a new null check.
        //
        // Note: we only check the top level statements of the block.  I think that's sufficient
        // as this will catch the 90% case, while not being that bad an experience even when 
        // people do strange things in their constructors.
        if (blockStatementOpt != null)
        {
            if (!CanOffer(blockStatementOpt.Syntax))
                return false;

            foreach (var statement in blockStatementOpt.Operations)
            {
                if (IsIfNullCheck(statement, parameter))
                    return false;

                if (ContainsNullCoalesceCheck(
                        syntaxFacts, semanticModel, statement,
                        parameter, cancellationToken))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsStringCheck(IOperation condition, IParameterSymbol parameter)
    {
        if (condition is IInvocationOperation invocation &&
            invocation.Arguments.Length == 1 &&
            IsParameterReference(invocation.Arguments[0].Value, parameter))
        {
            var targetMethod = invocation.TargetMethod;
            if (targetMethod?.Name is nameof(string.IsNullOrEmpty) or nameof(string.IsNullOrWhiteSpace))
                return targetMethod.ContainingType.SpecialType == SpecialType.System_String;
        }

        return false;
    }

    private static bool IsNullCheck(IOperation operand1, IOperation operand2, IParameterSymbol parameter)
        => operand1.UnwrapImplicitConversion().IsNullLiteral() && IsParameterReference(operand2, parameter);

    private async Task<Document> AddNullCheckAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        TSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        // First see if we can convert a statement of the form "this.s = s" into "this.s = s ?? throw ...".
        var modifiedDocument = await TryAddNullCheckToAssignmentAsync(
            document, parameter, blockStatement, options, cancellationToken).ConfigureAwait(false);

        if (modifiedDocument != null)
            return modifiedDocument;

        // If we can't, then just offer to add an "if (s == null)" statement.
        return await AddNullCheckStatementAsync(
            document, parameter, functionDeclaration, method, blockStatement,
            (s, g) => CreateNullCheckStatement(s, g, parameter, options),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Document> AddStringCheckAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatementOpt,
        string methodName,
        TSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        return await AddNullCheckStatementAsync(
            document, parameter, functionDeclaration, method, blockStatementOpt,
            (s, g) => CreateStringCheckStatement(s.Compilation, g, parameter, methodName, options),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> AddNullCheckStatementAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        Func<SemanticModel, SyntaxGenerator, TStatementSyntax> generateNullCheck,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);
        var nullCheckStatement = generateNullCheck(semanticModel, editor.Generator);

        // We may be inserting a statement into a single-line container.  In that case,
        // we don't want the formatting engine to think this construct should stay single-line
        // so add a newline after the check to help dissuade it from thinking we should stay
        // on a single line.
        nullCheckStatement = nullCheckStatement.WithAppendedTrailingTrivia(
            editor.Generator.ElasticCarriageReturnLineFeed);

        // Find a good location to add the null check. In general, we want the order of checks
        // and assignments in the constructor to match the order of parameters in the method
        // signature.
        var statementToAddAfter = GetStatementToAddNullCheckAfter(
            semanticModel, parameter, blockStatement, cancellationToken);

        var initializeParameterService = document.GetRequiredLanguageService<IInitializeParameterService>();
        initializeParameterService.InsertStatement(editor, functionDeclaration, method.ReturnsVoid, statementToAddAfter, nullCheckStatement);

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }

    private TStatementSyntax CreateNullCheckStatement(SemanticModel semanticModel, SyntaxGenerator generator, IParameterSymbol parameter, TSimplifierOptions options)
        => CreateParameterCheckIfStatement(
            (TExpressionSyntax)generator.CreateNullCheckExpression(generator.SyntaxGeneratorInternal, semanticModel, parameter.Name),
            (TStatementSyntax)generator.CreateThrowArgumentNullExceptionStatement(semanticModel.Compilation, parameter),
            options);

    private TStatementSyntax CreateStringCheckStatement(
        Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter, string methodName, TSimplifierOptions options)
    {
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // generates: if (string.IsXXX(s)) throw new ArgumentException("message", nameof(s))
        var condition = (TExpressionSyntax)generator.InvocationExpression(
                            generator.MemberAccessExpression(
                                generator.TypeExpression(stringType),
                                generator.IdentifierName(methodName)),
                            generator.Argument(generator.IdentifierName(parameter.Name)));
        var throwStatement = (TStatementSyntax)generator.ThrowStatement(CreateArgumentException(compilation, generator, parameter, methodName));

        return CreateParameterCheckIfStatement(condition, throwStatement, options);
    }

    private static SyntaxNode? GetStatementToAddNullCheckAfter(
        SemanticModel semanticModel,
        IParameterSymbol parameter,
        IBlockOperation? blockStatement,
        CancellationToken cancellationToken)
    {
        if (blockStatement == null)
            return null;

        var methodSymbol = (IMethodSymbol)parameter.ContainingSymbol;
        var parameterIndex = methodSymbol.Parameters.IndexOf(parameter);

        // look for an existing check for a parameter that comes before us.
        // If we find one, we'll add ourselves after that parameter check.
        for (var i = parameterIndex - 1; i >= 0; i--)
        {
            var checkStatement = TryFindParameterCheckStatement(
                semanticModel, methodSymbol.Parameters[i], blockStatement, cancellationToken);
            if (checkStatement != null)
                return checkStatement.Syntax;
        }

        // look for an existing check for a parameter that comes before us.
        // If we find one, we'll add ourselves after that parameter check.
        for (var i = parameterIndex + 1; i < methodSymbol.Parameters.Length; i++)
        {
            var checkStatement = TryFindParameterCheckStatement(
                semanticModel, methodSymbol.Parameters[i], blockStatement, cancellationToken);
            if (checkStatement != null)
            {
                var statementIndex = blockStatement.Operations.IndexOf(checkStatement);
                return statementIndex > 0 && blockStatement.Operations[statementIndex - 1] is { IsImplicit: false, Syntax: var priorSyntax }
                    ? priorSyntax
                    : null;
            }
        }

        // Just place the null check at the start of the block
        return null;
    }

    /// <summary>
    /// Tries to find an if-statement that looks like it is checking the provided parameter
    /// in some way.  If we find a match, we'll place our new null-check statement before/after
    /// this statement as appropriate.
    /// </summary>
    private static IOperation? TryFindParameterCheckStatement(
        SemanticModel semanticModel,
        IParameterSymbol parameterSymbol,
        IBlockOperation? blockStatement,
        CancellationToken cancellationToken)
    {
        if (blockStatement != null)
        {
            foreach (var statement in blockStatement.Operations)
            {
                // Ignore implicit code the compiler inserted at the top of the block (for example, the implicit
                // call to mybase.new() in VB).
                if (statement.IsImplicit)
                    continue;

                if (statement is IConditionalOperation ifStatement)
                {
                    if (ContainsParameterReference(semanticModel, ifStatement.Condition, parameterSymbol, cancellationToken))
                        return statement;

                    continue;
                }

                // Stop hunting after we hit something that isn't an if-statement
                break;
            }
        }

        return null;
    }

    private async Task<Document?> TryAddNullCheckToAssignmentAsync(
        Document document,
        IParameterSymbol parameter,
        IBlockOperation? blockStatement,
        TSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        // tries to convert "this.s = s" into "this.s = s ?? throw ...".  Only supported
        // in languages that have a throw-expression, and only if the user has set the
        // preference that they like throw-expressions.

        if (blockStatement == null)
            return null;

        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        if (!syntaxFacts.SupportsThrowExpression(syntaxTree.Options))
            return null;

        if (!PrefersThrowExpression(options))
            return null;

        // Look through all the top level statements in the block to see if we can
        // find an existing field/property assignment involving this parameter.
        var containingType = parameter.ContainingType;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        foreach (var statement in blockStatement.Operations)
        {
            if (IsFieldOrPropertyAssignment(statement, containingType, out var assignmentExpression) &&
                IsParameterReference(assignmentExpression.Value, parameter))
            {
                // Found one.  Convert it to a coalesce expression with an appropriate 
                // throw expression.
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var generator = SyntaxGenerator.GetGenerator(document);
                var coalesce = generator.CoalesceExpression(
                    assignmentExpression.Value.Syntax,
                    generator.ThrowExpression(
                        CreateArgumentNullException(compilation, generator, parameter)));

                var newRoot = root.ReplaceNode(assignmentExpression.Value.Syntax, coalesce);
                return document.WithSyntaxRoot(newRoot);
            }

            // Otherwise, if this statement references the parameter, then we need to stop looking.  We need the
            // null check to go before the parameter is otherwise used.
            if (ContainsParameterReference(semanticModel, statement, parameter, cancellationToken))
                return null;
        }

        return null;
    }

    private static SyntaxNode GetTypeNode(
        Compilation compilation, SyntaxGenerator generator, Type type)
    {
        var typeSymbol = compilation.GetTypeByMetadataName(type.FullName!);
        if (typeSymbol == null)
        {
            return generator.QualifiedName(
                generator.IdentifierName(nameof(System)),
                generator.IdentifierName(type.Name));
        }

        return generator.TypeExpression(typeSymbol);
    }

    private static SyntaxNode CreateArgumentNullException(
        Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter)
    {
        return generator.ObjectCreationExpression(
            GetTypeNode(compilation, generator, typeof(ArgumentNullException)),
            generator.NameOfExpression(generator.IdentifierName(parameter.Name)));
    }

    private SyntaxNode CreateArgumentException(
        Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter, string methodName)
    {
        var text = methodName switch
        {
            nameof(string.IsNullOrEmpty) => new LocalizableResourceString(nameof(FeaturesResources._0_cannot_be_null_or_empty), FeaturesResources.ResourceManager, typeof(FeaturesResources)).ToString(),
            nameof(string.IsNullOrWhiteSpace) => new LocalizableResourceString(nameof(FeaturesResources._0_cannot_be_null_or_whitespace), FeaturesResources.ResourceManager, typeof(FeaturesResources)).ToString(),
            _ => throw ExceptionUtilities.Unreachable(),
        };

        // The resource string is written to be shown in a UI and is not necessarily valid code, but we're
        // going to be putting it into a string literal so we need to escape quotes etc. to avoid syntax errors
        var escapedText = EscapeResourceString(text);

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var content);

        var nameofExpression = generator.NameOfExpression(generator.IdentifierName(parameter.Name));

        var textParts = GetPreAndPostTextParts(text);
        var escapedTextParts = GetPreAndPostTextParts(escapedText);
        if (textParts.pre is null)
        {
            Debug.Fail("Should have found {0} in the resource string.");
            content.Add(InterpolatedStringText(generator, escapedText, text));
        }
        else
        {
            content.Add(InterpolatedStringText(generator, escapedTextParts.pre!, textParts.pre));
            content.Add(generator.Interpolation(nameofExpression));
            content.Add(InterpolatedStringText(generator, escapedTextParts.post!, textParts.post!));
        }

        return generator.ObjectCreationExpression(
            GetTypeNode(compilation, generator, typeof(ArgumentException)),
            generator.InterpolatedStringExpression(
                generator.CreateInterpolatedStringStartToken(isVerbatim: false),
                content,
                generator.CreateInterpolatedStringEndToken()),
            nameofExpression);
    }

    private static (string? pre, string? post) GetPreAndPostTextParts(string text)
    {
        const string Placeholder = "{0}";

        var index = text.IndexOf(Placeholder);
        if (index < 0)
            return default;

        return (text[..index], text[(index + Placeholder.Length)..]);
    }

    private static SyntaxNode InterpolatedStringText(SyntaxGenerator generator, string content, string value)
        => generator.InterpolatedStringText(generator.InterpolatedStringTextToken(content, value));
}
