// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.UseIsNullCheck;

internal abstract class AbstractUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer<
    TLanguageKindEnum>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TLanguageKindEnum : struct
{
    protected AbstractUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(LocalizableString title)
        : base(IDEDiagnosticIds.UseIsNullCheckDiagnosticId,
               EnforceOnBuildValues.UseIsNullCheck,
               CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod,
               title,
               new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            var objectType = context.Compilation.GetSpecialType(SpecialType.System_Object);
            if (objectType != null && IsLanguageVersionSupported(context.Compilation))
            {
                var referenceEqualsMethod = objectType.GetMembers(nameof(ReferenceEquals))
                                                      .OfType<IMethodSymbol>()
                                                      .FirstOrDefault(m => m.DeclaredAccessibility == Accessibility.Public &&
                                                                           m.Parameters.Length == 2);
                if (referenceEqualsMethod != null)
                {
                    var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
                    var unconstraintedGenericSupported = IsUnconstrainedGenericSupported(context.Compilation);
                    context.RegisterSyntaxNodeAction(
                        c => AnalyzeSyntax(c, referenceEqualsMethod, unconstraintedGenericSupported),
                        syntaxKinds.Convert<TLanguageKindEnum>(syntaxKinds.InvocationExpression));
                }
            }
        });

    protected abstract bool IsLanguageVersionSupported(Compilation compilation);
    protected abstract bool IsUnconstrainedGenericSupported(Compilation compilation);
    protected abstract ISyntaxFacts GetSyntaxFacts();

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context, IMethodSymbol referenceEqualsMethod, bool unconstraintedGenericSupported)
    {
        var cancellationToken = context.CancellationToken;

        var semanticModel = context.SemanticModel;

        var option = context.GetAnalyzerOptions().PreferIsNullCheckOverReferenceEqualityMethod;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
        {
            return;
        }

        var invocation = context.Node;
        var syntaxFacts = GetSyntaxFacts();

        var expression = syntaxFacts.GetExpressionOfInvocationExpression(invocation);
        var nameNode = syntaxFacts.IsIdentifierName(expression)
            ? expression
            : syntaxFacts.IsSimpleMemberAccessExpression(expression)
                ? syntaxFacts.GetNameOfMemberAccessExpression(expression)
                : null;

        if (!syntaxFacts.IsIdentifierName(nameNode))
        {
            return;
        }

        syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out _);
        if (!syntaxFacts.StringComparer.Equals(name, nameof(ReferenceEquals)))
        {
            return;
        }

        var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
        if (arguments.Count != 2)
        {
            return;
        }

        if (!MatchesPattern(syntaxFacts, arguments[0], arguments[1]) &&
            !MatchesPattern(syntaxFacts, arguments[1], arguments[0]))
        {
            return;
        }

        var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
        if (!referenceEqualsMethod.Equals(symbol))
        {
            return;
        }

        var properties = ImmutableDictionary<string, string?>.Empty.Add(
            UseIsNullConstants.Kind, UseIsNullConstants.ReferenceEqualsKey);

        var genericParameterSymbol = GetGenericParameterSymbol(syntaxFacts, semanticModel, arguments[0], arguments[1], cancellationToken);
        if (genericParameterSymbol != null)
        {
            if (genericParameterSymbol.IsValueType)
            {
                // 'is null' would generate error CS0403: Cannot convert null to type parameter 'T' because it could be a non-nullable value type. Consider using 'default(T)' instead.
                // '== null' would generate error CS0019: Operator '==' cannot be applied to operands of type 'T' and '<null>'
                // 'Is Nothing' would generate error BC30020: 'Is' operator does not accept operands of type 'T'. Operands must be reference or nullable types.
                return;
            }

            // HasReferenceTypeConstraint returns false for base type constraint.
            // IsReferenceType returns true.

            if (!genericParameterSymbol.IsReferenceType && !unconstraintedGenericSupported)
            {
                // Needs special casing for C# as long as
                // 'is null' over unconstrained generic is implemented in C# 8.
                properties = properties.Add(UseIsNullConstants.UnconstrainedGeneric, "");
            }
        }

        var additionalLocations = ImmutableArray.Create(invocation.GetLocation());

        var negated = syntaxFacts.IsLogicalNotExpression(invocation.Parent);
        if (negated)
        {
            properties = properties.Add(UseIsNullConstants.Negated, "");
        }

        context.ReportDiagnostic(
            DiagnosticHelper.Create(
                Descriptor, nameNode.GetLocation(),
                option.Notification,
                context.Options,
                additionalLocations, properties));
    }

    private static ITypeParameterSymbol? GetGenericParameterSymbol(ISyntaxFacts syntaxFacts, SemanticModel semanticModel, SyntaxNode node1, SyntaxNode node2, CancellationToken cancellationToken)
    {
        var valueNode = syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(node1)) ? node2 : node1;
        var argumentExpression = syntaxFacts.GetExpressionOfArgument(valueNode);
        if (argumentExpression != null)
        {
            var parameterType = semanticModel.GetTypeInfo(argumentExpression, cancellationToken).Type;
            return parameterType as ITypeParameterSymbol;
        }

        return null;
    }

    private static bool MatchesPattern(ISyntaxFacts syntaxFacts, SyntaxNode node1, SyntaxNode node2)
        => syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(node1)) &&
           !syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(node2));
}
