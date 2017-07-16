// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpAddAccessibilityModifiersDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpAddAccessibilityModifiersDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Add_accessibility_modifiers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Accessibility_modifiers_required), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;

            var workspaceAnalyzerOptions = context.Options as WorkspaceAnalyzerOptions;
            if (workspaceAnalyzerOptions == null)
            {
                return;
            }

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.RequireAccessibilityModifiers, semanticModel.Language);
            if (!option.Value)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(workspaceAnalyzerOptions.Services.Workspace, semanticModel.Language);
            Recurse(context, generator, option.Notification.Value, syntaxTree.GetRoot(cancellationToken));
        }

        private void Recurse(
            SemanticModelAnalysisContext context, SyntaxGenerator generator, 
            DiagnosticSeverity severity, SyntaxNode node)
        {
            var cancellationToken = context.CancellationToken;
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    ProcessNode(context, generator, severity, child.AsNode());
                }
            }
        }

        private void ProcessNode(
            SemanticModelAnalysisContext context, SyntaxGenerator generator,
            DiagnosticSeverity severity, SyntaxNode node)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;

            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (symbol != null)
            {
                ProcessSymbol(context, generator, severity, symbol, node);

                // Only recurse into namespaces and types.
                if (symbol.Kind != SymbolKind.NamedType &&
                    symbol.Kind != SymbolKind.Namespace)
                {
                    return;
                }
            }

            Recurse(context, generator, severity, node);
        }

        private void ProcessSymbol(
            SemanticModelAnalysisContext context, SyntaxGenerator generator,
            DiagnosticSeverity severity, ISymbol symbol, SyntaxNode declaration)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (symbol.Kind == SymbolKind.Field && !IsFirstFieldDeclarator(declaration))
            {
                return;
            }

            if (!CanHaveModifiers(symbol))
            {
                return;
            }

            var declaredAccessibility = generator.GetAccessibility(declaration);
            if (declaredAccessibility == Accessibility.NotApplicable)
            {
                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                context.ReportDiagnostic(Diagnostic.Create(
                    CreateDescriptorWithSeverity(severity),
                    GetPreferredLocation(symbol.Locations, context.SemanticModel.SyntaxTree, declaration),
                    additionalLocations: additionalLocations));
            }
        }

        private bool CanHaveModifiers(ISymbol symbol)
        {
            if (symbol.Kind == SymbolKind.Method)
            {
                var method = (IMethodSymbol)symbol;
                if (method.MethodKind == MethodKind.Destructor ||
                    method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                    method.MethodKind == MethodKind.SharedConstructor)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsFirstFieldDeclarator(SyntaxNode declarator)
        {
            var variableDeclaration = (VariableDeclarationSyntax)declarator.Parent;
            return variableDeclaration.Variables[0] == declarator;
        }

        private Location GetPreferredLocation(
            ImmutableArray<Location> locations, SyntaxTree tree, SyntaxNode declaration)
        {
            foreach (var location in locations)
            {
                if (location.SourceTree == tree && location.SourceSpan.IntersectsWith(declaration.Span))
                {
                    return location;
                }
            }

            throw new InvalidOperationException();
        }
    }
}
