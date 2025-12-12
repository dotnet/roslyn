// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseNameofInAttribute;

/// <summary>
/// Analyzer that looks for things like `NotNullIfNotNull("param")` and offers to use `NotNullIfNotNull(nameof(param))` instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseNameofInAttributeDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public const string NameKey = nameof(NameKey);

    public CSharpUseNameofInAttributeDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseNameofInAttributeDiagnosticId,
               EnforceOnBuildValues.UseNameofInAttribute,
               option: null,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Use_nameof), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.LanguageVersion() >= LanguageVersion.CSharp11)
                context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        });
    }

    private void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        var cancellationToken = context.CancellationToken;
        var attribute = (AttributeSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (attribute.ArgumentList is null)
            return;

        var attributeName = attribute.Name.GetRightmostName()?.Identifier.ValueText;
        if (attributeName is null)
            return;

        if (attributeName
                is not "NotNullIfNotNull"
                and not "NotNullIfNotNullAttribute"
                and not "MemberNotNull"
                and not "MemberNotNullAttribute"
                and not "MemberNotNullWhen"
                and not "MemberNotNullWhenAttribute"
                and not "CallerArgumentExpression"
                and not "CallerArgumentExpressionAttribute")
        {
            return;
        }

        INamedTypeSymbol? containingType = null;
        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            if (argument.Expression is not LiteralExpressionSyntax(SyntaxKind.StringLiteralExpression) and not InterpolatedStringExpressionSyntax)
                continue;

            var constantValue = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
            if (constantValue.Value is not string stringValue)
                continue;

            if (stringValue == "")
                continue;

            var position = argument.Expression.SpanStart;

            containingType ??= semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (containingType is null)
                return;

            // Now, see if there are any parameters in scope with this same name.  If so, we can now suggest the user
            // just use `nameof(param)` instead of `"param"` in the attribute.
            var symbols = semanticModel.LookupSymbols(argument.Expression.SpanStart, name: stringValue);
            if (symbols.Any(s => s.IsAccessibleWithin(containingType)) ||
                MatchesParameterOnContainer(attribute, stringValue))
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    this.Descriptor,
                    argument.Expression.GetLocation(),
                    NotificationOption2.Suggestion,
                    context.Options,
                    additionalLocations: null,
                    ImmutableDictionary<string, string?>.Empty.Add(NameKey, stringValue)));
            }
        }
    }

    private static bool MatchesParameterOnContainer(AttributeSyntax attribute, string stringValue)
    {
        var attributeList = attribute.Parent as AttributeListSyntax;
        var container = attributeList?.Parent;

        if (container is ParameterSyntax)
        {
            var parameterList = container.Parent as BaseParameterListSyntax;
            container = parameterList?.Parent;
        }

        if (container is null)
            return false;

        var parameters = container.GetParameterList();
        if (parameters is null)
            return false;

        foreach (var parameter in parameters.Parameters)
        {
            if (parameter.Identifier.ValueText == stringValue)
                return true;
        }

        return false;
    }
}
