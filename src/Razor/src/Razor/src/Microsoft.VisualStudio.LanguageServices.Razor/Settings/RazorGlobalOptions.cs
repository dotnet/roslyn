// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.Razor.Settings;

[Export(typeof(RazorGlobalOptions))]
internal sealed class RazorGlobalOptions
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    public RazorGlobalOptions(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool UseTabs
    {
        get => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.UseTabs);
        set => _globalOptions.SetGlobalOption(RazorLineFormattingOptionsStorage.UseTabs, value);
    }

    public int TabSize
    {
        get => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.TabSize);
        set => _globalOptions.SetGlobalOption(RazorLineFormattingOptionsStorage.TabSize, value);
    }
}
