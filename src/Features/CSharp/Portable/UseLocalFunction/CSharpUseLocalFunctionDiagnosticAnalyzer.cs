// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseLocalFunction
{
    /// <summary>
    /// Looks for code of the form:
    /// 
    ///     Func&lt;int, int&gt; fib = n =>
    ///     {
    ///         if (n &lt;= 2)
    ///             return 1
    ///             
    ///         return fib(n - 1) + fib(n - 2);
    ///     }
    ///     
    /// and converts it to:
    /// 
    ///     int fib(int n)
    ///     {
    ///         if (n &lt;= 2)
    ///             return 1
    ///             
    ///         return fib(n - 1) + fib(n - 2);
    ///     }
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseLocalFunctionDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public override bool OpenFileOnly(Workspace workspace) => false;

        public CSharpUseLocalFunctionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseLocalFunctionDiagnosticId,
                   new LocalizableResourceString(
                       nameof(FeaturesResources.Use_local_function), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var options = syntaxContext.Options;
            var syntaxTree = syntaxContext.Node.SyntaxTree;
            var cancellationToken = syntaxContext.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var styleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            // Local functions are only available in C# 7.0 and above.  Don't offer this refactoring
            // in projects targetting a lesser version.
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            var severity = styleOption.Notification.Value;
            var anonymousFunction = (AnonymousFunctionExpressionSyntax)syntaxContext.Node;

            var semanticModel = syntaxContext.SemanticModel;
            if (!CheckForPattern(semanticModel, anonymousFunction, cancellationToken,
                    out var localDeclaration))
            {
                return;
            }

            if (localDeclaration.Declaration.Variables.Count != 1)
            {
                return;
            }

            if (!(localDeclaration.Parent is BlockSyntax block))
            {
                return;
            }

            var local = semanticModel.GetDeclaredSymbol(localDeclaration.Declaration.Variables[0], cancellationToken);
            if (local == null)
            {
                return;
            }

            if (IsWrittenAfter(semanticModel, local, block, anonymousFunction, cancellationToken))
            {
                return;
            }

            var delegateType = semanticModel.GetTypeInfo(anonymousFunction, cancellationToken).ConvertedType as INamedTypeSymbol;
            if (!delegateType.IsDelegateType() ||
                delegateType.DelegateInvokeMethod == null)
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                localDeclaration.GetLocation(),
                anonymousFunction.GetLocation());

            if (severity != DiagnosticSeverity.Hidden)
            {
                // If the diagnostic is not hidden, then just place the user visible part
                // on the local being initialized with the lambda.
                syntaxContext.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptorWithSeverity(severity),
                    localDeclaration.Declaration.Variables[0].Identifier.GetLocation(),
                    additionalLocations));
            }
            else
            {
                // If the diagnostic is hidden, place it on the entire construct.
                syntaxContext.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptorWithSeverity(severity),
                    localDeclaration.GetLocation(),
                    additionalLocations));

                var anonymousFunctionStatement = anonymousFunction.GetAncestor<StatementSyntax>();
                if (localDeclaration != anonymousFunctionStatement)
                {
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(
                        GetDescriptorWithSeverity(severity),
                        anonymousFunctionStatement.GetLocation(),
                        additionalLocations));
                }
            }
        }

        private bool CheckForPattern(
            SemanticModel semanticModel,
            AnonymousFunctionExpressionSyntax anonymousFunction,
            CancellationToken cancellationToken,
            out LocalDeclarationStatementSyntax localDeclaration)
        {
            // Look for:
            //
            // Type t = <anonymous function>
            // var t = (Type)(<anonymous function>)
            //
            // Type t = null;
            // t = <anonymous function>
            return CheckForSimpleLocalDeclarationPattern(semanticModel, anonymousFunction, cancellationToken, out localDeclaration) ||
                   CheckForCastedLocalDeclarationPattern(semanticModel, anonymousFunction, cancellationToken, out localDeclaration) ||
                   CheckForLocalDeclarationAndAssignment(semanticModel, anonymousFunction, cancellationToken, out localDeclaration);
        }

        private bool CheckForSimpleLocalDeclarationPattern(
            SemanticModel semanticModel, 
            AnonymousFunctionExpressionSyntax anonymousFunction,
            CancellationToken cancellationToken,
            out LocalDeclarationStatementSyntax localDeclaration)
        {
            // Type t = <anonymous function>
            if (anonymousFunction.IsParentKind(SyntaxKind.EqualsValueClause) &&
                anonymousFunction.Parent.IsParentKind(SyntaxKind.VariableDeclarator) &&
                anonymousFunction.Parent.Parent.IsParentKind(SyntaxKind.VariableDeclaration) &&
                anonymousFunction.Parent.Parent.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement))
            {
                localDeclaration = (LocalDeclarationStatementSyntax)anonymousFunction.Parent.Parent.Parent.Parent;
                if (!localDeclaration.Declaration.Type.IsVar)
                {
                    return true;
                }
            }

            localDeclaration = null;
            return false;
        }

        private bool IsWrittenAfter(
            SemanticModel semanticModel, ISymbol local, BlockSyntax block,
            AnonymousFunctionExpressionSyntax anonymousFunction, CancellationToken cancellationToken)
        {
            var anonymousFunctionStart = anonymousFunction.SpanStart;
            foreach (var descendentNode in block.DescendantNodes())
            {
                var descendentStart = descendentNode.Span.Start;
                if (descendentStart <= anonymousFunctionStart)
                {
                    // This node is before the local declaration.  Can ignore it entirely as it could
                    // not be an access to the local.
                    continue;
                }

                if (descendentNode.IsKind(SyntaxKind.IdentifierName))
                {
                    var identifierName = (IdentifierNameSyntax)descendentNode;
                    if (identifierName.Identifier.ValueText == local.Name &&
                        identifierName.IsWrittenTo() &&
                        local.Equals(semanticModel.GetSymbolInfo(identifierName, cancellationToken).GetAnySymbol()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CheckForCastedLocalDeclarationPattern(
            SemanticModel semanticModel,
            AnonymousFunctionExpressionSyntax anonymousFunction,
            CancellationToken cancellationToken,
            out LocalDeclarationStatementSyntax localDeclaration)
        {
            // var t = (Type)(<anonymous function>)
            var containingStatement = anonymousFunction.GetAncestor<StatementSyntax>();
            if (containingStatement.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                localDeclaration = (LocalDeclarationStatementSyntax)containingStatement;
                if (localDeclaration.Declaration.Variables.Count == 1)
                {
                    var variableDeclarator = localDeclaration.Declaration.Variables[0];
                    if (variableDeclarator.Initializer != null)
                    {
                        var value = variableDeclarator.Initializer.Value.WalkDownParentheses();
                        if (value is CastExpressionSyntax castExpression)
                        {
                            if (castExpression.Expression.WalkDownParentheses() == anonymousFunction)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            localDeclaration = null;
            return false;
        }

        private bool CheckForLocalDeclarationAndAssignment(
            SemanticModel semanticModel,
            AnonymousFunctionExpressionSyntax anonymousFunction,
            CancellationToken cancellationToken,
            out LocalDeclarationStatementSyntax localDeclaration)
        {
            // Type t = null;
            // t = <anonymous function>
            if (anonymousFunction.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
                anonymousFunction.Parent.IsParentKind(SyntaxKind.ExpressionStatement) &&
                anonymousFunction.Parent.Parent.IsParentKind(SyntaxKind.Block))
            {
                var assignment = (AssignmentExpressionSyntax)anonymousFunction.Parent;
                if (assignment.Left.IsKind(SyntaxKind.IdentifierName))
                {
                    var expressionStatement = (ExpressionStatementSyntax)assignment.Parent;
                    var block = (BlockSyntax)expressionStatement.Parent;
                    var expressionStatementIndex = block.Statements.IndexOf(expressionStatement);
                    if (expressionStatementIndex >= 1)
                    {
                        var previousStatement = block.Statements[expressionStatementIndex - 1];
                        if (previousStatement.IsKind(SyntaxKind.LocalDeclarationStatement))
                        {
                            localDeclaration = (LocalDeclarationStatementSyntax)previousStatement;
                            if (localDeclaration.Declaration.Variables.Count == 1)
                            {
                                var variableDeclarator = localDeclaration.Declaration.Variables[0];
                                if (variableDeclarator.Initializer != null)
                                {
                                    var value = variableDeclarator.Initializer.Value;
                                    if (value.IsKind(SyntaxKind.NullLiteralExpression) ||
                                        value.IsKind(SyntaxKind.DefaultLiteralExpression) ||
                                        value.IsKind(SyntaxKind.DefaultExpression))
                                    {
                                        var identifierName = (IdentifierNameSyntax)assignment.Left;
                                        if (variableDeclarator.Identifier.ValueText == identifierName.Identifier.ValueText)
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            localDeclaration = null;
            return false;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
    }
}
