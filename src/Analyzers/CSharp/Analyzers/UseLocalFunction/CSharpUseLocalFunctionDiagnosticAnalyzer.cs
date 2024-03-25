// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

namespace Microsoft.CodeAnalysis.CSharp.UseLocalFunction;

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
               EnforceOnBuildValues.UseLocalFunction,
               CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Use_local_function), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;

            // Local functions are only available in C# 7.0 and above.  Don't offer this refactoring
            // in projects targeting a lesser version.
            if (compilation.LanguageVersion() < LanguageVersion.CSharp7)
                return;

            var expressionType = compilation.GetTypeByMetadataName(typeof(Expression<>).FullName!);

            // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
            // get callbacks for lambda expression nodes, but analyze nodes across the entire code block
            // and eventually report a diagnostic on the local declaration node.
            // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
            // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
            context.RegisterCodeBlockStartAction<SyntaxKind>(blockStartContext =>
                blockStartContext.RegisterSyntaxNodeAction(ctx => SyntaxNodeAction(ctx, expressionType),
                    SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.AnonymousMethodExpression));
        });
    }

    private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext, INamedTypeSymbol? expressionType)
    {
        var styleOption = syntaxContext.GetCSharpAnalyzerOptions().PreferLocalOverAnonymousFunction;
        // Bail immediately if the user has disabled this feature.
        if (!styleOption.Value || ShouldSkipAnalysis(syntaxContext, styleOption.Notification))
            return;

        var anonymousFunction = (AnonymousFunctionExpressionSyntax)syntaxContext.Node;

        var semanticModel = syntaxContext.SemanticModel;
        if (!CheckForPattern(anonymousFunction, out var localDeclaration))
            return;

        if (localDeclaration.Declaration.Variables.Count != 1)
            return;

        if (localDeclaration.Parent is not BlockSyntax block)
            return;

        // If there are compiler error on the declaration we can't reliably
        // tell that the refactoring will be accurate, so don't provide any
        // code diagnostics
        if (localDeclaration.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return;

        // Bail out if none of possible diagnostic locations are within the analysis span
        var anonymousFunctionStatement = anonymousFunction.GetAncestor<StatementSyntax>();
        var shouldReportOnAnonymousFunctionStatement = anonymousFunctionStatement != null
            && localDeclaration != anonymousFunctionStatement;
        if (!IsInAnalysisSpan(syntaxContext, localDeclaration, anonymousFunctionStatement, shouldReportOnAnonymousFunctionStatement))
            return;

        var cancellationToken = syntaxContext.CancellationToken;
        var local = semanticModel.GetDeclaredSymbol(localDeclaration.Declaration.Variables[0], cancellationToken);
        if (local == null)
            return;

        var delegateType = semanticModel.GetTypeInfo(anonymousFunction, cancellationToken).ConvertedType as INamedTypeSymbol;
        if (!delegateType.IsDelegateType() ||
            delegateType.DelegateInvokeMethod == null ||
            !CanReplaceDelegateWithLocalFunction(delegateType, localDeclaration, semanticModel, cancellationToken))
        {
            return;
        }

        if (!CanReplaceAnonymousWithLocalFunction(semanticModel, expressionType, local, block, anonymousFunction, out var referenceLocations, cancellationToken))
            return;

        if (localDeclaration.Declaration.Type.IsVar)
        {
            var options = semanticModel.SyntaxTree.Options;
            if (options.LanguageVersion() < LanguageVersion.CSharp10)
                return;
        }

        // Looks good!
        var additionalLocations = ImmutableArray.Create(
            localDeclaration.GetLocation(),
            anonymousFunction.GetLocation());

        additionalLocations = additionalLocations.AddRange(referenceLocations);

        if (styleOption.Notification.Severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) < ReportDiagnostic.Hidden)
        {
            // If the diagnostic is not hidden, then just place the user visible part
            // on the local being initialized with the lambda.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                localDeclaration.Declaration.Variables[0].Identifier.GetLocation(),
                styleOption.Notification,
                syntaxContext.Options,
                additionalLocations,
                properties: null));
        }
        else
        {
            // If the diagnostic is hidden, place it on the entire construct.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                localDeclaration.GetLocation(),
                styleOption.Notification,
                syntaxContext.Options,
                additionalLocations,
                properties: null));

            if (shouldReportOnAnonymousFunctionStatement)
            {
                syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    anonymousFunctionStatement!.GetLocation(),
                    styleOption.Notification,
                    syntaxContext.Options,
                    additionalLocations,
                    properties: null));
            }
        }

        static bool IsInAnalysisSpan(
            SyntaxNodeAnalysisContext context,
            LocalDeclarationStatementSyntax localDeclaration,
            StatementSyntax? anonymousFunctionStatement,
            bool shouldReportOnAnonymousFunctionStatement)
        {
            if (context.ShouldAnalyzeSpan(localDeclaration.Span))
                return true;

            if (shouldReportOnAnonymousFunctionStatement
                && context.ShouldAnalyzeSpan(anonymousFunctionStatement!.Span))
            {
                return true;
            }

            return false;
        }
    }

    private static bool CheckForPattern(
        AnonymousFunctionExpressionSyntax anonymousFunction,
        [NotNullWhen(true)] out LocalDeclarationStatementSyntax? localDeclaration)
    {
        // Look for:
        //
        // Type t = <anonymous function>
        // var t = (Type)(<anonymous function>)
        //
        // Type t = null;
        // t = <anonymous function>
        return CheckForSimpleLocalDeclarationPattern(anonymousFunction, out localDeclaration) ||
               CheckForCastedLocalDeclarationPattern(anonymousFunction, out localDeclaration) ||
               CheckForLocalDeclarationAndAssignment(anonymousFunction, out localDeclaration);
    }

    private static bool CheckForSimpleLocalDeclarationPattern(
        AnonymousFunctionExpressionSyntax anonymousFunction,
        [NotNullWhen(true)] out LocalDeclarationStatementSyntax? localDeclaration)
    {
        // Type t = <anonymous function>
        if (anonymousFunction is
            {
                Parent: EqualsValueClauseSyntax
                {
                    Parent: VariableDeclaratorSyntax
                    {
                        Parent: VariableDeclarationSyntax
                        {
                            Parent: LocalDeclarationStatementSyntax declaration,
                        }
                    }
                }
            })
        {
            localDeclaration = declaration;
            return true;
        }

        localDeclaration = null;
        return false;
    }

    private static bool CanReplaceAnonymousWithLocalFunction(
        SemanticModel semanticModel, INamedTypeSymbol? expressionTypeOpt, ISymbol local, BlockSyntax block,
        AnonymousFunctionExpressionSyntax anonymousFunction, out ImmutableArray<Location> referenceLocations, CancellationToken cancellationToken)
    {
        // Check all the references to the anonymous function and disallow the conversion if
        // they're used in certain ways.
        using var _ = ArrayBuilder<Location>.GetInstance(out var references);
        referenceLocations = [];
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

            if (descendentNode is IdentifierNameSyntax identifierName)
            {
                if (identifierName.Identifier.ValueText == local.Name &&
                    local.Equals(semanticModel.GetSymbolInfo(identifierName, cancellationToken).GetAnySymbol()))
                {
                    if (identifierName.IsWrittenTo(semanticModel, cancellationToken))
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

        referenceLocations = references.ToImmutableAndClear();
        return true;
    }

    private static bool CheckForCastedLocalDeclarationPattern(
        AnonymousFunctionExpressionSyntax anonymousFunction,
        [NotNullWhen(true)] out LocalDeclarationStatementSyntax? localDeclaration)
    {
        // var t = (Type)(<anonymous function>)
        var containingStatement = anonymousFunction.GetAncestor<StatementSyntax>();
        if (containingStatement.IsKind(SyntaxKind.LocalDeclarationStatement, out localDeclaration) &&
            localDeclaration.Declaration.Variables.Count == 1)
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

        localDeclaration = null;
        return false;
    }

    private static bool CheckForLocalDeclarationAndAssignment(
        AnonymousFunctionExpressionSyntax anonymousFunction,
        [NotNullWhen(true)] out LocalDeclarationStatementSyntax? localDeclaration)
    {
        // Type t = null;
        // t = <anonymous function>
        if (anonymousFunction?.Parent is AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression) assignment &&
            assignment.Parent is ExpressionStatementSyntax expressionStatement &&
            expressionStatement.Parent is BlockSyntax block)
        {
            if (assignment.Left.IsKind(SyntaxKind.IdentifierName))
            {
                var expressionStatementIndex = block.Statements.IndexOf(expressionStatement);
                if (expressionStatementIndex >= 1)
                {
                    var previousStatement = block.Statements[expressionStatementIndex - 1];
                    if (previousStatement.IsKind(SyntaxKind.LocalDeclarationStatement, out localDeclaration) &&
                        localDeclaration.Declaration.Variables.Count == 1)
                    {
                        var variableDeclarator = localDeclaration.Declaration.Variables[0];
                        if (variableDeclarator.Initializer == null ||
                            variableDeclarator.Initializer.Value.Kind() is
                                SyntaxKind.NullLiteralExpression or
                                SyntaxKind.DefaultLiteralExpression or
                                SyntaxKind.DefaultExpression)
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
                if (typeParams.Any(static (p, delegateTypeParamNames) => delegateTypeParamNames.Contains(p.Name), delegateTypeParamNames))
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
