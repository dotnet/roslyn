// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Internal.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(LanguageServerFeatureOptions))]
internal class VisualStudioLanguageServerFeatureOptions : LanguageServerFeatureOptions
{
    private readonly Lazy<bool> _showAllCSharpCodeActions;

    [ImportingConstructor]
    public VisualStudioLanguageServerFeatureOptions()
    {
        _showAllCSharpCodeActions = new Lazy<bool>(() =>
        {
            var featureFlags = (IVsFeatureFlags)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsFeatureFlags));
            var showAllCSharpCodeActions = featureFlags.IsFeatureEnabled(WellKnownFeatureFlagNames.ShowAllCSharpCodeActions, defaultValue: false);
            return showAllCSharpCodeActions;
        });
    }

    public override bool ShowAllCSharpCodeActions => _showAllCSharpCodeActions.Value;
}
