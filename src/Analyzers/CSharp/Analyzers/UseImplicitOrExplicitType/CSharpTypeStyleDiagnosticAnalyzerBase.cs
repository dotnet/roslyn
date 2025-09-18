// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;

internal static class CSharpTypeStyleUtilities
{
    public const string EquivalenceyKey = nameof(EquivalenceyKey);

    public const string BuiltInType = nameof(BuiltInType);
    public const string TypeIsApparent = nameof(TypeIsApparent);
    public const string Elsewhere = nameof(Elsewhere);
}

internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase(
    string diagnosticId,
    EnforceOnBuild enforceOnBuild,
    LocalizableString title,
    LocalizableString message)
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(diagnosticId,
        enforceOnBuild,
        [CSharpCodeStyleOptions.VarForBuiltInTypes, CSharpCodeStyleOptions.VarWhenTypeIsApparent, CSharpCodeStyleOptions.VarElsewhere],
        title, message)
{
    private static readonly ImmutableDictionary<string, string?> BuiltInTypeProperties = ImmutableDictionary<string, string?>.Empty.Add(CSharpTypeStyleUtilities.EquivalenceyKey, CSharpTypeStyleUtilities.BuiltInType);
    private static readonly ImmutableDictionary<string, string?> TypeIsApparentProperties = ImmutableDictionary<string, string?>.Empty.Add(CSharpTypeStyleUtilities.EquivalenceyKey, CSharpTypeStyleUtilities.TypeIsApparent);
    private static readonly ImmutableDictionary<string, string?> ElsewhereProperties = ImmutableDictionary<string, string?>.Empty.Add(CSharpTypeStyleUtilities.EquivalenceyKey, CSharpTypeStyleUtilities.Elsewhere);

    protected abstract CSharpTypeStyleHelper Helper { get; }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(
            HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression);

    private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        var declarationStatement = context.Node;
        var cancellationToken = context.CancellationToken;

        var semanticModel = context.SemanticModel;
        var declaredType = Helper.FindAnalyzableType(declarationStatement, semanticModel, cancellationToken);
        if (declaredType == null)
            return;

        var simplifierOptions = context.GetCSharpAnalyzerOptions().GetSimplifierOptions();

        var typeStyle = Helper.AnalyzeTypeName(
            declaredType, semanticModel, simplifierOptions, cancellationToken);
        if (!typeStyle.IsStylePreferred
            || ShouldSkipAnalysis(context, typeStyle.Notification)
            || !typeStyle.CanConvert)
        {
            return;
        }

        // The severity preference is not Hidden, as indicated by IsStylePreferred.
        var descriptor = Descriptor;
        context.ReportDiagnostic(DiagnosticHelper.Create(
            descriptor,
            declarationStatement.SyntaxTree.GetLocation(declaredType.StripRefIfNeeded().Span),
            typeStyle.Notification,
            context.Options,
            additionalLocations: null,
            typeStyle.Context switch
            {
                CSharpTypeStyleHelper.Context.BuiltInType => BuiltInTypeProperties,
                CSharpTypeStyleHelper.Context.TypeIsApparent => TypeIsApparentProperties,
                CSharpTypeStyleHelper.Context.Elsewhere => ElsewhereProperties,
                _ => throw ExceptionUtilities.UnexpectedValue(typeStyle.Context),
            }));
    }
}
