﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructReadOnly;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMakeStructReadOnlyDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpMakeStructReadOnlyDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId,
               EnforceOnBuildValues.MakeStructReadOnly,
               CSharpCodeStyleOptions.PreferReadOnlyStruct,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_struct_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Struct_can_be_made_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            if (compilation.LanguageVersion() < LanguageVersion.CSharp7_2)
                return;

            context.RegisterSymbolStartAction(context =>
            {
                // First, see if this at least strongly looks like a struct that could be converted.
                if (!IsCandidate(context, out var typeDeclaration, out var option))
                    return;

                // Looks good.  However, we have to make sure that the struct has no code which actually overwrites 'this'

                var writesToThis = false;
                context.RegisterSyntaxNodeAction(context =>
                {
                    var semanticModel = context.SemanticModel;
                    var thisExpression = (ThisExpressionSyntax)context.Node;
                    var cancellationToken = context.CancellationToken;
                    writesToThis = writesToThis || thisExpression.IsWrittenTo(semanticModel, cancellationToken);
                }, SyntaxKind.ThisExpression);

                context.RegisterSymbolEndAction(context =>
                {
                    // if we wrote to 'this', then we cannot convert this struct.
                    if (writesToThis)
                        return;

                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        Descriptor,
                        typeDeclaration.Identifier.GetLocation(),
                        option.Notification.Severity,
                        additionalLocations: ImmutableArray.Create(typeDeclaration.GetLocation()),
                        properties: null));
                });
            }, SymbolKind.NamedType);
        });

    private static bool IsCandidate(
        SymbolStartAnalysisContext context,
        [NotNullWhen(true)] out TypeDeclarationSyntax? typeDeclaration,
        [NotNullWhen(true)] out CodeStyleOption2<bool>? option)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        var cancellationToken = context.CancellationToken;

        typeDeclaration = null;
        option = null;
        if (typeSymbol.TypeKind is not TypeKind.Struct)
            return false;

        if (typeSymbol.IsReadOnly)
            return false;

        if (typeSymbol.DeclaringSyntaxReferences.Length == 0)
            return false;

        typeDeclaration = typeSymbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) as TypeDeclarationSyntax;
        if (typeDeclaration is null)
            return false;

        var options = context.GetCSharpAnalyzerOptions(typeDeclaration.SyntaxTree);
        option = options.PreferReadOnlyStruct;
        if (!option.Value)
            return false;

        // Now, ensure we have at least one field and that all fields are readonly.
        var hasField = false;
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IFieldSymbol field)
            {
                hasField = true;
                if (!field.IsReadOnly)
                    return false;
            }
        }

        if (!hasField)
            return false;

        return true;
    }
}
