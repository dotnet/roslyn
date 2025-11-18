// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

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
    private const string IsPrefix = "Is";
    private const string ThrowIfPrefix = "ThrowIf";

    private const string NullSuffix = "Null";
    private const string NullOrEmptySuffix = "NullOrEmpty";
    private const string NullOrWhiteSpaceSuffix = "NullOrWhiteSpace";

    private const string NegativeSuffix = "Negative";
    private const string NegativeOrZeroSuffix = "NegativeOrZero";

    private const string ThrowIfNullName = ThrowIfPrefix + NullSuffix;
    private const string ThrowIfNullOrEmptyName = ThrowIfPrefix + NullOrEmptySuffix;
    private const string ThrowIfNullOrWhiteSpaceName = ThrowIfPrefix + NullOrWhiteSpaceSuffix;

    private const string ThrowIfNegativeName = ThrowIfPrefix + NegativeSuffix;
    private const string ThrowIfNegativeOrZeroName = ThrowIfPrefix + NegativeOrZeroSuffix;

    protected abstract bool CanOffer(SyntaxNode body);
    protected abstract bool PrefersThrowExpression(TSimplifierOptions options);
    protected abstract string EscapeResourceString(string input);
    protected abstract TStatementSyntax CreateParameterCheckIfStatement(TExpressionSyntax condition, TStatementSyntax ifTrueStatement, TSimplifierOptions options);

    protected override async Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(
        Document document,
        SyntaxNode functionDeclaration,
        IMethodSymbol methodSymbol,
        IBlockOperation? blockStatement,
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
            if (ParameterValidForNullCheck(document, parameter, semanticModel, blockStatement, cancellationToken))
                listOfParametersOrdinals.Add(parameter.Ordinal);
        }

        // Min 2 parameters to offer the refactoring
        if (listOfParametersOrdinals.Count < 2)
            return [];

        // Great.  The list has parameters that need null checks. Offer to add null checks for all.
        return [CodeAction.Create(
            FeaturesResources.Add_null_checks_for_all_parameters,
            c => UpdateDocumentForRefactoringAsync(document, blockStatement, listOfParametersOrdinals, parameterSpan, c),
            nameof(FeaturesResources.Add_null_checks_for_all_parameters))];
    }

    protected override async Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(
        Document document,
        TParameterSyntax parameterSyntax,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol methodSymbol,
        IBlockOperation? blockStatement,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Only should provide null-checks for reference types and nullable types.
        if (ParameterValidForNullCheck(document, parameter, semanticModel, blockStatement, cancellationToken))
        {
            var simplifierOptions = (TSimplifierOptions)await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);

            // Great.  There was no null check.  Offer to add one.
            using var result = TemporaryArray<CodeAction>.Empty;
            result.Add(CodeAction.Create(
                FeaturesResources.Add_null_check,
                cancellationToken => AddNullCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatement, simplifierOptions, cancellationToken),
                nameof(FeaturesResources.Add_null_check)));

            // Also, if this was a string, offer to add the special checks to string.IsNullOrEmpty and
            // string.IsNullOrWhitespace.
            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                result.Add(CodeAction.Create(
                    string.Format(FeaturesResources.Add_0_check, "string.IsNullOrEmpty"),
                    cancellationToken => AddStringCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatement, NullOrEmptySuffix, simplifierOptions, cancellationToken),
                    "Add_string_IsNullOrEmpty_check"));

                result.Add(CodeAction.Create(
                    string.Format(FeaturesResources.Add_0_check, "string.IsNullOrWhiteSpace"),
                    cancellationToken => AddStringCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatement, NullOrWhiteSpaceSuffix, simplifierOptions, cancellationToken),
                    "Add_string_IsNullOrWhiteSpace_check"));
            }

            return result.ToImmutableAndClear();
        }

        var compilation = semanticModel.Compilation;

        if (ParameterValidForNumericCheck(parameter, blockStatement))
        {
            var simplifierOptions = (TSimplifierOptions)await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);

            var negativeCheckAction = CodeAction.Create(
                FeaturesResources.Add_negative_value_check,
                cancellationToken => AddNumericCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatement, includeZero: false, simplifierOptions, cancellationToken),
                nameof(FeaturesResources.Add_negative_value_check));

            var negativeOrZeroCheckAction = CodeAction.Create(
                FeaturesResources.Add_negative_value_or_zero_check,
                cancellationToken => AddNumericCheckAsync(document, parameter, functionDeclaration, methodSymbol, blockStatement, includeZero: true, simplifierOptions, cancellationToken),
                nameof(FeaturesResources.Add_negative_value_or_zero_check));

            return [negativeCheckAction, negativeOrZeroCheckAction];
        }

        // Provide 'Enum.IsDefined' check for suitable enum parameters
        if (ParameterValidForEnumIsDefinedCheck(parameter, compilation, blockStatement))
        {
            var action = CodeAction.Create(
                string.Format(FeaturesResources.Add_0_check, "Enum.IsDefined"),
                cancellationToken => AddEnumIsDefinedCheckStatementAsync(document, parameter, functionDeclaration, methodSymbol, blockStatement, cancellationToken),
                "Add_Enum_IsDefined_check");

            return [action];
        }

        return [];
    }

    private async Task<Document> UpdateDocumentForRefactoringAsync(
        Document document,
        IBlockOperation? blockStatement,
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

            if (!CanOfferRefactoring(functionDeclaration, semanticModel, syntaxFacts, cancellationToken, out blockStatement))
                continue;

            lazySimplifierOptions ??= (TSimplifierOptions)await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);

            // If parameter is a string, default check would be IsNullOrEmpty. This is because IsNullOrEmpty is more
            // commonly used in this regard according to telemetry and UX testing.
            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                document = await AddStringCheckAsync(document, parameter, functionDeclaration, (IMethodSymbol)parameter.ContainingSymbol, blockStatement, NullOrEmptySuffix, lazySimplifierOptions, cancellationToken).ConfigureAwait(false);
                continue;
            }

            // For all other parameters, add null check - updates document
            document = await AddNullCheckAsync(document, parameter, functionDeclaration,
                (IMethodSymbol)parameter.ContainingSymbol, blockStatement, lazySimplifierOptions, cancellationToken).ConfigureAwait(false);
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

    private static bool IsIfNumericCheck(IOperation statement, IParameterSymbol parameter)
    {
        if (statement is IConditionalOperation ifStatement)
        {
            var condition = ifStatement.Condition.UnwrapImplicitConversion();

            // parameter < num
            // parameter <= num
            // parameter > num
            // parameter >= num
            // num < parameter
            // num <= parameter
            // num > parameter
            // num >= parameter
            if (condition is IBinaryOperation { OperatorKind: BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual } binaryOperator &&
                IsNumericCheckOperands(binaryOperator.LeftOperand, binaryOperator.RightOperand, parameter))
            {
                return true;
            }
            // parameter is < num
            // parameter is <= num
            // parameter is > num
            // parameter is >= num
            else if (condition is IIsPatternOperation
            {
                Pattern: IRelationalPatternOperation
                {
                    OperatorKind: BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual,
                    Value: ILiteralOperation value
                }
            } && value.Type.IsNumericType())
            {
                return true;
            }
        }

        return false;

        static bool IsNumericCheckOperands(IOperation operand1, IOperation operand2, IParameterSymbol parameter)
        {
            return (IsParameterReference(operand1, parameter) && operand2.IsNumericLiteral()) ||
                   (operand1.IsNumericLiteral() && IsParameterReference(operand2, parameter));
        }
    }

    private bool ParameterValidForNullCheck(Document document, IParameterSymbol parameter, SemanticModel semanticModel,
        IBlockOperation? blockStatement, CancellationToken cancellationToken)
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
        if (blockStatement != null)
        {
            if (!CanOffer(blockStatement.Syntax))
                return false;

            foreach (var statement in blockStatement.Operations)
            {
                if (IsIfNullCheck(statement, parameter))
                    return false;

                if (IsAnyThrowIfNullInvocation(statement, parameter))
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

    private bool ParameterValidForNumericCheck(IParameterSymbol parameter, IBlockOperation? blockStatement)
    {
        if (parameter.RefKind == RefKind.Out)
            return false;

        if (parameter.IsDiscard)
            return false;

        if (!parameter.Type.IsSignedIntegralType())
            return false;

        if (blockStatement is not null)
        {
            if (!CanOffer(blockStatement.Syntax))
                return false;

            foreach (var statement in blockStatement.Operations)
            {
                if (IsIfNumericCheck(statement, parameter))
                    return false;

                if (IsAnyThrowIfNumericCheckInvocation(statement, parameter))
                    return false;
            }
        }

        return true;
    }

    private static (IMethodSymbol? GenericOverload, IMethodSymbol? NonGenericOverload) GetEnumIsDefinedMethods(Compilation compilation)
    {
        var enumType = compilation.GetSpecialType(SpecialType.System_Enum);
        var enumIsDefinedMembers = enumType.GetMembers(nameof(Enum.IsDefined));
        var enumIsDefinedGenericMethod = (IMethodSymbol?)enumIsDefinedMembers.FirstOrDefault(m => m is IMethodSymbol { IsStatic: true, Arity: 1, Parameters.Length: 1 });
        var enumIsDefinedNonGenericMethod = (IMethodSymbol?)enumIsDefinedMembers.FirstOrDefault(m => m is IMethodSymbol { IsStatic: true, Arity: 0, Parameters.Length: 2 });
        return (enumIsDefinedGenericMethod, enumIsDefinedNonGenericMethod);
    }

    private bool ParameterValidForEnumIsDefinedCheck(IParameterSymbol parameter, Compilation compilation, IBlockOperation? blockStatement)
    {
        if (parameter.RefKind == RefKind.Out)
            return false;

        if (parameter.IsDiscard)
            return false;

        var parameterType = parameter.Type;

        if (parameterType.TypeKind != TypeKind.Enum)
            return false;

        var flagsAttributeType = compilation.GetBestTypeByMetadataName(typeof(FlagsAttribute).FullName!);
        if (parameterType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, flagsAttributeType)))
            return false;

        if (blockStatement is not null)
        {
            if (!CanOffer(blockStatement.Syntax))
                return false;

            var (enumIsDefinedGenericMethod, enumIsDefinedNonGenericMethod) = GetEnumIsDefinedMethods(compilation);

            foreach (var statement in blockStatement.Operations)
            {
                if (IsEnumIsDefinedCheck(statement, parameter, enumIsDefinedGenericMethod, enumIsDefinedNonGenericMethod))
                    return false;
            }
        }

        return true;
    }

    private static bool IsEnumIsDefinedCheck(IOperation statement, IParameterSymbol parameter, IMethodSymbol? enumIsDefinedGenericMethod, IMethodSymbol? enumIsDefinedNonGenericMethod)
    {
        if (statement is IConditionalOperation ifStatement)
        {
            var condition = ifStatement.Condition;
            condition = condition.UnwrapImplicitConversion();

            if (condition is not IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, Operand: IInvocationOperation invocation })
                return false;

            var method = invocation.TargetMethod;

            if (method.OriginalDefinition.Equals(enumIsDefinedGenericMethod, SymbolEqualityComparer.Default) &&
                IsParameterReference(invocation.Arguments[0].Value, parameter))
            {
                return true;
            }

            if (method.Equals(enumIsDefinedNonGenericMethod, SymbolEqualityComparer.Default) &&
                IsParameterReference(invocation.Arguments[1].Value, parameter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAnyThrowInvocation(IOperation statement, IParameterSymbol? parameter, ReadOnlySpan<string> possibleTypeNames, Func<string, bool> methodNamePredicate)
    {
        if (statement is IExpressionStatementOperation
            {
                Operation: IInvocationOperation
                {
                    TargetMethod:
                    {
                        ContainingType.Name: var containingTypeName,
                        Name: var methodName,
                    },
                    Arguments: [{ Value: var argumentValue }, ..]
                }
            } &&
            possibleTypeNames.Contains(containingTypeName) &&
            methodNamePredicate(methodName))
        {
            if (argumentValue.UnwrapImplicitConversion() is IParameterReferenceOperation parameterReference)
                return parameter is null || parameter.Equals(parameterReference.Parameter);
        }

        return false;
    }

    private static bool IsAnyThrowIfNullInvocation(IOperation statement, IParameterSymbol? parameter)
    {
        return IsAnyThrowInvocation(statement, parameter, [nameof(ArgumentNullException), nameof(ArgumentException)], m => m is ThrowIfNullName or ThrowIfNullOrEmptyName or ThrowIfNullOrWhiteSpaceName);
    }

    private static bool IsAnyThrowIfNumericCheckInvocation(IOperation statement, IParameterSymbol? parameter)
    {
        return IsAnyThrowInvocation(statement, parameter, [nameof(ArgumentOutOfRangeException)], m => m.StartsWith(ThrowIfPrefix));
    }

    private static bool IsStringCheck(IOperation condition, IParameterSymbol parameter)
    {
        if (condition is IInvocationOperation { Arguments: [{ Value: var argumentValue }] } invocation &&
            IsParameterReference(argumentValue, parameter))
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
        return await AddCheckStatementAsync(
            document, parameter, functionDeclaration, method, blockStatement,
            (s, g) => CreateNullCheckStatement(s, g, parameter, options),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Document> AddStringCheckAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        string methodNameSuffix,
        TSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        return await AddCheckStatementAsync(
            document, parameter, functionDeclaration, method, blockStatement,
            (s, g) => CreateStringCheckStatement(s.Compilation, g, parameter, methodNameSuffix, options),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Document> AddNumericCheckAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        bool includeZero,
        TSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        return await AddCheckStatementAsync(
            document, parameter, functionDeclaration, method, blockStatement,
            (s, g) => CreateNumericCheckStatement(s.Compilation, g, parameter, includeZero, options),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> AddCheckStatementAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        Func<SemanticModel, SyntaxGenerator, TStatementSyntax> generateCheck,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);
        var checkStatement = generateCheck(semanticModel, editor.Generator);

        // We may be inserting a statement into a single-line container.  In that case,
        // we don't want the formatting engine to think this construct should stay single-line
        // so add a newline after the check to help dissuade it from thinking we should stay
        // on a single line.
        checkStatement = checkStatement.WithAppendedTrailingTrivia(
            editor.Generator.ElasticCarriageReturnLineFeed);

        // Find a good location to add the check. In general, we want the order of checks
        // and assignments in the constructor to match the order of parameters in the method
        // signature.
        var statementToAddAfter = GetStatementToAddCheckAfter(semanticModel, parameter, blockStatement, cancellationToken);

        var initializeParameterService = document.GetRequiredLanguageService<IInitializeParameterService>();
        initializeParameterService.InsertStatement(editor, functionDeclaration, method.ReturnsVoid, statementToAddAfter, checkStatement);

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> AddEnumIsDefinedCheckStatementAsync(
        Document document,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel.Compilation;

        var enumType = compilation.GetSpecialType(SpecialType.System_Enum);
        var enumIsDefinedGenericMethod = enumType.GetMembers(nameof(Enum.IsDefined)).FirstOrDefault(m => m is IMethodSymbol { IsStatic: true, Arity: 1, Parameters.Length: 1 });

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);
        var generator = editor.Generator;

        var parameterIdentifierName = generator.IdentifierName(parameter.Name);
        var typeOfParameterExpression = generator.TypeOfExpression(generator.TypeExpression(parameter.Type));

        SyntaxNode enumIsDefinedInvocation;
        if (enumIsDefinedGenericMethod is not null)
        {
            enumIsDefinedInvocation = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.TypeExpression(enumType),
                    generator.GenericName(enumIsDefinedGenericMethod.Name, parameter.Type)),
                parameterIdentifierName);
        }
        else
        {
            enumIsDefinedInvocation = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.TypeExpression(enumType),
                    nameof(Enum.IsDefined)),
                typeOfParameterExpression,
                parameterIdentifierName);
        }

        var finalCondition = generator.LogicalNotExpression(enumIsDefinedInvocation);

        var throwStatement = generator.ThrowStatement(
            generator.ObjectCreationExpression(
                GetTypeNode(compilation, generator, typeof(InvalidEnumArgumentException)),
                generator.NameOfExpression(parameterIdentifierName),
                generator.CastExpression(
                    compilation.GetSpecialType(SpecialType.System_Int32),
                    parameterIdentifierName),
                typeOfParameterExpression));

        var enumIsDefinedCheckStatement = generator.IfStatement(finalCondition, [throwStatement]);

        var initializeParameterService = document.GetRequiredLanguageService<IInitializeParameterService>();
        var statementToAddAfter = GetStatementToAddCheckAfter(semanticModel, parameter, blockStatement, cancellationToken);
        initializeParameterService.InsertStatement(editor, functionDeclaration, method.ReturnsVoid, statementToAddAfter, enumIsDefinedCheckStatement);

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }

    private TStatementSyntax CreateNullCheckStatement(SemanticModel semanticModel, SyntaxGenerator generator, IParameterSymbol parameter, TSimplifierOptions options)
    {
        var argumentNullExceptionType = semanticModel.Compilation.ArgumentNullExceptionType();
        if (parameter.Type.IsReferenceType && argumentNullExceptionType != null)
        {
            var throwIfNullMethod = argumentNullExceptionType
                .GetMembers(ThrowIfNullName)
                .FirstOrDefault(s => s is IMethodSymbol { Parameters: [{ Type.SpecialType: SpecialType.System_Object }, ..] });
            if (throwIfNullMethod != null)
            {
                return (TStatementSyntax)generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.TypeExpression(argumentNullExceptionType),
                        ThrowIfNullName),
                    generator.IdentifierName(parameter.Name)));
            }
        }

        return CreateParameterCheckIfStatement(
            (TExpressionSyntax)generator.CreateNullCheckExpression(generator.SyntaxGeneratorInternal, semanticModel, parameter.Name),
            (TStatementSyntax)generator.CreateThrowArgumentNullExceptionStatement(semanticModel.Compilation, parameter),
            options);
    }

    private TStatementSyntax CreateNumericCheckStatement(Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter, bool includeZero, TSimplifierOptions options)
    {
        var argumentOutOfRangeExceptionType = compilation.ArgumentOutOfRangeExceptionType();
        if (argumentOutOfRangeExceptionType is not null)
        {
            var throwMethodName = includeZero ? ThrowIfNegativeOrZeroName : ThrowIfNegativeName;
            var throwMethod = argumentOutOfRangeExceptionType
                .GetMembers(throwMethodName)
                .FirstOrDefault(s => s is IMethodSymbol { IsStatic: true, Arity: 1, Parameters.Length: 2 });
            if (throwMethod is not null)
            {
                // We found 'ThrowIfX' method. Generate 'ArgumentOutOfRangeException.ThrowIfNegative[OrZero](parameter);'
                return (TStatementSyntax)generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.TypeExpression(argumentOutOfRangeExceptionType),
                        throwMethodName),
                    generator.IdentifierName(parameter.Name)));
            }
        }

        // Generate 'manual check' like
        // if (parameter <[=] 0) throw new ArgumentOutOfRangeException(nameof(parameter), parameter, "message");
        var parameterNameExpression = generator.IdentifierName(parameter.Name);
        var zeroLiteralExpression = generator.LiteralExpression(0);
        var condition = includeZero
            ? generator.LessThanOrEqualExpression(parameterNameExpression, zeroLiteralExpression)
            : generator.LessThanExpression(parameterNameExpression, zeroLiteralExpression);

        var parameterNameOfExpression = generator.NameOfExpression(parameterNameExpression);
        var throwStatement = generator.ThrowStatement(
            generator.ObjectCreationExpression(
                GetTypeNode(compilation, generator, typeof(ArgumentOutOfRangeException)),
                parameterNameOfExpression,
                parameterNameExpression,
                CreateExceptionMessageArgument(includeZero
                    ? FeaturesResources._0_cannot_be_negative_or_zero
                    : FeaturesResources._0_cannot_be_negative, generator, parameterNameOfExpression)));

        return CreateParameterCheckIfStatement((TExpressionSyntax)condition, (TStatementSyntax)throwStatement, options);
    }

    private TStatementSyntax CreateStringCheckStatement(
        Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter, string methodNameSuffix, TSimplifierOptions options)
    {
        var argumentExceptionType = compilation.ArgumentExceptionType();
        if (argumentExceptionType != null)
        {
            var throwMethodName = ThrowIfPrefix + methodNameSuffix;
            var throwIfNullMethod = argumentExceptionType
                .GetMembers(throwMethodName)
                .FirstOrDefault(s => s is IMethodSymbol { Parameters: [{ Type.SpecialType: SpecialType.System_String }, ..] });
            if (throwIfNullMethod != null)
            {
                return (TStatementSyntax)generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.TypeExpression(argumentExceptionType),
                        throwMethodName),
                    generator.IdentifierName(parameter.Name)));
            }
        }

        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // generates: if (string.IsXXX(s)) throw new ArgumentException("message", nameof(s))
        var isMethodName = IsPrefix + methodNameSuffix;
        var condition = (TExpressionSyntax)generator.InvocationExpression(
            generator.MemberAccessExpression(
                generator.TypeExpression(stringType),
                generator.IdentifierName(isMethodName)),
            generator.Argument(generator.IdentifierName(parameter.Name)));
        var throwStatement = (TStatementSyntax)generator.ThrowStatement(
            CreateArgumentException(compilation, generator, parameter, isMethodName));

        return CreateParameterCheckIfStatement(condition, throwStatement, options);
    }

    private static SyntaxNode? GetStatementToAddCheckAfter(
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
    /// in some way.  If we find a match, we'll place our new check statement before/after
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
            var (enumIsDefinedGenericMethod, enumIsDefinedNonGenericMethod) = GetEnumIsDefinedMethods(semanticModel.Compilation);

            foreach (var statement in blockStatement.Operations)
            {
                // Ignore implicit code the compiler inserted at the top of the block (for example, the implicit
                // call to mybase.new() in VB).
                if (statement.IsImplicit)
                    continue;

                if (IsAnyThrowIfNullInvocation(statement, parameter: null))
                {
                    if (IsAnyThrowIfNullInvocation(statement, parameterSymbol))
                        return statement;

                    continue;
                }

                if (IsAnyThrowIfNumericCheckInvocation(statement, parameterSymbol) ||
                    IsIfNumericCheck(statement, parameterSymbol) ||
                    IsEnumIsDefinedCheck(statement, parameterSymbol, enumIsDefinedGenericMethod, enumIsDefinedNonGenericMethod))
                {
                    return statement;
                }

                if (statement is IConditionalOperation ifStatement)
                {
                    if (ContainsParameterReference(semanticModel, ifStatement.Condition, parameterSymbol, cancellationToken))
                        return statement;

                    continue;
                }

                // Stop hunting after we hit something that isn't an if-statement or a ThrowIfNull invocation.
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
        var fullName = type.FullName!;
        var typeSymbol = compilation.GetTypeByMetadataName(fullName);
        return typeSymbol is null
            ? generator.ParseTypeName(fullName)
            : generator.TypeExpression(typeSymbol);
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
            nameof(string.IsNullOrEmpty) => FeaturesResources._0_cannot_be_null_or_empty,
            nameof(string.IsNullOrWhiteSpace) => FeaturesResources._0_cannot_be_null_or_whitespace,
            _ => throw ExceptionUtilities.Unreachable(),
        };

        var nameofExpression = generator.NameOfExpression(generator.IdentifierName(parameter.Name));

        return generator.ObjectCreationExpression(
            GetTypeNode(compilation, generator, typeof(ArgumentException)),
            CreateExceptionMessageArgument(text, generator, nameofExpression),
            nameofExpression);
    }

    private SyntaxNode CreateExceptionMessageArgument(string messageTemplate, SyntaxGenerator generator, SyntaxNode parameterNameOfExpression)
    {
        // The resource string is written to be shown in a UI and is not necessarily valid code, but we're
        // going to be putting it into a string literal so we need to escape quotes etc. to avoid syntax errors
        var escapedText = EscapeResourceString(messageTemplate);

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var content);

        var textParts = GetPreAndPostTextParts(messageTemplate);
        var escapedTextParts = GetPreAndPostTextParts(escapedText);
        if (textParts.pre is null)
        {
            Debug.Fail("Should have found {0} in the resource string.");
            content.Add(InterpolatedStringText(generator, escapedText, messageTemplate));
        }
        else
        {
            content.Add(InterpolatedStringText(generator, escapedTextParts.pre!, textParts.pre));
            content.Add(generator.Interpolation(parameterNameOfExpression));
            content.Add(InterpolatedStringText(generator, escapedTextParts.post!, textParts.post!));
        }

        return generator.InterpolatedStringExpression(
            generator.CreateInterpolatedStringStartToken(isVerbatim: false),
            content,
            generator.CreateInterpolatedStringEndToken());
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
