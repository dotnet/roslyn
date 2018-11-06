// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
{
    // Map from different combinations of diagnostic properties to a properties map that gets added to each diagnostic instance.
    using PropertiesMap = ImmutableDictionary<(UnusedValuePreference preference, bool isUnusedLocalAssignment, bool isRemovableAssignment),
                                              ImmutableDictionary<string, string>>;

    /// <summary>
    /// Analyzer to report unused expression values and parameters:
    /// It flags the following cases:
    ///     1. Expression statements that drop computed value, for example, "Computation();".
    ///        These should either be removed (redundant computation) or should be replaced
    ///        with explicit assignment to discard variable OR an unused local variable,
    ///        i.e. "_ = Computation();" or "var unused = Computation();"
    ///        This diagnostic configuration is controlled by language specific code style option "UnusedValueExpressionStatement".
    ///     2. Value assignments to locals/parameters that are never used on any control flow path,
    ///        For example, value assigned to 'x' in first statement below is unused and will be flagged:
    ///             x = Computation();
    ///             if (...)
    ///                 x = Computation2();
    ///             else
    ///                 Computation3(out x);
    ///             ... = x;
    ///        Just as for case 1., these should either be removed (redundant computation) or
    ///        should be replaced with explicit assignment to discard variable OR an unused local variable,
    ///        i.e. "_ = Computation();" or "var unused = Computation();"
    ///        This diagnostic configuration is controlled by language specific code style option "UnusedValueAssignment".
    ///     3. Redundant parameters that fall into one of the following two categories:
    ///         a. Have no references in the executable code block(s) for it's containing method symbol.
    ///         b. Have one or more references but it's initial value at start of code block is never used.
    ///            For example, if 'x' in the example for case 2. above was a parameter symbol with RefKind.None
    ///            and "x = Computation();" is the first statement in the method body, then it's initial value
    ///            is never used. Such a parameter should be removed and 'x' should be converted into a local.
    ///        We provide additional information in the diagnostic message to clarify the above two categories
    ///        and also detect and mention about potential breaking change if the containing method is a public API.
    ///        Currently, we do not provide any code fix for removing unused parameters as it needs fixing the
    ///        call sites and any automated fix can lead to subtle overload resolution differences,
    ///        though this may change in future.
    ///        This diagnostic configuration is controlled by <see cref="CodeStyleOptions.UnusedParameters"/> option.
    /// </summary>
    internal abstract partial class AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string DiscardVariableName = "_";

        private const string UnusedValuePreferenceKey = nameof(UnusedValuePreferenceKey);
        private const string IsUnusedLocalAssignmentKey = nameof(IsUnusedLocalAssignmentKey);
        private const string IsRemovableAssignmentKey = nameof(IsRemovableAssignmentKey);

        // Diagnostic reported for expression statements that drop computed value, for example, "Computation();".
        // This is **not** an unneccessary (fading) diagnostic as the expression being flagged is not unncessary, but the dropped value is.
        private static readonly DiagnosticDescriptor s_expressionValueIsUnusedRule = CreateDescriptorWithId(
            IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Expression_value_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Expression_value_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: false);

        // Diagnostic reported for value assignments to locals/parameters that are never used on any control flow path.
        private static readonly DiagnosticDescriptor s_valueAssignedIsUnusedRule = CreateDescriptorWithId(
            IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Value_assigned_to_symbol_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Value_assigned_to_0_is_never_used), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        // Diagnostic reported for unneccessary parameters that can be removed.
        private static readonly DiagnosticDescriptor s_unusedParameterRule = CreateDescriptorWithId(
            IDEDiagnosticIds.UnusedParameterDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Remove_unused_parameter), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Remove_unused_parameter_0_1), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        private static readonly PropertiesMap s_propertiesMap = CreatePropertiesMap();

        protected AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer()
            : base(ImmutableArray.Create(s_expressionValueIsUnusedRule, s_valueAssignedIsUnusedRule, s_unusedParameterRule))
        {
        }

        protected abstract Location GetDefinitionLocationToFade(IOperation unusedDefinition);
        protected abstract bool SupportsDiscard(SyntaxTree tree);
        protected abstract Option<CodeStyleOption<UnusedValuePreference>> UnusedValueExpressionStatementOption { get; }
        protected abstract Option<CodeStyleOption<UnusedValuePreference>> UnusedValueAssignmentOption { get; }

        /// <summary>
        /// Method to compute well-known diagnostic property maps for different comnbinations of diagnostic properties.
        /// The property map is added to each instance of the reported diagnostic and is used by the code fixer to
        /// compute the correct code fix.
        /// It currently maps to three different properties of the diagnostic:
        ///     1. The underlying <see cref="UnusedValuePreference"/> for the reported diagnostic
        ///     2. "isUnusedLocalAssignment": Flag indicating if the flagged local variable has no reads/uses.
        ///     3. "isRemovableAssignment": Flag indicating if the assigned value is from an expression that has no side effects
        ///             and hence can be removed completely. For example, if the assigned value is a constant or a reference
        ///             to a local/parameter, then it has no side effects, but if it is method invocation, it may have side effects.
        /// </summary>
        /// <returns></returns>
        private static PropertiesMap CreatePropertiesMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<(UnusedValuePreference preference, bool isUnusedLocalAssignment, bool isRemovableAssignment),
                                                            ImmutableDictionary<string, string>>();
            AddEntries(UnusedValuePreference.DiscardVariable);
            AddEntries(UnusedValuePreference.UnusedLocalVariable);
            return builder.ToImmutable();

            void AddEntries(UnusedValuePreference preference)
            {
                AddEntries2(preference, isUnusedLocalAssignment: true);
                AddEntries2(preference, isUnusedLocalAssignment: false);
            }

            void AddEntries2(UnusedValuePreference preference, bool isUnusedLocalAssignment)
            {
                AddEntryCore(preference, isUnusedLocalAssignment, isRemovableAssignment: true);
                AddEntryCore(preference, isUnusedLocalAssignment, isRemovableAssignment: false);
            }

            void AddEntryCore(UnusedValuePreference preference, bool isUnusedLocalAssignment, bool isRemovableAssignment)
            {
                var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string>();

                propertiesBuilder.Add(UnusedValuePreferenceKey, preference.ToString());
                if (isUnusedLocalAssignment)
                {
                    propertiesBuilder.Add(IsUnusedLocalAssignmentKey, string.Empty);
                }
                if (isRemovableAssignment)
                {
                    propertiesBuilder.Add(IsRemovableAssignmentKey, string.Empty);
                }

                builder.Add((preference, isUnusedLocalAssignment, isRemovableAssignment), propertiesBuilder.ToImmutable());
            }
        }

        public override bool OpenFileOnly(Workspace workspace) => false;

        // Our analysis is limited to unused expressions in a code block, hence is unaffected by changes outside the code block.
        // Hence, we can support incremental span based method body analysis.
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(
                compilationContext => SymbolStartAnalyzer.CreateAndRegisterActions(compilationContext, this));

        private bool TryGetOptions(
            SyntaxTree syntaxTree,
            string language,
            AnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken,
            out Options options)
        {
            options = null;
            var optionSet = analyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return false;
            }

            var unusedParametersPreference = optionSet.GetOption(CodeStyleOptions.UnusedParameters, language).Value;
            var (unusedValueExpressionStatementPreference, unusedValueExpressionStatementSeverity) = GetPreferenceAndSeverity(UnusedValueExpressionStatementOption);
            var (unusedValueAssignmentPreference, unusedValueAssignmentSeverity) = GetPreferenceAndSeverity(UnusedValueAssignmentOption);
            if (unusedParametersPreference == UnusedParametersPreference.None &&
                unusedValueExpressionStatementPreference == UnusedValuePreference.None &&
                unusedValueAssignmentPreference == UnusedValuePreference.None)
            {
                return false;
            }

            options = new Options(unusedValueExpressionStatementPreference, unusedValueExpressionStatementSeverity,
                unusedValueAssignmentPreference, unusedValueAssignmentSeverity, unusedParametersPreference);
            return true;

            // Local functions.
            (UnusedValuePreference preference, ReportDiagnostic severity) GetPreferenceAndSeverity(
                Option<CodeStyleOption<UnusedValuePreference>> codeStyleOption)
            {
                var option = optionSet.GetOption(codeStyleOption);
                var preference = option?.Value ?? UnusedValuePreference.None;
                if (preference == UnusedValuePreference.None ||
                    option.Notification.Severity == ReportDiagnostic.Suppress)
                {
                    return (UnusedValuePreference.None, ReportDiagnostic.Suppress);
                }

                // If language or language version does not support discard, fall back to prefer unused local variable.
                if (preference == UnusedValuePreference.DiscardVariable &&
                    !SupportsDiscard(syntaxTree))
                {
                    preference = UnusedValuePreference.UnusedLocalVariable;
                }

                return (preference, option.Notification.Severity);
            }
        }

        private sealed class Options
        {
            private readonly UnusedParametersPreference _unusedParametersPreference;
            public Options(
                UnusedValuePreference unusedValueExpressionStatementPreference,
                ReportDiagnostic unusedValueExpressionStatementSeverity,
                UnusedValuePreference unusedValueAssignmentPreference,
                ReportDiagnostic unusedValueAssignmentSeverity,
                UnusedParametersPreference unusedParametersPreference)
            {
                Debug.Assert(unusedValueExpressionStatementPreference != UnusedValuePreference.None ||
                             unusedValueAssignmentPreference != UnusedValuePreference.None ||
                             unusedParametersPreference != UnusedParametersPreference.None);

                UnusedValueExpressionStatementPreference = unusedValueExpressionStatementPreference;
                UnusedValueExpressionStatementSeverity = unusedValueExpressionStatementSeverity;
                UnusedValueAssignmentPreference = unusedValueAssignmentPreference;
                UnusedValueAssignmentSeverity = unusedValueAssignmentSeverity;
                _unusedParametersPreference = unusedParametersPreference;
            }

            public UnusedValuePreference UnusedValueExpressionStatementPreference { get; }
            public ReportDiagnostic UnusedValueExpressionStatementSeverity { get; }
            public UnusedValuePreference UnusedValueAssignmentPreference { get; }
            public ReportDiagnostic UnusedValueAssignmentSeverity { get; }
            public bool IsComputingUnusedParams(ISymbol symbol)
                => ShouldReportUnusedParameters(symbol, _unusedParametersPreference);
        }

        public static bool ShouldReportUnusedParameters(ISymbol symbol, UnusedParametersPreference unusedParametersPreference)
        {
            switch (unusedParametersPreference)
            {
                case UnusedParametersPreference.None:
                    return false;
                case UnusedParametersPreference.AllMethods:
                    return true;
                case UnusedParametersPreference.PrivateMethods:
                    return symbol.DeclaredAccessibility == Accessibility.Private;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public static UnusedValuePreference GetUnusedValuePreference(Diagnostic diagnostic)
        {
            if (diagnostic.Properties != null &&
                diagnostic.Properties.TryGetValue(UnusedValuePreferenceKey, out var preference))
            {
                switch (preference)
                {
                    case nameof(UnusedValuePreference.DiscardVariable):
                        return UnusedValuePreference.DiscardVariable;

                    case nameof(UnusedValuePreference.UnusedLocalVariable):
                        return UnusedValuePreference.UnusedLocalVariable;
                }
            }

            return UnusedValuePreference.None;
        }

        public static bool GetIsUnusedLocalDiagnostic(Diagnostic diagnostic)
        {
            Debug.Assert(GetUnusedValuePreference(diagnostic) != UnusedValuePreference.None);
            return diagnostic.Properties.ContainsKey(IsUnusedLocalAssignmentKey);
        }

        public static bool GetIsRemovableAssignmentDiagnostic(Diagnostic diagnostic)
        {
            Debug.Assert(GetUnusedValuePreference(diagnostic) != UnusedValuePreference.None);
            return diagnostic.Properties.ContainsKey(IsRemovableAssignmentKey);
        }
    }
}
