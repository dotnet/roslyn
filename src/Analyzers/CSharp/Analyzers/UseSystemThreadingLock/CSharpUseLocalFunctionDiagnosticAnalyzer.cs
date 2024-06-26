// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseLock;

/// <summary>
/// Looks for code of the form:
/// 
///     object _gate = new object();
///     ...
///     lock (_gate)
///     {
///     }
///     
/// and converts it to:
/// 
///     Lock _gate = new Lock();
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpUseSystemThreadingLockDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseSystemThreadingLockDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseSystemThreadingLockDiagnosticId,
               EnforceOnBuildValues.UseSystemThreadingLock,
               CSharpCodeStyleOptions.PreferSystemThreadingLock,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Use_System_Threading_Lock), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;

            // The new 'Lock' feature is only supported in C# 13 and above.
            if (!compilation.LanguageVersion().IsCSharp13OrAbove())
                return;

            var lockType = compilation.GetTypeByMetadataName("System.Threading.Lock");
            if (lockType is null)
                return;

            context.RegisterSymbolStartAction(context => AnalyzeNamedType(context, lockType), SymbolKind.NamedType);
        });
    }

    private void AnalyzeNamedType(SymbolStartAnalysisContext context, INamedTypeSymbol lockType)
    {
        var cancellationToken = context.CancellationToken;
        if (lockType is not
            {
                TypeKind: TypeKind.Class or TypeKind.Struct,
                DeclaringSyntaxReferences: [var reference, ..]
            })
        {
            return;
        }

        var syntaxTree = reference.GetSyntax(cancellationToken).SyntaxTree;
        var option = context.GetCSharpAnalyzerOptions(syntaxTree).PreferSystemThreadingLock;

        // Bail immediately if the user has disabled this feature.
        if (!option.Value || ShouldSkipAnalysis(syntaxTree, context.Options, context.Compilation.Options, option.Notification, cancellationToken))
            return;

        // Needs to have a private field that is exactly typed as 'object'
        using var fieldsArray = TemporaryArray<IFieldSymbol>.Empty;

        foreach (var member in lockType.GetMembers())
        {
            if (member is not IFieldSymbol
                {
                    Type.SpecialType: SpecialType.System_Object,
                    DeclaredAccessibility: Accessibility.Private,
                    DeclaringSyntaxReferences: [var fieldSyntaxReference],
                } field)
            {
                continue;
            }

            // If we have a private-object field, it needs to be initialized with either `new object()` or `new()`.
            if (fieldSyntaxReference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax fieldSyntax)
                continue;

            if (fieldSyntax.Initializer != null)
            {
                if (fieldSyntax.Initializer.Value
                        is not ImplicitObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 }
                        and not ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Type: PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.ObjectKeyword } })
                {
                    continue;
                }
            }

            // Looks like something that could be converted to a lock if we see that this is used as a lock.
            fieldsArray.Add(field);
        }

        if (fieldsArray.Count == 0)
            return;

        var potentialLockFields = new ConcurrentSet<IFieldSymbol>();
        var wasLockedSet = new ConcurrentSet<IFieldSymbol>();
        foreach (var field in fieldsArray)
            potentialLockFields.Add(field);

        context.RegisterOperationAction(context =>
        {
            var cancellationToken = context.CancellationToken;
            var fieldReferenceOperation = (IFieldReferenceOperation)context.Operation;
            var fieldReference = fieldReferenceOperation.Field.OriginalDefinition;

            if (!potentialLockFields.Contains(fieldReference))
                return;

            if (fieldReferenceOperation.Parent is ILockOperation lockOperation)
            {
                // We did lock on this field, mark as such as its now something we'd def like to convert to a Lock if possible.
                wasLockedSet.Add(fieldReference);
                return;
            }

            // it's ok to assign to the field, as long as we're assigning a new lock object to it.
            if (fieldReferenceOperation.Parent is IAssignmentOperation
                {
                    Value: IObjectCreationOperation { Arguments.Length: 0, Constructor.ContainingType.SpecialType: SpecialType.System_Object },
                } assignment &&
                assignment.Target == fieldReferenceOperation)
            {
                return;
            }

            // Add more supported patterns here as needed.

            // This wasn't a supported case.
            potentialLockFields.Remove(fieldReference);
        }, OperationKind.FieldReference);

        context.RegisterSymbolEndAction()

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
