// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities.BaseUtility;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Editor option to indicate if the RoslynPackage is loaded/unloaded.
/// This is done to support DeferCreationAttribute of Editor extensions that should be
/// defer created only after Roslyn package has been loaded.
/// See https://github.com/dotnet/roslyn/issues/62877#issuecomment-1271493105 for more details.
/// </summary>
[Export(typeof(EditorOptionDefinition))]
[Name(OptionName)]
[DefaultEditorOptionValue(false)]
internal sealed class IsRoslynPackageLoadedOption : EditorOptionDefinition<bool>
{
    private static readonly EditorOptionKey<bool> s_optionKey = new(OptionName);
    public const string OptionName = nameof(IsRoslynPackageLoadedOption);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public IsRoslynPackageLoadedOption()
    {
    }

    public override bool Default => false;

    public override EditorOptionKey<bool> Key => s_optionKey;
}
