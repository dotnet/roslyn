// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpImmutableObjectMethodAnalyzer : DiagnosticAnalyzer
    {
        // Each analyzer needs a public id to identify each DiagnosticDescriptor and subsequently fix diagnostics in CodeFixProvider.cs
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocationDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor DoNotIgnoreReturnValueDiagnosticRule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotIgnoreReturnValueOnImmutableObjectMethodInvocation,
            s_localizableTitle,
            s_localizableMessage,
            AnalyzerDiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DoNotIgnoreReturnValueDiagnosticRule);

        private static readonly string s_solutionFullName = @"Microsoft.CodeAnalysis.Solution";
        private static readonly string s_projectFullName = @"Microsoft.CodeAnalysis.Project";
        private static readonly string s_documentFullName = @"Microsoft.CodeAnalysis.Document";
        private static readonly string s_syntaxNodeFullName = @"Microsoft.CodeAnalysis.SyntaxNode";
        private static readonly string s_compilationFullName = @"Microsoft.CodeAnalysis.Compilation";

        private static readonly string s_Add = "Add";
        private static readonly string s_Remove = "Remove";
        private static readonly string s_Replace = "Replace";
        private static readonly string s_With = "With";

        private static readonly ImmutableArray<string> s_immutableMethodNames = ImmutableArray.Create(
            s_Add,
            s_Remove,
            s_Replace,
            s_With);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol solutionSymbol = compilationContext.Compilation.GetTypeByMetadataName(s_solutionFullName);
                INamedTypeSymbol projectSymbol = compilationContext.Compilation.GetTypeByMetadataName(s_projectFullName);
                INamedTypeSymbol documentSymbol = compilationContext.Compilation.GetTypeByMetadataName(s_documentFullName);
                INamedTypeSymbol syntaxNodeSymbol = compilationContext.Compilation.GetTypeByMetadataName(s_syntaxNodeFullName);
                INamedTypeSymbol compilationSymbol = compilationContext.Compilation.GetTypeByMetadataName(s_compilationFullName);

                ImmutableArray<INamedTypeSymbol> immutableSymbols = ImmutableArray.Create(solutionSymbol, projectSymbol, documentSymbol, syntaxNodeSymbol, compilationSymbol);
                //Only register our node action if we can find the symbols for our immutable types
                if (immutableSymbols.Any(n => n == null))
                {
                    return;
                }
                compilationContext.RegisterSyntaxNodeAction(sc => AnalyzeInvocationForIgnoredReturnValue(sc, immutableSymbols), SyntaxKind.InvocationExpression);
            });
        }

        public void AnalyzeInvocationForIgnoredReturnValue(SyntaxNodeAnalysisContext context, ImmutableArray<INamedTypeSymbol> immutableTypeSymbols)
        {
            SemanticModel model = context.SemanticModel;
            var candidateInvocation = (InvocationExpressionSyntax)context.Node;

            //We're looking for invocations that are direct children of expression statements
            if (!(candidateInvocation.Parent.IsKind(SyntaxKind.ExpressionStatement)))
            {
                return;
            }

            //If we can't find the method symbol, quit
            var methodSymbol = model.GetSymbolInfo(candidateInvocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            //If the method doesn't start with something like "With" or "Replace", quit
            string methodName = methodSymbol.Name;
            if (!s_immutableMethodNames.Any(n => methodName.StartsWith(n)))
            {
                return;
            }

            //If we're not in one of the known immutable types, quit
            var parentType = methodSymbol.ReceiverType as INamedTypeSymbol;
            if (parentType == null)
            {
                return;
            }

            System.Collections.Generic.List<INamedTypeSymbol> baseTypesAndSelf = methodSymbol.ReceiverType.GetBaseTypes().ToList();
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
