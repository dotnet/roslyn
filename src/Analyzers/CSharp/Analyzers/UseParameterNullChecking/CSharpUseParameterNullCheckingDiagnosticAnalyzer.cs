// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseParameterNullChecking
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseParameterNullCheckingDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private const string ArgumentNullExceptionName = $"{nameof(System)}.{nameof(ArgumentNullException)}";
        private static readonly LocalizableResourceString s_resourceTitle = new(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        public CSharpUseParameterNullCheckingDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseParameterNullCheckingId,
                   EnforceOnBuildValues.UseParameterNullChecking,
                   CSharpCodeStyleOptions.PreferParameterNullChecking,
                   LanguageNames.CSharp,
                   s_resourceTitle,
                   s_resourceTitle)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(context =>
            {
                var compilation = (CSharpCompilation)context.Compilation;
                if (compilation.LanguageVersion < LanguageVersionExtensions.CSharpNext)
                {
                    return;
                }

                var argumentNullException = compilation.GetBestTypeByMetadataName(ArgumentNullExceptionName);
                if (argumentNullException is null)
                {
                    return;
                }

                IMethodSymbol? argumentNullExceptionConstructor = null;
                IMethodSymbol? argumentNullExceptionStringConstructor = null;
                foreach (var constructor in argumentNullException.InstanceConstructors)
                {
                    if (argumentNullExceptionConstructor is not null && argumentNullExceptionStringConstructor is not null)
                    {
                        break;
                    }

                    switch (constructor)
                    {
                        case { DeclaredAccessibility: Accessibility.Public, Parameters.Length: 0 }:
                            argumentNullExceptionConstructor = constructor;
                            break;
                        case { DeclaredAccessibility: Accessibility.Public, Parameters.Length: 1 }
                            when constructor.Parameters[0].Type.SpecialType == SpecialType.System_String:

                            argumentNullExceptionStringConstructor = constructor;
                            break;
                    }
                }

                if (argumentNullExceptionConstructor is null || argumentNullExceptionStringConstructor is null)
                {
                    return;
                }

                var objectType = compilation.GetSpecialType(SpecialType.System_Object);
                var referenceEqualsMethod = (IMethodSymbol?)objectType
                    .GetMembers(nameof(ReferenceEquals))
                    .FirstOrDefault(m => m is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, Parameters.Length: 2 });

                // We are potentially interested in any declaration that has parameters.
                // However, we avoid indexers specifically because of the complexity of locating and deleting equivalent null checks across multiple accessors.
                context.RegisterSyntaxNodeAction(
                    context => AnalyzeSyntax(context, argumentNullExceptionConstructor, argumentNullExceptionStringConstructor, referenceEqualsMethod),
                    SyntaxKind.ConstructorDeclaration,
                    SyntaxKind.MethodDeclaration,
                    SyntaxKind.LocalFunctionStatement,
                    SyntaxKind.SimpleLambdaExpression,
                    SyntaxKind.ParenthesizedLambdaExpression,
                    SyntaxKind.AnonymousMethodExpression,
                    SyntaxKind.OperatorDeclaration,
                    SyntaxKind.ConversionOperatorDeclaration);
            });

        private void AnalyzeSyntax(
            SyntaxNodeAnalysisContext context,
            IMethodSymbol argumentNullExceptionConstructor,
            IMethodSymbol argumentNullExceptionStringConstructor,
            IMethodSymbol? referenceEqualsMethod)
        {
            var cancellationToken = context.CancellationToken;

            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;

            var option = context.Options.GetOption(CSharpCodeStyleOptions.PreferParameterNullChecking, syntaxTree, cancellationToken);
            if (!option.Value)
            {
                return;
            }

            var node = context.Node;
            var block = node switch
            {
                MethodDeclarationSyntax methodDecl => methodDecl.Body,
                ConstructorDeclarationSyntax constructorDecl => constructorDecl.Body,
                LocalFunctionStatementSyntax localFunctionStatement => localFunctionStatement.Body,
                AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.Block,
                OperatorDeclarationSyntax operatorDecl => operatorDecl.Body,
                ConversionOperatorDeclarationSyntax conversionDecl => conversionDecl.Body,
                _ => throw ExceptionUtilities.UnexpectedValue(node)
            };

            // More scenarios should be supported eventually: https://github.com/dotnet/roslyn/issues/58699
            if (block is null)
            {
                return;
            }

            var methodSymbol = node is AnonymousFunctionExpressionSyntax
                ? (IMethodSymbol?)semanticModel.GetSymbolInfo(node, cancellationToken).Symbol
                : (IMethodSymbol?)semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (methodSymbol is null || methodSymbol.Parameters.IsEmpty)
            {
                return;
            }

            foreach (var statement in block.Statements)
            {
                if (TryGetParameterNullCheckedByStatement(statement) is var (parameter, diagnosticLocation)
                    && ParameterCanUseNullChecking(parameter)
                    && parameter.DeclaringSyntaxReferences.FirstOrDefault() is SyntaxReference reference
                    && reference.SyntaxTree.Equals(statement.SyntaxTree)
                    && reference.GetSyntax() is ParameterSyntax parameterSyntax)
                {
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        Descriptor,
                        diagnosticLocation,
                        option.Notification.Severity,
                        additionalLocations: new[] { parameterSyntax.GetLocation() },
                        properties: null));
                }
                else
                {
                    var descendants = statement.DescendantNodesAndSelf(descendIntoChildren: static c => c is StatementSyntax);
                    foreach (var descendant in descendants)
                    {
                        // Mostly, we are fine with simplifying null checks in a way that
                        // causes us to *throw a different exception than before* for some inputs.
                        // However, we don't want to change semantics such that we
                        // *throw an exception instead of returning* or vice-versa.
                        // Therefore we ignore any null checks which are syntactically preceded by conditional or unconditional returns.
                        if (descendant is ReturnStatementSyntax)
                        {
                            return;
                        }
                    }
                }
            }

            return;

            bool ParameterCanUseNullChecking([NotNullWhen(true)] IParameterSymbol? parameter)
            {
                if (parameter is null)
                    return false;

                if (parameter.RefKind == RefKind.Out)
                    return false;

                if (parameter.Type.IsValueType)
                {
                    return parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                        || parameter.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer;
                }

                return true;
            }

            (IParameterSymbol parameter, Location diagnosticLocation)? TryGetParameterNullCheckedByStatement(StatementSyntax statement)
            {
                switch (statement)
                {
                    // if (param == null) { throw new ArgumentNullException(nameof(param)); }
                    // if (param is null) { throw new ArgumentNullException(nameof(param)); }
                    // if (object.ReferenceEquals(param, null)) { throw new ArgumentNullException(nameof(param)); }
                    case IfStatementSyntax ifStatement:
                        ExpressionSyntax left, right;
                        switch (ifStatement)
                        {
                            case { Condition: BinaryExpressionSyntax { OperatorToken.RawKind: (int)SyntaxKind.EqualsEqualsToken } binary }:
                                left = binary.Left;
                                right = binary.Right;
                                break;
                            case { Condition: IsPatternExpressionSyntax { Expression: var patternInput, Pattern: ConstantPatternSyntax { Expression: var patternExpression } } }:
                                left = patternInput;
                                right = patternExpression;
                                break;
                            case { Condition: InvocationExpressionSyntax { Expression: var receiver, ArgumentList.Arguments: { Count: 2 } arguments } }
                                when referenceEqualsMethod != null && referenceEqualsMethod.Equals(semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol):

                                left = arguments[0].Expression;
                                right = arguments[1].Expression;
                                break;

                            default:
                                return null;
                        }

                        var parameterInBinary = left.IsKind(SyntaxKind.NullLiteralExpression) ? TryGetParameter(right)
                            : right.IsKind(SyntaxKind.NullLiteralExpression) ? TryGetParameter(left)
                            : null;
                        if (parameterInBinary is null)
                        {
                            return null;
                        }

                        var throwStatement = ifStatement.Statement switch
                        {
                            ThrowStatementSyntax @throw => @throw,
                            BlockSyntax { Statements: { Count: 1 } statements } => statements[0] as ThrowStatementSyntax,
                            _ => null
                        };

                        if (throwStatement?.Expression is not ObjectCreationExpressionSyntax thrownInIf
                            || !IsConstructorApplicable(thrownInIf, parameterInBinary))
                        {
                            return null;
                        }

                        // The if statement could be associated with an arbitrarily complex else clause. We only want to highlight the "if" part which is removed by the fix.
                        var location = Location.Create(ifStatement.SyntaxTree, Text.TextSpan.FromBounds(ifStatement.SpanStart, ifStatement.Statement.Span.End));
                        return (parameterInBinary, location);

                    // this.field = param ?? throw new ArgumentNullException(nameof(param));
                    case ExpressionStatementSyntax
                    {
                        Expression: AssignmentExpressionSyntax
                        {
                            Left: var leftOfAssignment,
                            Right: BinaryExpressionSyntax
                            {
                                OperatorToken.RawKind: (int)SyntaxKind.QuestionQuestionToken,
                                Left: ExpressionSyntax maybeParameter,
                                Right: ThrowExpressionSyntax { Expression: ObjectCreationExpressionSyntax thrownInNullCoalescing } throwExpression
                            }
                        }
                    }:
                        var coalescedParameter = TryGetParameter(maybeParameter);
                        if (coalescedParameter is null || !IsConstructorApplicable(thrownInNullCoalescing, coalescedParameter))
                        {
                            return null;
                        }

                        // ensure we delete the entire statement in the below scenario:
                        //     void M(string s) { s = s ?? throw new ArgumentNullException(); }
                        // otherwise, we just replace the '??' expression with its left operand
                        var diagnosticLocation = coalescedParameter.Equals(semanticModel.GetSymbolInfo(leftOfAssignment, cancellationToken).Symbol)
                            ? statement.GetLocation()
                            : throwExpression.GetLocation();

                        return (coalescedParameter, diagnosticLocation);

                    default:
                        return null;
                }
            }

            IParameterSymbol? TryGetParameter(ExpressionSyntax maybeParameter)
            {
                // `(object)x == null` is often used to ensure reference equality is used.
                // therefore, we specially unwrap casts when the cast is to `object`.
                if (maybeParameter is CastExpressionSyntax { Type: var type, Expression: var operand })
                {
                    if (semanticModel.GetTypeInfo(type).Type?.SpecialType != SpecialType.System_Object)
                    {
                        return null;
                    }

                    maybeParameter = operand;
                }

                if (semanticModel.GetSymbolInfo(maybeParameter).Symbol is not IParameterSymbol { ContainingSymbol: { } containingSymbol } parameterSymbol || !containingSymbol.Equals(methodSymbol))
                {
                    return null;
                }

                return parameterSymbol;
            }

            bool IsConstructorApplicable(ObjectCreationExpressionSyntax exceptionCreation, IParameterSymbol parameterSymbol)
            {
                if (exceptionCreation.ArgumentList?.Arguments is not { } arguments)
                {
                    return false;
                }

                // 'new ArgumentNullException()'
                if (argumentNullExceptionConstructor.Equals(semanticModel.GetSymbolInfo(exceptionCreation, cancellationToken).Symbol))
                {
                    return arguments.Count == 0;
                }

                // 'new ArgumentNullException(nameof(param))' (or equivalent)
                if (!argumentNullExceptionStringConstructor.Equals(semanticModel.GetSymbolInfo(exceptionCreation, cancellationToken).Symbol))
                {
                    return false;
                }

                if (arguments.Count != 1)
                {
                    return false;
                }

                var constantValue = semanticModel.GetConstantValue(arguments[0].Expression, cancellationToken);
                if (constantValue.Value is not string constantString || !string.Equals(constantString, parameterSymbol.Name, StringComparison.Ordinal))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
