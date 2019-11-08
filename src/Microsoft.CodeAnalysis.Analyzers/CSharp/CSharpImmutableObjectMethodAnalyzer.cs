// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpImmutableObjectMethodAnalyzer : DiagnosticAnalyzer
    {
        // Each analyzer needs a public id to identify each DiagnosticDescriptor and subsequently fix diagnostics in CodeFixProvider.cs
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor DoNotIgnoreReturnValueDiagnosticRule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocation,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DoNotIgnoreReturnValueDiagnosticRule);

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

            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (!compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(SolutionFullName, out var solutionSymbol) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(ProjectFullName, out var projectSymbol) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(DocumentFullName, out var documentSymbol) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(SyntaxNodeFullName, out var syntaxNodeSymbol) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(CompilationFullName, out var compilationSymbol))
                {
                    // Only register our node action if we can find the symbols for our immutable types
                    return;
                }

                var immutableSymbols = ImmutableHashSet.Create(solutionSymbol, projectSymbol, documentSymbol, syntaxNodeSymbol, compilationSymbol);
                compilationContext.RegisterSyntaxNodeAction(sc => AnalyzeInvocationForIgnoredReturnValue(sc, immutableSymbols), SyntaxKind.InvocationExpression);
            });
        }

        public static void AnalyzeInvocationForIgnoredReturnValue(SyntaxNodeAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> immutableTypeSymbols)
        {
            SemanticModel model = context.SemanticModel;
            var candidateInvocation = (InvocationExpressionSyntax)context.Node;

            //We're looking for invocations that are direct children of expression statements
            if (!(candidateInvocation.Parent.IsKind(SyntaxKind.ExpressionStatement)))
            {
                return;
            }

            //If we can't find the method symbol, quit
            if (!(model.GetSymbolInfo(candidateInvocation).Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            //If the method doesn't start with something like "With" or "Replace", quit
            string methodName = methodSymbol.Name;
            if (!s_immutableMethodNames.Any(n => methodName.StartsWith(n, StringComparison.Ordinal)))
            {
                return;
            }

            //If we're not in one of the known immutable types, quit
            if (!(methodSymbol.ReceiverType is INamedTypeSymbol parentType))
            {
                return;
            }

            var baseTypesAndSelf = methodSymbol.ReceiverType.GetBaseTypes().ToList();
            baseTypesAndSelf.Add(parentType);

            if (!baseTypesAndSelf.Any(n => immutableTypeSymbols.Contains(n)))
            {
                return;
            }

            Location location = candidateInvocation.GetLocation();
            Diagnostic diagnostic = Diagnostic.Create(DoNotIgnoreReturnValueDiagnosticRule, location, methodSymbol.ReceiverType.Name, methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
