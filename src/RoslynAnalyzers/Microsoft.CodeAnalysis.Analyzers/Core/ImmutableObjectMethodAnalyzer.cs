// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Analyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1014: <inheritdoc cref="DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class ImmutableObjectMethodAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor DoNotIgnoreReturnValueDiagnosticRule = new(
            DiagnosticIds.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocation,
            CreateLocalizableResourceString(nameof(DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationTitle)),
            CreateLocalizableResourceString(nameof(DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DoNotIgnoreReturnValueDiagnosticRule);

        private const string SolutionFullName = @"Microsoft.CodeAnalysis.Solution";
        private const string ProjectFullName = @"Microsoft.CodeAnalysis.Project";
        private const string DocumentFullName = @"Microsoft.CodeAnalysis.Document";
        private const string SyntaxNodeFullName = @"Microsoft.CodeAnalysis.SyntaxNode";
        private const string CompilationFullName = @"Microsoft.CodeAnalysis.Compilation";

        private static readonly ImmutableArray<string> s_immutableMethodNames = ImmutableArray.Create(
            "Add",
            "Remove",
            "Replace",
            "With");

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(context =>
            {
                var compilation = context.Compilation;
                var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                var provider = WellKnownTypeProvider.GetOrCreate(compilation);
                AddIfNotNull(builder, provider.GetOrCreateTypeByMetadataName(SolutionFullName));
                AddIfNotNull(builder, provider.GetOrCreateTypeByMetadataName(ProjectFullName));
                AddIfNotNull(builder, provider.GetOrCreateTypeByMetadataName(DocumentFullName));
                AddIfNotNull(builder, provider.GetOrCreateTypeByMetadataName(SyntaxNodeFullName));
                AddIfNotNull(builder, provider.GetOrCreateTypeByMetadataName(CompilationFullName));
                var immutableTypeSymbols = builder.ToImmutable();
                if (immutableTypeSymbols.Length > 0)
                {
                    context.RegisterOperationAction(context => AnalyzeInvocationForIgnoredReturnValue(context, immutableTypeSymbols), OperationKind.Invocation);
                }
            });

            static void AddIfNotNull(ImmutableArray<INamedTypeSymbol>.Builder builder, INamedTypeSymbol? symbol)
            {
                if (symbol is not null)
                {
                    builder.Add(symbol);
                }
            }
        }

        public static void AnalyzeInvocationForIgnoredReturnValue(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> immutableTypeSymbols)
        {
            var invocation = (IInvocationOperation)context.Operation;
            // Returns void happens for the internal AddDebugSourceDocumentsForChecksumDirectives in the compiler itself.
            if (invocation.Parent is not IExpressionStatementOperation || invocation.TargetMethod.ReturnsVoid)
            {
                return;
            }

            // If the method doesn't start with something like "With" or "Replace", quit
            string methodName = invocation.TargetMethod.Name;
            if (!s_immutableMethodNames.Any(n => methodName.StartsWith(n, StringComparison.Ordinal)))
            {
                return;
            }

            // If we're not in one of the known immutable types, quit
            if (invocation.GetReceiverType(context.Compilation, beforeConversion: false, context.CancellationToken) is INamedTypeSymbol type
                && type.GetBaseTypesAndThis().Any(immutableTypeSymbols.Contains))
            {
                context.ReportDiagnostic(invocation.CreateDiagnostic(DoNotIgnoreReturnValueDiagnosticRule, type.Name, methodName));
            }
        }
    }
}
