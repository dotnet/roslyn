// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation;

/// <summary>
/// Supports code like <c>o switch { int _ => ... }</c> to just <c>o switch { int => ... }</c> in C# 9 and above.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnnecessaryDiscardDesignationDiagnosticAnalyzer
    : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
{
    public CSharpRemoveUnnecessaryDiscardDesignationDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.RemoveUnnecessaryDiscardDesignationDiagnosticId,
               EnforceOnBuildValues.RemoveUnnecessaryDiscardDesignation,
               option: null,
               fadingOption: null,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_unnessary_discard), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Discard_can_be_removed), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp9)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeDiscardDesignation, SyntaxKind.DiscardDesignation);
        });
    }

    private void AnalyzeDiscardDesignation(SyntaxNodeAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        var discard = (DiscardDesignationSyntax)context.Node;

        if (discard.Parent is DeclarationPatternSyntax declarationPattern)
        {
            // Don't perform semantic checks if we are in a 'simple' pattern like `x is A _`.
            // Since single identifier in cases like `x is A` binds stronger to type name for back compat reasons, we can safely remove discard anyway.
            if (declarationPattern.Parent is not IsPatternExpressionSyntax)
            {
                var typeSyntax = declarationPattern.Type;

                while (typeSyntax is QualifiedNameSyntax qualifiedName)
                    typeSyntax = qualifiedName.Left;

                if (typeSyntax is IdentifierNameSyntax identifierName &&
                    identifierName.GetAncestor<TypeDeclarationSyntax>() is { } containingTypeSyntax)
                {
                    var typeSymbol = semanticModel.GetRequiredDeclaredSymbol(containingTypeSyntax, cancellationToken);

                    // If we find other symbols with the same name in the type we are currently in, removing discard can lead to a compiler error.
                    // For instance, we can have a property in the type we are currently in with the same name as an identifier in the discard designation.
                    // Since a single identifier binds stronger to property name, we cannot remove discard.
                    if (typeSymbol.GetMembers(identifierName.Identifier.ValueText).Any())
                        return;
                }
            }

            Report(discard);
        }
        else if (discard.Parent is RecursivePatternSyntax recursivePattern)
        {
            // can't remove from `(int i) _` as `(int i)` is not a legal pattern itself.
            if (recursivePattern.PositionalPatternClause != null &&
                recursivePattern.PositionalPatternClause.Subpatterns.Count == 1)
            {
                return;
            }

            Report(discard);
        }

        return;

        void Report(DiscardDesignationSyntax discard)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                this.Descriptor,
                discard.GetLocation()));
        }
    }
}
