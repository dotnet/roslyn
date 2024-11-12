// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseExplicitTupleName;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
internal sealed class UseExplicitTupleNameDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public const string ElementName = nameof(ElementName);

    public UseExplicitTupleNameDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId,
               EnforceOnBuildValues.UseExplicitTupleName,
               CodeStyleOptions2.PreferExplicitTupleNames,
               title: new LocalizableResourceString(nameof(AnalyzersResources.Use_explicitly_provided_tuple_name), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
               messageFormat: new LocalizableResourceString(nameof(AnalyzersResources.Prefer_explicitly_provided_tuple_element_name), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterOperationAction(AnalyzeOperation, OperationKind.FieldReference);

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        // We only create a diagnostic if the option's value is set to true.
        var option = context.GetAnalyzerOptions().PreferExplicitTupleNames;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
        {
            return;
        }

        if (option.Notification.Severity == ReportDiagnostic.Suppress)
        {
            return;
        }

        var fieldReferenceOperation = (IFieldReferenceOperation)context.Operation;

        var field = fieldReferenceOperation.Field;
        if (field.ContainingType.IsTupleType)
        {
            if (field.CorrespondingTupleField?.Equals(field) == true)
            {
                var namedField = GetNamedField(field.ContainingType, field, context.CancellationToken);
                if (namedField != null)
                {
                    var memberAccessSyntax = fieldReferenceOperation.Syntax;
                    var nameNode = memberAccessSyntax.ChildNodesAndTokens().Reverse().FirstOrDefault().AsNode();
                    if (nameNode != null)
                    {
                        var properties = ImmutableDictionary<string, string?>.Empty.Add(
                            nameof(ElementName), namedField.Name);
                        context.ReportDiagnostic(DiagnosticHelper.Create(
                            Descriptor,
                            nameNode.GetLocation(),
                            option.Notification,
                            context.Options,
                            additionalLocations: null,
                            properties));
                    }
                }
            }
        }
    }

    private static IFieldSymbol? GetNamedField(
        INamedTypeSymbol containingType, IFieldSymbol unnamedField, CancellationToken cancellationToken)
    {
        foreach (var member in containingType.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member.Kind == SymbolKind.Field)
            {
                var fieldSymbol = (IFieldSymbol)member;
                if (unnamedField.Equals(fieldSymbol.CorrespondingTupleField) &&
                    !fieldSymbol.Name.Equals(unnamedField.Name))
                {
                    return fieldSymbol;
                }
            }
        }

        return null;
    }
}
