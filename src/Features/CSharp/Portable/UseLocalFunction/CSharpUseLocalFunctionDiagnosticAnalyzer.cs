// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
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
    internal class CSharpUseLocalFunctionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseLocalFunctionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseLocalFunctionDiagnosticId,
                   CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                       nameof(FeaturesResources.Use_local_function), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var compilation = compilationContext.Compilation;
                var expressionTypeOpt = compilation.GetTypeByMetadataName(typeof(Expression<>).FullName);

                context.RegisterSyntaxNodeAction(ctx => SyntaxNodeAction(ctx, expressionTypeOpt),
                    SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.AnonymousMethodExpression);
            });
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext, INamedTypeSymbol expressionTypeOpt)
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

            var severity = styleOption.Notification.Severity;
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

            // If there are compiler error on the declaration we can't reliably
            // tell that the refactoring will be accurate, so don't provide any
            // code diagnostics
            if (localDeclaration.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            var local = semanticModel.GetDeclaredSymbol(localDeclaration.Declaration.Variables[0], cancellationToken);
            if (local == null)
            {
                return;
            }

            var delegateType = semanticModel.GetTypeInfo(anonymousFunction, cancellationToken).ConvertedType as INamedTypeSymbol;
            if (!delegateType.IsDelegateType() ||
                delegateType.DelegateInvokeMethod == null ||
                !CanReplaceDelegateWithLocalFunction(delegateType, localDeclaration, semanticModel, cancellationToken))
            {
                return;
            }

            if (!CanReplaceAnonymousWithLocalFunction(semanticModel, expressionTypeOpt, local, block, anonymousFunction, out var referenceLocations, cancellationToken))
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                localDeclaration.GetLocation(),
                anonymousFunction.GetLocation());

            additionalLocations = additionalLocations.AddRange(referenceLocations);

            if (severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) < ReportDiagnostic.Hidden)
            {
                // If the diagnostic is not hidden, then just place the user visible part
                // on the local being initialized with the lambda.
                syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    localDeclaration.Declaration.Variables[0].Identifier.GetLocation(),
                    severity,
                    additionalLocations,
                    properties: null));
            }
            else
            {
                // If the diagnostic is hidden, place it on the entire construct.
                syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    localDeclaration.GetLocation(),
                    severity,
                    additionalLocations,
                    properties: null));

                var anonymousFunctionStatement = anonymousFunction.GetAncestor<StatementSyntax>();
                if (localDeclaration != anonymousFunctionStatement)
                {
                    syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                        Descriptor,
                        anonymousFunctionStatement.GetLocation(),
                        severity,
                        additionalLocations,
                        properties: null));
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

        private bool CanReplaceAnonymousWithLocalFunction(
            SemanticModel semanticModel, INamedTypeSymbol expressionTypeOpt, ISymbol local, BlockSyntax block,
            AnonymousFunctionExpressionSyntax anonymousFunction, out ImmutableArray<Location> referenceLocations, CancellationToken cancellationToken)
        {
            // Check all the references to the anonymous function and disallow the conversion if
            // they're used in certain ways.
            var references = ArrayBuilder<Location>.GetInstance();
            referenceLocations = ImmutableArray<Location>.Empty;
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
                        local.Equals(semanticModel.GetSymbolInfo(identifierName, cancellationToken).GetAnySymbol()))
                    {
                        if (identifierName.IsWrittenTo())
                        {
                            // Can't change this to a local function if it is assigned to.
                            return false;
                        }

                        var nodeToCheck = identifierName.WalkUpParentheses();
                        if (nodeToCheck.Parent is BinaryExpressionSyntax)
                        {
                            // Can't change this if they're doing things like delegate addition with
                            // the lambda.
                            return false;
                        }

                        if (nodeToCheck.Parent is InvocationExpressionSyntax invocationExpression)
                        {
                            references.Add(invocationExpression.GetLocation());
                        }
                        else if (nodeToCheck.Parent is MemberAccessExpressionSyntax memberAccessExpression)
                        {
                            if (memberAccessExpression.Parent is InvocationExpressionSyntax explicitInvocationExpression &&
                                memberAccessExpression.Name.Identifier.ValueText == WellKnownMemberNames.DelegateInvokeName)
                            {
                                references.Add(explicitInvocationExpression.GetLocation());
                            }
                            else
                            {
                                // They're doing something like "del.ToString()".  Can't do this with a
                                // local function.
                                return false;
                            }
                        }
                        else
                        {
                            references.Add(nodeToCheck.GetLocation());
                        }

                        var convertedType = semanticModel.GetTypeInfo(nodeToCheck, cancellationToken).ConvertedType;
                        if (!convertedType.IsDelegateType())
                        {
                            // We can't change this anonymous function into a local function if it is
                            // converted to a non-delegate type (i.e. converted to 'object' or 
                            // 'System.Delegate'). Local functions are not convertible to these types.  
                            // They're only convertible to other delegate types.
                            return false;
                        }

                        if (nodeToCheck.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken))
                        {
                            // Can't reference a local function inside an expression tree.
                            return false;
                        }
                    }
                }
            }

            referenceLocations = references.ToImmutableAndFree();
            return true;
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
                                if (variableDeclarator.Initializer == null ||
                                    variableDeclarator.Initializer.Value.IsKind(
                                        SyntaxKind.NullLiteralExpression,
                                        SyntaxKind.DefaultLiteralExpression,
                                        SyntaxKind.DefaultExpression))
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

            localDeclaration = null;
            return false;
        }

        private static bool CanReplaceDelegateWithLocalFunction(
            INamedTypeSymbol delegateType,
            LocalDeclarationStatementSyntax localDeclaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var delegateContainingType = delegateType.ContainingType;
            if (delegateContainingType is null || !delegateContainingType.IsGenericType)
            {
                return true;
            }

            var delegateTypeParamNames = delegateType.GetAllTypeParameters().Select(p => p.Name).ToImmutableHashSet();
            var localEnclosingSymbol = semanticModel.GetEnclosingSymbol(localDeclaration.SpanStart, cancellationToken);
            while (localEnclosingSymbol != null)
            {
                if (localEnclosingSymbol.Equals(delegateContainingType))
                {
                    return true;
                }

                var typeParams = localEnclosingSymbol.GetTypeParameters();
                if (typeParams.Any())
                {
                    if (typeParams.Any(p => delegateTypeParamNames.Contains(p.Name)))
                    {
                        return false;
                    }
                }

                localEnclosingSymbol = localEnclosingSymbol.ContainingType;
            }

            return true;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
