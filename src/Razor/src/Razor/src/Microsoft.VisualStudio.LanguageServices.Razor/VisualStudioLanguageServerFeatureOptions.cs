// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(LanguageServerFeatureOptions))]
internal class VisualStudioLanguageServerFeatureOptions : LanguageServerFeatureOptions
{
    private readonly ILspEditorFeatureDetector _lspEditorFeatureDetector;
    private readonly Lazy<bool> _showAllCSharpCodeActions;

    [ImportingConstructor]
    public VisualStudioLanguageServerFeatureOptions(ILspEditorFeatureDetector lspEditorFeatureDetector)
    {
        _lspEditorFeatureDetector = lspEditorFeatureDetector;

        _showAllCSharpCodeActions = new Lazy<bool>(() =>
        {
            var featureFlags = (IVsFeatureFlags)Package.GetGlobalService(typeof(SVsFeatureFlags));
            var showAllCSharpCodeActions = featureFlags.IsFeatureEnabled(WellKnownFeatureFlagNames.ShowAllCSharpCodeActions, defaultValue: false);
            return showAllCSharpCodeActions;
        });
    }

    // We don't currently support file creation operations on VS Codespaces or VS Liveshare
    public override bool SupportsFileManipulation => !IsCodespacesOrLiveshare;

    public override bool ReturnCodeActionAndRenamePathsWithPrefixedSlash => false;

    private bool IsCodespacesOrLiveshare => _lspEditorFeatureDetector.IsRemoteClient() || _lspEditorFeatureDetector.IsLiveShareHost();

    public override bool ShowAllCSharpCodeActions => _showAllCSharpCodeActions.Value;
}
