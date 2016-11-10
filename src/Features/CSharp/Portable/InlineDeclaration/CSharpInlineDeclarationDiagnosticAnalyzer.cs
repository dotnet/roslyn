﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal class CSharpInlineDeclarationDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpInlineDeclarationDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineDeclarationDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Inline_variable_declaration), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Variable_declaration_can_be_inlined), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        public bool OpenFileOnly(Workspace workspace) => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.Argument);

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
                // Don't bother doing any work if the user doesn't even have this preference set.
                return;
            }

            if (argumentNode.RefOrOutKeyword.Kind() != SyntaxKind.OutKeyword)
            {
                // Immediately bail if this is not an out-argument.  If it's not an out-argument
                // we clearly can't convert it to be an out-variable-declaration.
                return;
            }

            var argumentExpression = argumentNode.Expression;
            if (argumentExpression.Kind() != SyntaxKind.IdentifierName)
            {
                // has to be exactly the form "out i".  i.e. "out this.i" or "out v[i]" are legal
                // cases for out-arguments, but could not be converted to an out-variable-declaration.
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
                // Out-variables are only legal with invocations and object creations.
                // If we don't have one of those bail.  Note: we need hte parent to be
                // one of these forms so we can accurately verify that inlining the 
                // variable doesn't change semantics.
                return;
            }

            var identifierName = (IdentifierNameSyntax)argumentExpression;

            var containingStatement = argumentExpression.FirstAncestorOrSelf<StatementSyntax>();
            if (containingStatement == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            var outSymbol = semanticModel.GetSymbolInfo(argumentExpression, cancellationToken).Symbol;
            if (outSymbol?.Kind != SymbolKind.Local)
            {
                // The out-argument wasn't referencing a local.  So we don't have an local
                // declaration that we can attempt to inline here.
                return;
            }

            // Ensure that the local-symbol actually belongs to LocalDeclarationStatement.
            // Trying to do things like inline a var-decl in a for-statement is just too 
            // esoteric and would make us have to write a lot more complex code to support
            // that scenario.
            var localReference = outSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            var localDeclarator = localReference?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
            if (localDeclarator == null)
            {
                return;
            }

            var localDeclaration = localDeclarator.Parent as VariableDeclarationSyntax;
            var localStatement = localDeclaration?.Parent as LocalDeclarationStatementSyntax;
            if (localStatement == null)
            {
                return;
            }

            if (localDeclarator.SpanStart >= argumentNode.SpanStart)
            {
                // We have an error situation where the local was declared after the out-var.  
                // Don't even bother offering anything here.
                return;
            }

            // If the local has an initializer, only allow the refactoring if it is initialized
            // with a simple literal or 'default' expression.  i.e. it's ok to inline "var v = 0"
            // since there are no side-effects of the initialization.  However something like
            // "var v = M()" shoudl not be inlined as that could break program semantics.
            if (localDeclarator.Initializer != null)
            {
                if (!(localDeclarator.Initializer.Value is LiteralExpressionSyntax) &&
                    !(localDeclarator.Initializer.Value is DefaultExpressionSyntax))
                {
                    return;
                }
            }

            // Get the block that the local is scoped inside of.  We'll search that block
            // for references to the local to make sure that no reads/writes happen before
            // the out-argument.  If there are any reads/writes we can't inline as those
            // accesses will become invalid.
            var enclosingBlockOfLocalStatement = localStatement.Parent as BlockSyntax;
            if (enclosingBlockOfLocalStatement == null)
            {
                return;
            }

            // Find the scope that the out-declaration variable will live in after we
            // rewrite things.
            var outArgumentScope = containingStatement.Parent is BlockSyntax
                ? (BlockSyntax)containingStatement.Parent
                : containingStatement;

            // Make sure that variable is not accessed outside of that scope.
            var dataFlow = semanticModel.AnalyzeDataFlow(outArgumentScope);
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

            // Collect some useful nodes for the fix provider to use so it doesn't have to
            // find them again.
            var allLocations = ImmutableArray.Create(
                localDeclarator.GetLocation(),
                identifierName.GetLocation(),
                invocationOrCreation.GetLocation(),
                containingStatement.GetLocation());

            // If the local variable only has one declarator, then report the suggestion on the whole
            // declaration.  Otherwise, return the suggestion only on the single declarator.
            var reportNode = localDeclaration.Variables.Count == 1
                ? (SyntaxNode)localDeclaration
                : localDeclarator;

            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity(option.Notification.Value),
                reportNode.GetLocation(),
                additionalLocations: allLocations));
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

            // Walk the block that the local is declared in looking for accesses.
            // We can ignore anything prior to the actual local declaration point,
            // and we only need to check up until we reach the out-argument.
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
                            // We definitely accessed the local before the out-argument.  We 
                            // can't inline this local.
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