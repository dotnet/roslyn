// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    using static PublicApiAnalyzerResources;

    /// <summary>
    /// RS0016: <inheritdoc cref="DeclarePublicApiTitle"/>
    /// RS0017: <inheritdoc cref="RemoveDeletedPublicApiTitle"/>
    /// RS0022: <inheritdoc cref="ExposedNoninstantiableTypeTitle" />
    /// RS0024: <inheritdoc cref="PublicApiFilesInvalidTitle" />
    /// RS0025: <inheritdoc cref="DuplicateSymbolsInPublicApiFilesTitle" />
    /// RS0026: <inheritdoc cref="AvoidMultipleOverloadsWithOptionalParametersTitle" />
    /// RS0027: <inheritdoc cref="OverloadWithOptionalParametersShouldHaveMostParametersTitle" />
    /// RS0036: <inheritdoc cref="AnnotatePublicApiTitle"/>
    /// RS0037: <inheritdoc cref="ShouldAnnotatePublicApiFilesTitle" />
    /// RS0041: <inheritdoc cref="ObliviousPublicApiTitle"/>
    /// RS0048: <inheritdoc cref="PublicApiFileMissingTitle" />
    /// RS0050: <inheritdoc cref="RemovedApiIsNotActuallyRemovedTitle" />
    /// RS0051: <inheritdoc cref="DeclareInternalApiTitle"/>
    /// RS0052: <inheritdoc cref="RemoveDeletedInternalApiTitle" />
    /// RS0053: <inheritdoc cref="InternalApiFilesInvalidTitle" />
    /// RS0054: <inheritdoc cref="DuplicateSymbolsInInternalApiFilesTitle" />
    /// RS0055: <inheritdoc cref="AnnotateInternalApiTitle"/>
    /// RS0056: <inheritdoc cref="ShouldAnnotateInternalApiFilesTitle" />
    /// RS0057: <inheritdoc cref="ObliviousInternalApiTitle"/>
    /// RS0058: <inheritdoc cref="InternalApiFileMissingTitle" />
    /// RS0059: <inheritdoc cref="AvoidMultipleOverloadsWithOptionalParametersTitle" />
    /// RS0060: <inheritdoc cref="OverloadWithOptionalParametersShouldHaveMostParametersTitle" />
    /// RS0061: <inheritdoc cref="ExposedNoninstantiableTypeTitle" />
    /// </summary>
    public partial class DeclarePublicApiAnalyzer
    {
        internal static readonly DiagnosticDescriptor DeclareNewPublicApiRule = new(
            id: DiagnosticIds.DeclarePublicApiRuleId,
            title: CreateLocalizableResourceString(nameof(DeclarePublicApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(DeclarePublicApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DeclarePublicApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor DeclareNewInternalApiRule = new(
            id: DiagnosticIds.DeclareInternalApiRuleId,
            title: CreateLocalizableResourceString(nameof(DeclareInternalApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(DeclareInternalApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(DeclareInternalApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor AnnotatePublicApiRule = new(
            id: DiagnosticIds.AnnotatePublicApiRuleId,
            title: CreateLocalizableResourceString(nameof(AnnotatePublicApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(AnnotatePublicApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(AnnotatePublicApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor AnnotateInternalApiRule = new(
            id: DiagnosticIds.AnnotateInternalApiRuleId,
            title: CreateLocalizableResourceString(nameof(AnnotateInternalApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(AnnotateInternalApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(AnnotateInternalApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor ObliviousPublicApiRule = new(
            id: DiagnosticIds.ObliviousPublicApiRuleId,
            title: CreateLocalizableResourceString(nameof(ObliviousPublicApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(ObliviousPublicApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(ObliviousPublicApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor ObliviousInternalApiRule = new(
            id: DiagnosticIds.ObliviousInternalApiRuleId,
            title: CreateLocalizableResourceString(nameof(ObliviousInternalApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(ObliviousInternalApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(ObliviousInternalApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveDeletedPublicApiRule = new(
            id: DiagnosticIds.RemoveDeletedPublicApiRuleId,
            title: CreateLocalizableResourceString(nameof(RemoveDeletedPublicApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(RemoveDeletedPublicApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(RemoveDeletedPublicApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor RemoveDeletedInternalApiRule = new(
            id: DiagnosticIds.RemoveDeletedInternalApiRuleId,
            title: CreateLocalizableResourceString(nameof(RemoveDeletedInternalApiTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(RemoveDeletedInternalApiMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(RemoveDeletedInternalApiDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor RemovedApiIsNotActuallyRemovedRule = new(
           id: DiagnosticIds.RemovedApiIsNotActuallyRemovedRuleId,
           title: CreateLocalizableResourceString(nameof(RemovedApiIsNotActuallyRemovedTitle)),
           messageFormat: CreateLocalizableResourceString(nameof(RemovedApiIsNotActuallyRemovedMessage)),
           category: "ApiDesign",
           defaultSeverity: DiagnosticSeverity.Warning,
           isEnabledByDefault: true,
           helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
           customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor ExposedNoninstantiableTypePublic = new(
            id: DiagnosticIds.ExposedNoninstantiableTypeRuleIdPublic,
            title: CreateLocalizableResourceString(nameof(ExposedNoninstantiableTypeTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(ExposedNoninstantiableTypeMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor ExposedNoninstantiableTypeInternal = new(
            id: DiagnosticIds.ExposedNoninstantiableTypeRuleIdInternal,
            title: CreateLocalizableResourceString(nameof(ExposedNoninstantiableTypeTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(ExposedNoninstantiableTypeMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor PublicApiFilesInvalid = new(
            id: DiagnosticIds.PublicApiFilesInvalid,
            title: CreateLocalizableResourceString(nameof(PublicApiFilesInvalidTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(PublicApiFilesInvalidMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor InternalApiFilesInvalid = new(
            id: DiagnosticIds.InternalApiFilesInvalid,
            title: CreateLocalizableResourceString(nameof(InternalApiFilesInvalidTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(InternalApiFilesInvalidMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor PublicApiFileMissing = new(
            id: DiagnosticIds.PublicApiFileMissing,
            title: CreateLocalizableResourceString(nameof(PublicApiFileMissingTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(PublicApiFileMissingMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor InternalApiFileMissing = new(
            id: DiagnosticIds.InternalApiFileMissing,
            title: CreateLocalizableResourceString(nameof(InternalApiFileMissingTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(InternalApiFileMissingMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor DuplicateSymbolInPublicApiFiles = new(
            id: DiagnosticIds.DuplicatedSymbolInPublicApiFiles,
            title: CreateLocalizableResourceString(nameof(DuplicateSymbolsInPublicApiFilesTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(DuplicateSymbolsInPublicApiFilesMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor DuplicateSymbolInInternalApiFiles = new(
            id: DiagnosticIds.DuplicatedSymbolInInternalApiFiles,
            title: CreateLocalizableResourceString(nameof(DuplicateSymbolsInInternalApiFilesTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(DuplicateSymbolsInInternalApiFilesMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor AvoidMultipleOverloadsWithOptionalParametersPublic = new(
            id: DiagnosticIds.AvoidMultipleOverloadsWithOptionalParametersPublic,
            title: CreateLocalizableResourceString(nameof(AvoidMultipleOverloadsWithOptionalParametersTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(AvoidMultipleOverloadsWithOptionalParametersMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor AvoidMultipleOverloadsWithOptionalParametersInternal = new(
            id: DiagnosticIds.AvoidMultipleOverloadsWithOptionalParametersInternal,
            title: CreateLocalizableResourceString(nameof(AvoidMultipleOverloadsWithOptionalParametersTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(AvoidMultipleOverloadsWithOptionalParametersMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor OverloadWithOptionalParametersShouldHaveMostParametersPublic = new(
            id: DiagnosticIds.OverloadWithOptionalParametersShouldHaveMostParametersPublic,
            title: CreateLocalizableResourceString(nameof(OverloadWithOptionalParametersShouldHaveMostParametersTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(OverloadWithOptionalParametersShouldHaveMostParametersMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor OverloadWithOptionalParametersShouldHaveMostParametersInternal = new(
            id: DiagnosticIds.OverloadWithOptionalParametersShouldHaveMostParametersInternal,
            title: CreateLocalizableResourceString(nameof(OverloadWithOptionalParametersShouldHaveMostParametersTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(OverloadWithOptionalParametersShouldHaveMostParametersMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor ShouldAnnotatePublicApiFilesRule = new(
            id: DiagnosticIds.ShouldAnnotatePublicApiFilesRuleId,
            title: CreateLocalizableResourceString(nameof(ShouldAnnotatePublicApiFilesTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(ShouldAnnotatePublicApiFilesMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(ShouldAnnotatePublicApiFilesDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor ShouldAnnotateInternalApiFilesRule = new(
            id: DiagnosticIds.ShouldAnnotateInternalApiFilesRuleId,
            title: CreateLocalizableResourceString(nameof(ShouldAnnotateInternalApiFilesTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(ShouldAnnotateInternalApiFilesMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(ShouldAnnotateInternalApiFilesDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                DeclareNewPublicApiRule,
                DeclareNewInternalApiRule,
                AnnotatePublicApiRule,
                AnnotateInternalApiRule,
                ObliviousPublicApiRule,
                ObliviousInternalApiRule,
                RemoveDeletedPublicApiRule,
                RemoveDeletedInternalApiRule,
                ExposedNoninstantiableTypePublic,
                ExposedNoninstantiableTypeInternal,
                PublicApiFilesInvalid,
                InternalApiFilesInvalid,
                PublicApiFileMissing,
                InternalApiFileMissing,
                DuplicateSymbolInPublicApiFiles,
                DuplicateSymbolInInternalApiFiles,
                AvoidMultipleOverloadsWithOptionalParametersPublic,
                AvoidMultipleOverloadsWithOptionalParametersInternal,
                OverloadWithOptionalParametersShouldHaveMostParametersPublic,
                OverloadWithOptionalParametersShouldHaveMostParametersInternal,
                ShouldAnnotatePublicApiFilesRule,
                ShouldAnnotateInternalApiFilesRule,
                RemovedApiIsNotActuallyRemovedRule);
    }
}
