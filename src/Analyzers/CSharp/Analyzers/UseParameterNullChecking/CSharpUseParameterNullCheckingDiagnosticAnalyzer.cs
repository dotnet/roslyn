// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseParameterNullChecking
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseParameterNullCheckingDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private const string ArgumentNullExceptionName = $"{nameof(System)}.{nameof(ArgumentNullException)}";

        public CSharpUseParameterNullCheckingDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseParameterNullCheckingId,
                   EnforceOnBuildValues.UseParameterNullChecking,
                   CodeStyleOptions2.PreferParameterNullChecking,
                   CSharpAnalyzersResources.Use_parameter_null_checking,
                   new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(context =>
            {
                if (((CSharpCompilation)context.Compilation).LanguageVersion < LanguageVersionExtensions.CSharpNext)
                {
                    return;
                }

                var compilation = context.Compilation;
                var argumentNullException = compilation.GetTypeByMetadataName(ArgumentNullExceptionName);
                if (argumentNullException is null)
                {
                    return;
                }

                var argumentNullExceptionConstructor = argumentNullException.InstanceConstructors.FirstOrDefault(m =>
                    m.DeclaredAccessibility == Accessibility.Public
                    && m.Parameters.Length == 1
                    && m.Parameters[0].Type.SpecialType == SpecialType.System_String);
                if (argumentNullExceptionConstructor is null)
                {
                    return;
                }

                var objectType = compilation.GetSpecialType(SpecialType.System_Object);
                var referenceEqualsMethod = (IMethodSymbol?)objectType
                    .GetMembers(nameof(ReferenceEquals))
                    .FirstOrDefault(m => m is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, Parameters.Length: 2 });

                context.RegisterSyntaxNodeAction(
                    context => AnalyzeSyntax(context, argumentNullExceptionConstructor, referenceEqualsMethod),
                    SyntaxKind.ConstructorDeclaration,
                    SyntaxKind.MethodDeclaration,
                    SyntaxKind.LocalFunctionStatement,
                    SyntaxKind.SimpleLambdaExpression,
                    SyntaxKind.ParenthesizedLambdaExpression,
                    SyntaxKind.AnonymousMethodExpression);
            });

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context, IMethodSymbol argumentNullExceptionConstructor, IMethodSymbol? referenceEqualsMethod)
        {
            var cancellationToken = context.CancellationToken;

            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;

            var option = context.Options.GetOption(CodeStyleOptions2.PreferParameterNullChecking, semanticModel.Language, syntaxTree, cancellationToken);
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
                _ => throw ExceptionUtilities.UnexpectedValue(node)
            };
            if (block is null)
            {
                return;
            }

            var methodSymbol = node is AnonymousFunctionExpressionSyntax
                ? semanticModel.GetSymbolInfo(node, cancellationToken).Symbol
                : semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (methodSymbol is null)
            {
                return;
            }

            foreach (var statement in block.Statements)
            {
                IParameterSymbol? parameter;
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
                            case { Condition: InvocationExpressionSyntax { Expression: var receiver, ArgumentList.Arguments: { Count: 2 } arguments } }:
                                if (referenceEqualsMethod == null || !referenceEqualsMethod.Equals(semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol))
                                {
                                    continue;
                                }

                                left = arguments[0].Expression;
                                right = arguments[1].Expression;
                                break;

                            default:
                                continue;
                        }

                        if (!AreOperandsApplicable(left, right, out parameter)
                            && !AreOperandsApplicable(right, left, out parameter))
                        {
                            continue;
                        }

                        var throwStatement = ifStatement.Statement switch
                        {
                            ThrowStatementSyntax @throw => @throw,
                            BlockSyntax { Statements: { Count: 1 } statements } => statements[0] as ThrowStatementSyntax,
                            _ => null
                        };

                        if (throwStatement is null
                            || throwStatement.Expression is not ObjectCreationExpressionSyntax thrownInIf
                            || !IsConstructorApplicable(thrownInIf, parameter))
                        {
                            continue;
                        }

                        break;

                    // this.field = param ?? throw new ArgumentNullException(nameof(param));
                    case ExpressionStatementSyntax
                    {
                        Expression: AssignmentExpressionSyntax
                        {
                            Right: BinaryExpressionSyntax
                            {
                                OperatorToken.RawKind: (int)SyntaxKind.QuestionQuestionToken,
                                Left: ExpressionSyntax maybeParameter,
                                Right: ThrowExpressionSyntax { Expression: ObjectCreationExpressionSyntax thrownInNullCoalescing }
                            }
                        }
                    }:
                        if (!IsParameter(maybeParameter, out parameter) || !IsConstructorApplicable(thrownInNullCoalescing, parameter))
                        {
                            continue;
                        }

                        break;

                    default:
                        continue;
                }

                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    statement.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: new[] { parameter.Locations[0] },
                    properties: null));
            }

            bool AreOperandsApplicable(ExpressionSyntax maybeParameter, ExpressionSyntax maybeNullLiteral, [NotNullWhen(true)] out IParameterSymbol? parameter)
            {
                if (!maybeNullLiteral.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    parameter = null;
                    return false;
                }

                return IsParameter(maybeParameter, out parameter);
            }

            bool IsParameter(ExpressionSyntax maybeParameter, [NotNullWhen(true)] out IParameterSymbol? parameter)
            {
                if (maybeParameter is CastExpressionSyntax { Type: var type, Expression: var operand })
                {
                    if (semanticModel.GetTypeInfo(type).Type?.SpecialType != SpecialType.System_Object)
                    {
                        parameter = null;
                        return false;
                    }

                    maybeParameter = operand;
                }

                if (semanticModel.GetSymbolInfo(maybeParameter).Symbol is not IParameterSymbol { ContainingSymbol: { } containingSymbol } parameterSymbol || !containingSymbol.Equals(methodSymbol))
                {
                    parameter = null;
                    return false;
                }

                parameter = parameterSymbol;
                return true;
            }

            bool IsConstructorApplicable(ObjectCreationExpressionSyntax exceptionCreation, IParameterSymbol parameterSymbol)
            {
                if (exceptionCreation.ArgumentList is null
                    || exceptionCreation.ArgumentList.Arguments.FirstOrDefault() is not { } argument)
                {
                    return false;
                }

                var constantValue = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
                if (!constantValue.HasValue || constantValue.Value is not string constantString || !string.Equals(constantString, parameterSymbol.Name, StringComparison.Ordinal))
                {
                    return false;
                }

                if (!argumentNullExceptionConstructor.Equals(semanticModel.GetSymbolInfo(exceptionCreation, cancellationToken).Symbol))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
