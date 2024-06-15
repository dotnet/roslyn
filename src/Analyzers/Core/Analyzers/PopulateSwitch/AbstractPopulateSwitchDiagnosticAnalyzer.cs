// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PopulateSwitch;

internal abstract class AbstractPopulateSwitchDiagnosticAnalyzer<TSwitchOperation, TSwitchSyntax> :
    AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSwitchOperation : IOperation
    where TSwitchSyntax : SyntaxNode
{
    private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(AnalyzersResources.Add_missing_cases), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(AnalyzersResources.Populate_switch), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    protected AbstractPopulateSwitchDiagnosticAnalyzer(string diagnosticId, EnforceOnBuild enforceOnBuild)
        : base(diagnosticId,
               enforceOnBuild,
               option: null,
               s_localizableTitle, s_localizableMessage)
    {
    }

    protected abstract OperationKind OperationKind { get; }

    protected abstract bool IsSwitchTypeUnknown(TSwitchOperation operation);
    protected abstract IOperation GetValueOfSwitchOperation(TSwitchOperation operation);

    protected abstract bool HasConstantCase(TSwitchOperation operation, object? value);
    protected abstract ICollection<ISymbol> GetMissingEnumMembers(TSwitchOperation operation);
    protected abstract bool HasDefaultCase(TSwitchOperation operation);
    protected abstract bool HasExhaustiveNullAndTypeCheckCases(TSwitchOperation operation);
    protected abstract Location GetDiagnosticLocation(TSwitchSyntax switchBlock);

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterOperationAction(AnalyzeOperation, OperationKind);

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        var switchOperation = (TSwitchOperation)context.Operation;
        if (switchOperation.Syntax is not TSwitchSyntax switchBlock || IsSwitchTypeUnknown(switchOperation))
            return;

        if (HasExhaustiveNullAndTypeCheckCases(switchOperation))
            return;

        var value = GetValueOfSwitchOperation(switchOperation);
        var type = value.Type;
        if (type is null)
            return;

        var (missingCases, missingDefaultCase) = AnalyzeSwitch(switchOperation, type);
        if (!missingCases && !missingDefaultCase)
            return;

        if (switchBlock.SyntaxTree.OverlapsHiddenPosition(switchBlock.Span, context.CancellationToken))
            return;

        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(PopulateSwitchStatementHelpers.MissingCases, missingCases.ToString())
            .Add(PopulateSwitchStatementHelpers.MissingDefaultCase, missingDefaultCase.ToString());
        var diagnostic = Diagnostic.Create(
            Descriptor,
            GetDiagnosticLocation(switchBlock),
            properties: properties,
            additionalLocations: [switchBlock.GetLocation()]);
        context.ReportDiagnostic(diagnostic);
    }

    private (bool missingCases, bool missingDefaultCase) AnalyzeSwitch(TSwitchOperation switchOperation, ITypeSymbol type)
    {
        var typeWithoutNullable = type.RemoveNullableIfPresent();

        if (typeWithoutNullable.SpecialType == SpecialType.System_Boolean)
        {
            return AnalyzeBooleanSwitch(switchOperation, type);
        }
        else if (typeWithoutNullable.TypeKind == TypeKind.Enum)
        {
            return AnalyzeEnumSwitch(switchOperation, type);
        }
        else
        {
            return (missingCases: false, missingDefaultCase: !HasDefaultCase(switchOperation));
        }
    }

    private (bool missingCases, bool missingDefaultCase) AnalyzeBooleanSwitch(TSwitchOperation operation, ITypeSymbol type)
    {
        if (type.RemoveNullableIfPresent() is not { SpecialType: SpecialType.System_Boolean })
            return default;

        // Doesn't have a default.  We don't want to offer that if they're already complete.
        var hasAllCases = HasConstantCase(operation, true) && HasConstantCase(operation, false);
        if (type.IsNullable())
            hasAllCases = hasAllCases && HasConstantCase(operation, null);

        // If the switch already has a default case or already has all cases, then we don't have to offer the user anything.
        if (HasDefaultCase(operation) || hasAllCases)
            return default;

        return (missingCases: true, missingDefaultCase: true);
    }

    private (bool missingCases, bool missingDefaultCase) AnalyzeEnumSwitch(TSwitchOperation operation, ITypeSymbol type)
    {
        if (type.RemoveNullableIfPresent()?.TypeKind != TypeKind.Enum)
            return default;

        var missingEnumMembers = GetMissingEnumMembers(operation);

        return (missingCases: missingEnumMembers.Count > 0, missingDefaultCase: !HasDefaultCase(operation));
    }

    protected static bool ConstantValueEquals(Optional<object?> constantValue, object? value)
        => constantValue.HasValue && Equals(constantValue.Value, value);
}
