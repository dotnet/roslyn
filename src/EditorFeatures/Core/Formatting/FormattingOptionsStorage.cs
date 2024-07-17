// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal sealed class FormattingOptionsStorage
{
    public static readonly PerLanguageOption2<bool> FormatOnPaste =
        new("dotnet_format_on_paste", defaultValue: true);

    public static readonly Option2<bool> FormatOnSave = new("dotnet_format_on_save", defaultValue: true);
}
