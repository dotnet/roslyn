// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Razor.Compiler.Analyzers;

#pragma warning disable RS1041 // Compiler extensions should be implemented in assemblies targeting netstandard2.0

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentParameterNullableWarningSuppressor : DiagnosticSuppressor
{
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.ComponentParameterNullableWarningSuppressorDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

    //Suppress CS8618: "Non-nullable {0} '{1}' must contain a non-null value when exiting constructor. Consider declaring the {0} as nullable."
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [
            new SuppressionDescriptor(AnalyzerIDs.ComponentParameterNullableWarningSuppressionId, "CS8618", Description)
        ];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        var editorRequiredSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.EditorRequiredAttribute");
        var parameterSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
        var componentSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");

        if (parameterSymbol is null || editorRequiredSymbol is null || componentSymbol is null)
        {
            return;
        }

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var node = diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
            if (node is PropertyDeclarationSyntax propertySyntax && propertySyntax.AttributeLists.Any())
            {
                var symbol = context.GetSemanticModel(propertySyntax.SyntaxTree).GetDeclaredSymbol(propertySyntax, context.CancellationToken);
                if (IsValidEditorRequiredParameter(symbol))
                {
                    context.ReportSuppression(Suppression.Create(SupportedSuppressions[0], diagnostic));
                }
            }
        }

        bool IsValidEditorRequiredParameter(ISymbol? symbol)
        {
            // public instance property, with a public setter
            if (symbol is not IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false, SetMethod.DeclaredAccessibility: Accessibility.Public })
            {
                return false;
            }

            // containing type implements IComponent
            if (!symbol.ContainingType.AllInterfaces.Any(componentSymbol, static (@interface, componentSymbol) => @interface.Equals(componentSymbol, SymbolEqualityComparer.Default)))
            {
                return false;
            }

            // has both [Parameter] and [EditorRequired] attributes
            bool hasParameter = false, hasRequired = false;
            foreach (var attribute in symbol.GetAttributes())
            {
                if (!hasParameter && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, parameterSymbol))
                {
                    hasParameter = true;
                    if (hasRequired)
                    {
                        break;
                    }
                    continue;
                }

                if (!hasRequired && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, editorRequiredSymbol))
                {
                    hasRequired = true;
                    if (hasParameter)
                    {
                        break;
                    }
                    continue;
                }
            }
            return hasParameter && hasRequired;
        }
    }
}

