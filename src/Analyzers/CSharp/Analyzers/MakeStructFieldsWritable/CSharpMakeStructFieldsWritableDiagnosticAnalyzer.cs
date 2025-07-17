// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMakeStructFieldsWritableDiagnosticAnalyzer : AbstractCodeQualityDiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_diagnosticDescriptor = CreateDescriptor(
        IDEDiagnosticIds.MakeStructFieldsWritable,
        EnforceOnBuildValues.MakeStructFieldsWritable,
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_readonly_fields_writable), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Struct_contains_assignment_to_this_outside_of_constructor_Make_readonly_fields_writable), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        hasAnyCodeStyleOption: false, isUnnecessary: false);

    public CSharpMakeStructFieldsWritableDiagnosticAnalyzer()
        : base([s_diagnosticDescriptor], GeneratedCodeAnalysisFlags.ReportDiagnostics)
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context
            => SymbolAnalyzer.CreateAndRegisterActions(context));
    }

    private sealed class SymbolAnalyzer
    {
        private readonly INamedTypeSymbol _namedTypeSymbol;
        private bool _hasTypeInstanceAssignment;

        private SymbolAnalyzer(INamedTypeSymbol namedTypeSymbol)
            => _namedTypeSymbol = namedTypeSymbol;

        public static void CreateAndRegisterActions(CompilationStartAnalysisContext context)
        {
            context.RegisterSymbolStartAction(context =>
            {
                // We report diagnostic only if these requirements are met:
                // 1. The type is struct
                // 2. Struct contains at least one 'readonly' field
                // 3. Struct contains assignment to 'this' outside the scope of constructor
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                // We are only interested in struct declarations
                if (namedTypeSymbol.TypeKind != TypeKind.Struct)
                    return;

                // We check if struct contains any 'readonly' fields
                if (!HasReadonlyField(namedTypeSymbol))
                    return;

                // Check if diagnostic location is within the analysis span
                if (!context.ShouldAnalyzeLocation(GetDiagnosticLocation(namedTypeSymbol)))
                    return;

                var symbolAnalyzer = new SymbolAnalyzer(namedTypeSymbol);
                symbolAnalyzer.RegisterActions(context);
            }, SymbolKind.NamedType);
        }

        private static Location GetDiagnosticLocation(INamedTypeSymbol namedTypeSymbol)
            => namedTypeSymbol.Locations[0];

        private static bool HasReadonlyField(INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Any(field => field is { AssociatedSymbol: null, IsStatic: false, IsReadOnly: true });
        }

        private void RegisterActions(SymbolStartAnalysisContext context)
        {
            context.RegisterOperationBlockStartAction(context =>
            {
                if (context.OwningSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor })
                {
                    // We are looking for assignment to 'this' outside the constructor scope
                    return;
                }

                context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
            });

            context.RegisterSymbolEndAction(SymbolEndAction);
        }

        private void AnalyzeAssignment(OperationAnalysisContext context)
        {
            var operationAssigmnent = (IAssignmentOperation)context.Operation;
            if (operationAssigmnent.Target is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance })
            {
                Volatile.Write(ref _hasTypeInstanceAssignment, true);
            }
        }

        private void SymbolEndAction(SymbolAnalysisContext context)
        {
            if (_hasTypeInstanceAssignment)
            {
                var diagnostic = Diagnostic.Create(
                                s_diagnosticDescriptor,
                                GetDiagnosticLocation(_namedTypeSymbol));
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
