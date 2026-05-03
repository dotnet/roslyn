// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PopulateSwitch;

internal abstract class AbstractPopulateSwitchDiagnosticAnalyzer<TSwitchOperation, TSwitchSyntax>(
    string diagnosticId,
    EnforceOnBuild enforceOnBuild)
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(diagnosticId,
        enforceOnBuild,
        option: null,
        s_localizableTitle, s_localizableMessage)
    where TSwitchOperation : IOperation
    where TSwitchSyntax : SyntaxNode
{
    private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(AnalyzersResources.Add_missing_cases), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(AnalyzersResources.Populate_switch), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    protected abstract OperationKind OperationKind { get; }

    protected abstract bool IsSwitchTypeUnknown(TSwitchOperation operation);
    protected abstract IOperation GetValueOfSwitchOperation(TSwitchOperation operation);

    protected abstract bool IsKnownToBeExhaustive(TSwitchOperation switchOperation);

    protected abstract bool HasConstantCase(TSwitchOperation operation, object? value);
    protected abstract ICollection<ISymbol> GetMissingEnumMembers(TSwitchOperation operation);
    protected abstract bool HasDefaultCase(TSwitchOperation operation);
    protected abstract bool HasExhaustiveNullAndTypeCheckCases(TSwitchOperation operation);
    protected abstract Location GetDiagnosticLocation(TSwitchSyntax switchBlock);

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

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

        // We treat enum switches specially.  Specifically, even if exhaustive (because of a 'default' case),
        // we still want to let users use the feature to fill in missing enum members.  That way if they add
        // new enum members, they can quickly find and fix the switches that aren't explicitly handling those cases.
        //
        // Note: this should likely be a refactoring instead of an analyzer.  However, for historical reasons,
        // we shipped in this fashion.
        if (typeWithoutNullable.TypeKind == TypeKind.Enum)
            return AnalyzeEnumSwitch(switchOperation, type);

        // For all other types, we don't want to offer the user anything if the switch is already exhaustive.
        if (this.IsKnownToBeExhaustive(switchOperation))
            return default;

        if (typeWithoutNullable.SpecialType == SpecialType.System_Boolean)
            return AnalyzeBooleanSwitch(switchOperation, type);

        return (missingCases: false, missingDefaultCase: !HasDefaultCase(switchOperation));
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

        return (missingCases: false, missingDefaultCase: true);
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
