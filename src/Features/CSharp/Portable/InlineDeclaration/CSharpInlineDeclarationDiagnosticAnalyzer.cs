// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.InlineDeclaration
{
    /// <summary>
    /// Looks for code of the form:
    /// 
    ///     int i;
    ///     if (int.TryParse(s, out i)) { }
    ///     
    /// And offers to convert it to:
    /// 
    ///     if (int.TryParse(s, out var i)) { }   or
    ///     if (int.TryParse(s, out int i)) { }   or
    /// 
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpInlineDeclarationDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.Inline_variable_declaration), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FeaturesResources.Inline_variable_declaration), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        private static DiagnosticDescriptor s_descriptor =
            CreateDescriptor(DiagnosticSeverity.Hidden);

        private static DiagnosticDescriptor s_unnecessaryCodeDescriptor =
            CreateDescriptor(DiagnosticSeverity.Hidden, customTags: DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(s_descriptor, s_unnecessaryCodeDescriptor);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        public bool OpenFileOnly(Workspace workspace) => false;

        private static DiagnosticDescriptor CreateDescriptor(DiagnosticSeverity severity, params string[] customTags)
            => new DiagnosticDescriptor(
                    IDEDiagnosticIds.InlineDeclarationDiagnosticId,
                    s_localizableTitle,
                    s_localizableMessage,
                    DiagnosticCategory.Style,
                    severity,
                    isEnabledByDefault: true,
                    customTags: customTags);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.Argument);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var argumentNode = (ArgumentSyntax)context.Node;
            var csOptions = (CSharpParseOptions)context.Node.SyntaxTree.Options;
            if (csOptions.LanguageVersion < LanguageVersion.CSharp7)
            {
                // out-vars are not supported prior to C# 7.0.
                return;
            }

            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferInlinedVariableDeclaration, argumentNode.Language);
            if (!option.Value)
            {
                return;
            }

            if (argumentNode.RefOrOutKeyword.Kind() != SyntaxKind.OutKeyword)
            {
                return;
            }

            var argumentList = argumentNode.Parent as ArgumentListSyntax;
            if (argumentList == null)
            {
                return;
            }

            var invocationOrCreation = argumentList.Parent;
            if (!invocationOrCreation.IsKind(SyntaxKind.InvocationExpression) &&
                !invocationOrCreation.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                return;
            }

            var argumentExpression = argumentNode.Expression;
            if (argumentExpression.Kind() != SyntaxKind.IdentifierName)
            {
                return;
            }

            var identifierName = (IdentifierNameSyntax)argumentExpression;

            var containingStatement = argumentExpression.FirstAncestorOrSelf<StatementSyntax>();
            if (containingStatement == null)
            {
                return;
            }
            
            var enclosingBlockOfOutArgument = containingStatement.Parent as BlockSyntax;
            if (enclosingBlockOfOutArgument == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            var outSymbol = semanticModel.GetSymbolInfo(argumentExpression, cancellationToken).Symbol;
            if (outSymbol?.Kind != SymbolKind.Local)
            {
                return;
            }

            var localReference = outSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            var localDeclarator = localReference?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
            if (localDeclarator == null)
            {
                return;
            }

            // If the local has an initializer, only allow the refactoring if it is initialized
            // with a simple literal or 'default' expression.
            if (localDeclarator.Initializer != null)
            {
                if (!(localDeclarator.Initializer.Value is LiteralExpressionSyntax) &&
                    !(localDeclarator.Initializer.Value is DefaultExpressionSyntax))
                {
                    return;
                }
            }

            if (localDeclarator.SpanStart >= argumentNode.SpanStart)
            {
                // We have an error situation where the local was declared after the out-var.  
                // Don't even bother offering anything here.
                return;
            }

            var localDeclaration = localDeclarator.Parent as VariableDeclarationSyntax;
            var localStatement = localDeclaration?.Parent as LocalDeclarationStatementSyntax;
            if (localStatement == null)
            {
                return;
            }

            var enclosingBlockOfLocalStatement = localStatement.Parent as BlockSyntax;
            if (enclosingBlockOfLocalStatement == null)
            {
                return;
            }

            var dataFlow = semanticModel.AnalyzeDataFlow(enclosingBlockOfOutArgument);
            if (dataFlow.ReadOutside.Contains(outSymbol) || dataFlow.WrittenOutside.Contains(outSymbol))
            {
                // The variable is read or written from outside the block that the new variable
                // would be scoped in.  This would cause a break.
                //
                // Note(cyrusn): We coudl still offer the refactoring, but just show an error in the
                // preview in this case.
                return;
            }

            // Make sure the variable isn't ever acessed before the usage in this out-var.
            if (IsAccessed(semanticModel, outSymbol, enclosingBlockOfLocalStatement, 
                           localStatement, argumentNode, cancellationToken))
            {
                return;
            }

            var allLocations = ImmutableArray.Create(
                localDeclarator.GetLocation(),
                identifierName.GetLocation(),
                invocationOrCreation.GetLocation());

            // If the local variable only has one declarator, then report the suggestion on the whole
            // declaration.  Otherwise, return the suggestion only on the single declarator.
            var reportNode = localDeclaration.Variables.Count == 1
                ? (SyntaxNode)localDeclaration
                : localDeclarator;

            var descriptor = CreateDescriptor(option.Notification.Value);

            context.ReportDiagnostic(
                Diagnostic.Create(descriptor, reportNode.GetLocation(), additionalLocations: allLocations));
        }

        private bool IsAccessed(
            SemanticModel semanticModel,
            ISymbol outSymbol, 
            BlockSyntax enclosingBlockOfLocalStatement,
            LocalDeclarationStatementSyntax localStatement, 
            ArgumentSyntax argumentNode,
            CancellationToken cancellationToken)
        {
            var localStatementStart = localStatement.Span.Start;
            var argumentNodeStart = argumentNode.Span.Start;
            var variableName = outSymbol.Name;

            foreach (var descendentNode in enclosingBlockOfLocalStatement.DescendantNodes())
            {
                var descendentStart = descendentNode.Span.Start;
                if (descendentStart <= localStatementStart)
                {
                    // This node is before the local declaration.  Can ignore it entirely as it could
                    // not be an access to the local.
                    continue;
                }

                if (descendentStart >= argumentNodeStart)
                {
                    // We reached the out-var.  We can stop searching entirely.
                    break;
                }

                if (descendentNode.IsKind(SyntaxKind.IdentifierName))
                {
                    // See if this looks like an accessor to the local variable syntactically.
                    var identifierName = (IdentifierNameSyntax)descendentNode;
                    if (identifierName.Identifier.ValueText == variableName)
                    {
                        // Confirm that it is a access of the local.
                        var symbol = semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol;
                        if (outSymbol.Equals(symbol))
                        {
                            return true;
                        }
                    }
                }
            }

            // No accesses detected
            return false;
        }
    }
}