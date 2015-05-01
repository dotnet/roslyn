// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Microsoft.CodeAnalysis.Performance
{
    /// <summary>Base type for an analyzer that looks for empty array allocations and recommends their replacement.</summary>
    public abstract class EmptyArrayDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Performance".</summary>
        private const string PerformanceCategory = "Performance";

        /// <summary>The name of the array type.</summary>
        internal const string ArrayTypeName = "System.Array"; // using instead of GetSpecialType to make more testable

        /// <summary>The name of the Empty method on System.Array.</summary>
        internal const string ArrayEmptyMethodName = "Empty";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseArrayEmptyDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseArrayEmptyMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        /// <summary>The diagnostic descriptor used when Array.Empty should be used instead of a new array allocation.</summary>
        internal static readonly DiagnosticDescriptor UseArrayEmptyDescriptor = new DiagnosticDescriptor(
            RoslynDiagnosticIds.UseArrayEmptyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            PerformanceCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(UseArrayEmptyDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            // When compilation begins, check whether Array.Empty<T> is available.
            // Only if it is, register the syntax node action provided by the derived implementations.
            context.RegisterCompilationStartAction(ctx =>
            {
                INamedTypeSymbol typeSymbol = ctx.Compilation.GetTypeByMetadataName(ArrayTypeName);
                if (typeSymbol != null && typeSymbol.DeclaredAccessibility == Accessibility.Public)
                {
                    IMethodSymbol methodSymbol = typeSymbol.GetMembers(ArrayEmptyMethodName).FirstOrDefault() as IMethodSymbol;
                    if (methodSymbol != null && methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                        methodSymbol.IsStatic && methodSymbol.Arity == 1 && methodSymbol.Parameters.Length == 0)
                    {
                        RegisterSyntaxNodeAction(ctx);
                    }
                }
            });
        }

        /// <summary>Registers a syntax node action for the current compilation context.</summary>
        /// <param name="context">The compilation context.</param>
        internal abstract void RegisterSyntaxNodeAction(CompilationStartAnalysisContext context);

        /// <summary>Reports a diagnostic warning for an array creation that should be replaced.</summary>
        /// <param name="context">The context.</param>
        /// <param name="arrayCreationExpression">The array creation expression to be replaced.</param>
        internal void Report(SyntaxNodeAnalysisContext context, SyntaxNode arrayCreationExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(UseArrayEmptyDescriptor, arrayCreationExpression.GetLocation()));
        }
    }
}
