// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal sealed class TieredAnalyzerConfigOptions
{
    public readonly AnalyzerConfigOptions EditorConfigOptions;
    public readonly IGlobalOptionService GlobalOptions;

    public readonly string EditorConfigFileName;
    public readonly string Language;

    public TieredAnalyzerConfigOptions(AnalyzerConfigOptions editorConfigOptions, IGlobalOptionService globalOptions, string language, string editorConfigFileName)
    {
        EditorConfigOptions = editorConfigOptions;
        GlobalOptions = globalOptions;
        Language = language;
        EditorConfigFileName = editorConfigFileName;
    }

    public void GetInitialLocationAndValue<TValue>(
        IOption2 option,
        out SettingLocation location,
        out TValue initialValue)
        where TValue : notnull
    {
        if (EditorConfigOptions.TryGetEditorConfigOption<TValue>(option, out var editorConfigValue) && editorConfigValue is not null)
        {
            location = new SettingLocation(LocationKind.EditorConfig, EditorConfigFileName);
            initialValue = editorConfigValue;
        }
        else
        {
            location = new SettingLocation(LocationKind.VisualStudio, Path: null);
            initialValue = GlobalOptions.GetOption<TValue>(new OptionKey2(option, option.IsPerLanguage ? Language : null))!;
        }
    }
}
