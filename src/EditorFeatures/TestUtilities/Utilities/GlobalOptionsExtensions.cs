// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal static class GlobalOptionsExtensions
{
    /// <summary>
    /// Sets options stored in <see cref="IGlobalOptionService"/> that are read by command handlers from the text editor to given <see cref="IEditorOptions"/>.
    /// </summary>
    public static void SetEditorOptions(this IGlobalOptionService globalOptions, IEditorOptions editorOptions, string language)
    {
        editorOptions.SetOptionValue(DefaultOptions.IndentStyleId, globalOptions.GetOption(FormattingOptions2.SmartIndent, language).ToEditorIndentStyle());
        editorOptions.SetOptionValue(DefaultOptions.NewLineCharacterOptionId, globalOptions.GetOption(FormattingOptions2.NewLine, language));
        editorOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, globalOptions.GetOption(FormattingOptions2.TabSize, language));
        editorOptions.SetOptionValue(DefaultOptions.IndentSizeOptionId, globalOptions.GetOption(FormattingOptions2.IndentationSize, language));
        editorOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !globalOptions.GetOption(FormattingOptions2.UseTabs, language));
    }
}
