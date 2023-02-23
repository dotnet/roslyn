// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Options settable by integration tests.
    /// 
    /// TODO: Options are currently explicitly listed since <see cref="OptionKey2"/> is not serializable.
    /// https://github.com/dotnet/roslyn/issues/59267
    /// </summary>
    public enum WellKnownGlobalOption
    {
        CompletionOptions_ShowItemsFromUnimportedNamespaces,
        CompletionViewOptions_EnableArgumentCompletionSnippets,
        CompletionOptions_TriggerInArgumentLists,
        MetadataAsSourceOptions_NavigateToDecompiledSources,
        VisualStudioSyntaxTreeConfigurationService_EnableOpeningSourceGeneratedFilesInWorkspace,
        WorkspaceConfigurationOptions_EnableOpeningSourceGeneratedFilesInWorkspace,
        SolutionCrawlerOptions_BackgroundAnalysisScopeOption,
        SolutionCrawlerOptions_CompilerDiagnosticsScopeOption,
        InlineRenameSessionOptions_UseNewUI,
    }

    internal static class WellKnownGlobalOptions
    {
        public static IOption2 GetOption(this WellKnownGlobalOption option)
            => option switch
            {
                WellKnownGlobalOption.CompletionOptions_ShowItemsFromUnimportedNamespaces => CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
                WellKnownGlobalOption.CompletionOptions_TriggerInArgumentLists => CompletionOptionsStorage.TriggerInArgumentLists,
                WellKnownGlobalOption.CompletionViewOptions_EnableArgumentCompletionSnippets => CompletionViewOptions.EnableArgumentCompletionSnippets,
                WellKnownGlobalOption.MetadataAsSourceOptions_NavigateToDecompiledSources => MetadataAsSourceOptionsStorage.NavigateToDecompiledSources,
                WellKnownGlobalOption.WorkspaceConfigurationOptions_EnableOpeningSourceGeneratedFilesInWorkspace => WorkspaceConfigurationOptionsStorage.EnableOpeningSourceGeneratedFilesInWorkspace,
                WellKnownGlobalOption.SolutionCrawlerOptions_BackgroundAnalysisScopeOption => SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption,
                WellKnownGlobalOption.SolutionCrawlerOptions_CompilerDiagnosticsScopeOption => SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption,
                WellKnownGlobalOption.InlineRenameSessionOptions_UseNewUI => InlineRenameUIOptions.UseInlineAdornment,
                _ => throw ExceptionUtilities.Unreachable()
            };

        public static OptionKey2 GetKey(this WellKnownGlobalOption option, string? language)
            => new OptionKey2(GetOption(option), language);
    }
}
