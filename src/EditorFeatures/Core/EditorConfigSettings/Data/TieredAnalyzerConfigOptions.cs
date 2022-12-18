// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal sealed class TieredAnalyzerConfigOptions
{
    public readonly AnalyzerConfigOptions EditorConfigOptions;
    public readonly AnalyzerConfigOptions GlobalOptions;
    public readonly string EditorConfigFileName;
    public readonly string Language;

    public TieredAnalyzerConfigOptions(AnalyzerConfigOptions editorConfigOptions, AnalyzerConfigOptions globalOptions, string language, string editorConfigFileName)
    {
        EditorConfigOptions = editorConfigOptions;
        GlobalOptions = globalOptions;
        Language = language;
        EditorConfigFileName = editorConfigFileName;
    }

    public void GetInitialLocationAndValue<TValue>(
        IOption option,
        out SettingLocation location,
        out TValue initialValue)
        where TValue : notnull
    {
        if (EditorConfigOptions.TryGetEditorConfigOption<TValue>(option, out var editorConfigValue) && editorConfigValue is not null)
        {
            location = new SettingLocation(LocationKind.EditorConfig, EditorConfigFileName);
            initialValue = editorConfigValue;
        }
        else if (GlobalOptions.TryGetEditorConfigOption<TValue>(option, out var globalValue))
        {
            location = new SettingLocation(LocationKind.VisualStudio, EditorConfigFileName);
            initialValue = globalValue;
        }
        else
        {
            // specified option is not an editorcondig option
            throw ExceptionUtilities.Unreachable();
        }
    }
}
