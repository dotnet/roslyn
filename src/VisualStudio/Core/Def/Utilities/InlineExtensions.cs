// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Microsoft.VisualStudio.LanguageServices.Utilities;

internal static class InlineExtensions
{
    public static string? GetText(this Inline inline)
        => inline switch
        {
            Run run => run.Text,
            Hyperlink hyperlink => string.Join("", hyperlink.Inlines.Select(GetText)),
            _ => null
        };
}
