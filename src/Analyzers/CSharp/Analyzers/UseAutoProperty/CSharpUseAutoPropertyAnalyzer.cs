// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseAutoProperty;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseAutoPropertyAnalyzer : AbstractUseAutoPropertyAnalyzer<
    SyntaxKind,
    PropertyDeclarationSyntax,
    ConstructorDeclarationSyntax,
    FieldDeclarationSyntax,
    VariableDeclaratorSyntax,
    ExpressionSyntax,
    IdentifierNameSyntax>
{
    protected override SyntaxKind PropertyDeclarationKind
        => SyntaxKind.PropertyDeclaration;

    protected override ISemanticFacts SemanticFacts
        => CSharpSemanticFacts.Instance;

    protected override bool SupportsReadOnlyProperties(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp6;

    protected override bool SupportsPropertyInitializer(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp6;

    protected override bool SupportsSemiAutoProperty(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp13;

    protected override bool CanExplicitInterfaceImplementationsBeFixed()
        => false;

    protected override ExpressionSyntax? GetFieldInitializer(VariableDeclaratorSyntax variable, CancellationToken cancellationToken)
        => variable.Initializer?.Value;

    protected override void RegisterIneligibleFieldsAction(
        HashSet<string> fieldNames,
        ConcurrentSet<IFieldSymbol> ineligibleFields,
        SemanticModel semanticModel,
        SyntaxNode codeBlock,
        CancellationToken cancellationToken)
    {
        foreach (var argument in codeBlock.DescendantNodesAndSelf().OfType<ArgumentSyntax>())
        {
            // An argument will disqualify a field if that field is used in a ref/out position.  
            // We can't change such field references to be property references in C#.
            if (argument.RefKindKeyword.Kind() != SyntaxKind.None)
                AddIneligibleFieldsForExpression(argument.Expression);
        }

        foreach (var refExpression in codeBlock.DescendantNodesAndSelf().OfType<RefExpressionSyntax>())
            AddIneligibleFieldsForExpression(refExpression.Expression);

        // Can't take the address of an auto-prop.  So disallow for fields that we do `&x` on.
        foreach (var addressOfExpression in codeBlock.DescendantNodesAndSelf().OfType<PrefixUnaryExpressionSyntax>())
        {
            if (addressOfExpression.Kind() == SyntaxKind.AddressOfExpression)
                AddIneligibleFieldsForExpression(addressOfExpression.Operand);
        }

        foreach (var memberAccess in codeBlock.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
        {
            if (CouldReferenceField(memberAccess))
                AddIneligibleFieldsIfAccessedOffNotDefinitelyAssignedValue(semanticModel, memberAccess, ineligibleFields, cancellationToken);
        }

        bool CouldReferenceField(ExpressionSyntax expression)
        {
            // Don't bother binding if the expression isn't even referencing the name of a field we know about.
            var rightmostName = expression.GetRightmostName()?.Identifier.ValueText;
            return rightmostName != null && fieldNames.Contains(rightmostName);
        }

        void AddIneligibleFieldsForExpression(ExpressionSyntax expression)
        {
            if (!CouldReferenceField(expression))
                return;

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            AddIneligibleFields(ineligibleFields, symbolInfo);
        }
    }

    private static void AddIneligibleFieldsIfAccessedOffNotDefinitelyAssignedValue(
        SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccess, ConcurrentSet<IFieldSymbol> ineligibleFields, CancellationToken cancellationToken)
    {
        // `c.x = ...` can't be converted to `c.X = ...` if `c` is a struct and isn't definitely assigned as that point.

        // only care about writes.  if this was a read, then it must be def assigned and thus is safe to convert to a prop.
        if (!memberAccess.IsOnlyWrittenTo())
            return;

        // this only matters for a field access off of a struct.  They can be declared unassigned and have their
        // fields directly written into.
        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
        if (symbolInfo.GetAnySymbol() is not IFieldSymbol { ContainingType.TypeKind: TypeKind.Struct })
            return;

        var exprSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).GetAnySymbol();
        if (exprSymbol is not IParameterSymbol and not ILocalSymbol)
            return;

        var dataFlow = semanticModel.AnalyzeDataFlow(memberAccess.Expression);
        if (dataFlow != null && !dataFlow.DefinitelyAssignedOnEntry.Contains(exprSymbol))
            AddIneligibleFields(ineligibleFields, symbolInfo);
    }

    private static void AddIneligibleFields(ConcurrentSet<IFieldSymbol> ineligibleFields, SymbolInfo symbolInfo)
    {
        AddIneligibleField(symbolInfo.Symbol);
        foreach (var symbol in symbolInfo.CandidateSymbols)
            AddIneligibleField(symbol);

        void AddIneligibleField(ISymbol? symbol)
        {
            if (symbol is IFieldSymbol field)
                ineligibleFields.Add(field);
        }
    }

    private static bool CheckExpressionSyntactically(ExpressionSyntax expression)
    {
        if (expression is MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression)
            {
                Expression: (kind: SyntaxKind.ThisExpression),
                Name: (kind: SyntaxKind.IdentifierName),
            })
        {
            return true;
        }
        else if (expression.IsKind(SyntaxKind.IdentifierName))
        {
            return true;
        }

        return false;
    }

    protected override ExpressionSyntax? GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken)
    {
        // Getter has to be of the form:
        // 1. Getter can be defined as accessor or expression bodied lambda
        //     get { return field; }
        //     get => field;
        //     int Property => field;
        // 2. Underlying field can be accessed with this qualifier or not
        //     get { return field; }
        //     get { return this.field; }
        var expr = GetGetterExpressionFromSymbol(getMethod, cancellationToken);
        if (expr == null)
            return null;

        return CheckExpressionSyntactically(expr) ? expr : null;
    }

    private static ExpressionSyntax? GetGetterExpressionFromSymbol(IMethodSymbol getMethod, CancellationToken cancellationToken)
    {
        var declaration = getMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        return declaration switch
        {
            AccessorDeclarationSyntax accessorDeclaration =>
                accessorDeclaration.ExpressionBody?.Expression ?? GetSingleStatementFromAccessor<ReturnStatementSyntax>(accessorDeclaration)?.Expression,
            ArrowExpressionClauseSyntax arrowExpression => arrowExpression.Expression,
            null => null,
            _ => throw ExceptionUtilities.Unreachable(),
        };
    }

    private static T? GetSingleStatementFromAccessor<T>(AccessorDeclarationSyntax? accessorDeclaration) where T : StatementSyntax
        => accessorDeclaration is { Body.Statements: [T statement] } ? statement : null;

    protected override ExpressionSyntax? GetSetterExpression(
         SemanticModel semanticModel, IMethodSymbol setMethod, CancellationToken cancellationToken)
    {
        // Setter has to be of the form:
        //
        //     set { field = value; }
        //     set { this.field = value; }
        //     set => field = value; 
        //     set => this.field = value; 
        var setAccessor = setMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) as AccessorDeclarationSyntax;
        var setExpression = GetExpressionFromSetter(setAccessor);
        if (setExpression is AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression)
            {
                Right: IdentifierNameSyntax { Identifier.ValueText: "value" }
            } assignmentExpression)
        {
            return CheckExpressionSyntactically(assignmentExpression.Left) ? assignmentExpression.Left : null;
        }

        return null;
    }

    private static ExpressionSyntax? GetExpressionFromSetter(AccessorDeclarationSyntax? setAccessor)
        => setAccessor?.ExpressionBody?.Expression ??
           GetSingleStatementFromAccessor<ExpressionStatementSyntax>(setAccessor)?.Expression;

    protected override SyntaxNode GetFieldNode(
        FieldDeclarationSyntax fieldDeclaration, VariableDeclaratorSyntax variableDeclarator)
    {
        return fieldDeclaration.Declaration.Variables.Count == 1
            ? fieldDeclaration
            : variableDeclarator;
    }

    protected override void AddAccessedFields(SemanticModel semanticModel, IMethodSymbol accessor, HashSet<IFieldSymbol> result, CancellationToken cancellationToken)
    {
    }
}
