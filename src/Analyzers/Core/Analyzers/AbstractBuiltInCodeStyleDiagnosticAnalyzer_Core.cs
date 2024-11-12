// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal abstract partial class AbstractBuiltInCodeStyleDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
{
    protected readonly DiagnosticDescriptor Descriptor;
    private DiagnosticSeverity? _minimumReportedSeverity;

    private AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        string descriptorId,
        EnforceOnBuild enforceOnBuild,
        LocalizableString title,
        LocalizableString? messageFormat,
        bool isUnnecessary,
        bool configurable,
        bool hasAnyCodeStyleOption)
    {
        // 'isUnnecessary' should be true only for sub-types of AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer.
        Debug.Assert(!isUnnecessary || this is AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer);

        Descriptor = CreateDescriptorWithId(descriptorId, enforceOnBuild, hasAnyCodeStyleOption, title, messageFormat ?? title, isUnnecessary: isUnnecessary, isConfigurable: configurable);
        SupportedDiagnostics = [Descriptor];
    }

    /// <summary>
    /// Constructor for a code style analyzer with a multiple diagnostic descriptors such that all the descriptors have no unique code style option to configure the descriptors.
    /// </summary>
    protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> supportedDiagnostics)
    {
        SupportedDiagnostics = supportedDiagnostics;

        Descriptor = SupportedDiagnostics[0];
        Debug.Assert(!supportedDiagnostics.Any(descriptor => descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Unnecessary)) || this is AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer);
    }

    public virtual bool IsHighPriority => false;
    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    protected static DiagnosticDescriptor CreateDescriptorWithId(
        string id,
        EnforceOnBuild enforceOnBuild,
        bool hasAnyCodeStyleOption,
        LocalizableString title,
        LocalizableString? messageFormat = null,
        bool isUnnecessary = false,
        bool isConfigurable = true,
        LocalizableString? description = null)
#pragma warning disable RS0030 // Do not used banned APIs
        => new(
                id, title, messageFormat ?? title,
                DiagnosticCategory.Style,
                DiagnosticSeverity.Hidden,
                isEnabledByDefault: true,
                description: description,
                helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(id),
                customTags: DiagnosticCustomTags.Create(isUnnecessary, isConfigurable, isCustomConfigurable: hasAnyCodeStyleOption, enforceOnBuild));
#pragma warning restore RS0030 // Do not used banned APIs

    /// <summary>
    /// Flags to configure the analysis of generated code.
    /// By default, code style analyzers should not analyze or report diagnostics on generated code, so the value is false.
    /// </summary>
    protected virtual GeneratedCodeAnalysisFlags GeneratedCodeAnalysisFlags => GeneratedCodeAnalysisFlags.None;

    public sealed override void Initialize(AnalysisContext context)
    {
        _minimumReportedSeverity = context.MinimumReportedSeverity;

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags);
        context.EnableConcurrentExecution();

        InitializeWorker(context);
    }

    protected abstract void InitializeWorker(AnalysisContext context);

    protected static bool IsAnalysisLevelGreaterThanOrEquals(int minAnalysisLevel, AnalyzerOptions analyzerOptions)
        => analyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions.IsAnalysisLevelGreaterThanOrEquals(minAnalysisLevel);

    protected bool ShouldSkipAnalysis(SemanticModelAnalysisContext context, NotificationOption2? notification)
        => ShouldSkipAnalysis(context.FilterTree, context.Options, context.SemanticModel.Compilation.Options, notification, context.CancellationToken);

    protected bool ShouldSkipAnalysis(SyntaxNodeAnalysisContext context, NotificationOption2? notification)
        => ShouldSkipAnalysis(context.Node.SyntaxTree, context.Options, context.Compilation.Options, notification, context.CancellationToken);

    protected bool ShouldSkipAnalysis(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions, NotificationOption2? notification)
        => ShouldSkipAnalysis(context.Tree, context.Options, compilationOptions, notification, context.CancellationToken);

    protected bool ShouldSkipAnalysis(CodeBlockAnalysisContext context, NotificationOption2? notification)
        => ShouldSkipAnalysis(context.FilterTree, context.Options, context.SemanticModel.Compilation.Options, notification, context.CancellationToken);

    protected bool ShouldSkipAnalysis(OperationAnalysisContext context, NotificationOption2? notification)
        => ShouldSkipAnalysis(context.FilterTree, context.Options, context.Compilation.Options, notification, context.CancellationToken);

    protected bool ShouldSkipAnalysis(OperationBlockAnalysisContext context, NotificationOption2? notification)
        => ShouldSkipAnalysis(context.FilterTree, context.Options, context.Compilation.Options, notification, context.CancellationToken);

    protected bool ShouldSkipAnalysis(
        SyntaxTree tree,
        AnalyzerOptions analyzerOptions,
        CompilationOptions compilationOptions,
        NotificationOption2? notification,
        CancellationToken cancellationToken)
        => ShouldSkipAnalysis(tree, analyzerOptions, compilationOptions, notification, performDescriptorsCheck: true, cancellationToken);

    protected bool ShouldSkipAnalysis(
        SyntaxTree tree,
        AnalyzerOptions analyzerOptions,
        CompilationOptions compilationOptions,
        ImmutableArray<NotificationOption2> notifications,
        CancellationToken cancellationToken)
    {
        // We need to check if the analyzer's severity has been escalated either via 'option_name = option_value:severity'
        // setting or 'dotnet_diagnostic.RuleId.severity = severity'.
        // For the former, we check if any of the given notifications have been escalated via the ':severity' such
        // that analysis cannot be skipped. For the latter, we perform descriptor-based checks.
        // Descriptors check verifies if any of the diagnostic IDs reported by this analyzer
        // have been escalated to a severity that they must be executed.

        // PERF: Execute the descriptors check only once for the analyzer, not once per each notification option.
        var performDescriptorsCheck = true;

        // Check if any of the notifications are enabled, if so we need to execute analysis.
        foreach (var notification in notifications)
        {
            if (!ShouldSkipAnalysis(tree, analyzerOptions, compilationOptions, notification, performDescriptorsCheck, cancellationToken))
                return false;

            if (performDescriptorsCheck)
                performDescriptorsCheck = false;
        }

        return true;
    }

    private bool ShouldSkipAnalysis(
        SyntaxTree tree,
        AnalyzerOptions analyzerOptions,
        CompilationOptions compilationOptions,
        NotificationOption2? notification,
        bool performDescriptorsCheck,
        CancellationToken cancellationToken)
    {
        // We need to check if the analyzer's severity has been escalated either via 'option_name = option_value:severity'
        // setting or 'dotnet_diagnostic.RuleId.severity = severity'.
        // For the former, we check if the given notification have been escalated via the ':severity' such
        // that analysis cannot be skipped. For the latter, we perform descriptor-based checks.
        // Descriptors check verifies if any of the diagnostic IDs reported by this analyzer
        // have been escalated to a severity that they must be executed.

        Debug.Assert(_minimumReportedSeverity != null);

        if (notification?.Severity == ReportDiagnostic.Suppress)
            return true;

        // If _minimumReportedSeverity is 'Hidden', then we are reporting diagnostics with all severities.
        if (_minimumReportedSeverity!.Value == DiagnosticSeverity.Hidden)
            return false;

        // If the severity is explicitly configured with `option_name = option_value:severity`,
        // we should skip analysis if the configured severity is lesser than the minimum reported severity.
        // Additionally, notification based severity configuration is respected on build only for AnalysisLevel >= 9.
        if (notification.HasValue
            && notification.Value.IsExplicitlySpecified
            && IsAnalysisLevelGreaterThanOrEquals(9, analyzerOptions))
        {
            return notification.Value.Severity.ToDiagnosticSeverity() < _minimumReportedSeverity.Value;
        }

        if (!performDescriptorsCheck)
            return true;

        // Otherwise, we check if any of the descriptors have been configured or bulk-configured
        // in editorconfig/globalconfig options to a severity that is greater than or equal to
        // the minimum reported severity.
        // If so, we should execute analysis. Otherwise, analysis should be skipped.
        // See https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-options#scope
        // for precedence rules for configuring severity of a single rule ID, a category of rule IDs
        // or all analyzer rule IDs.

        var severityOptionsProvider = compilationOptions.SyntaxTreeOptionsProvider!;
        var globalOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;
        var treeOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);

        // See https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-options#scope
        // for supported analyzer bulk configuration formats.
        const string DotnetAnalyzerDiagnosticPrefix = "dotnet_analyzer_diagnostic";
        const string CategoryPrefix = "category";
        const string SeveritySuffix = "severity";

        var allDiagnosticsBulkSeverityKey = $"{DotnetAnalyzerDiagnosticPrefix}.{SeveritySuffix}";
        var hasAllBulkSeverityConfiguration = treeOptions.TryGetValue(allDiagnosticsBulkSeverityKey, out var editorConfigBulkSeverity)
            || globalOptions.TryGetValue(allDiagnosticsBulkSeverityKey, out editorConfigBulkSeverity);

        foreach (var descriptor in SupportedDiagnostics)
        {
            if (descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                continue;

            // First check if the diagnostic ID has been explicitly configured with `dotnet_diagnostic` entry.
            if (severityOptionsProvider.TryGetDiagnosticValue(tree, descriptor.Id, cancellationToken, out var configuredReportDiagnostic)
                || severityOptionsProvider.TryGetGlobalDiagnosticValue(descriptor.Id, cancellationToken, out configuredReportDiagnostic))
            {
                if (configuredReportDiagnostic.ToDiagnosticSeverity() is { } configuredSeverity
                    && configuredSeverity >= _minimumReportedSeverity.Value)
                {
                    return false;
                }

                continue;
            }

            // Next, check if the descriptor's category has been bulk configured with `dotnet_analyzer_diagnostic.category-Category.severity` entry.
            // or severity of all analyzer diagnostics has been bulk configured with `dotnet_analyzer_diagnostic.severity` entry.
            var categoryConfigurationKey = $"{DotnetAnalyzerDiagnosticPrefix}.{CategoryPrefix}-{descriptor.Category}.{SeveritySuffix}";
            if (treeOptions.TryGetValue(categoryConfigurationKey, out var editorConfigSeverity)
                || globalOptions.TryGetValue(categoryConfigurationKey, out editorConfigSeverity))
            {
            }
            else if (hasAllBulkSeverityConfiguration)
            {
                editorConfigSeverity = editorConfigBulkSeverity;
            }
            else
            {
                // No diagnostic ID or bulk configuration for the descriptor.
                // Check if the descriptor's default severity is greater than or equals the minimum reported severiity.
                if (descriptor.IsEnabledByDefault && descriptor.DefaultSeverity >= _minimumReportedSeverity.Value)
                    return false;

                // Otherwise, we can skip this descriptor as it cannot contribute a diagnostic that will be reported.
                continue;
            }

            Debug.Assert(editorConfigSeverity != null);
            if (EditorConfigSeverityStrings.TryParse(editorConfigSeverity!, out var effectiveReportDiagnostic)
                && effectiveReportDiagnostic.ToDiagnosticSeverity() is { } effectiveSeverity
                && effectiveSeverity >= _minimumReportedSeverity.Value)
            {
                return false;
            }
        }

        return true;
    }
}
