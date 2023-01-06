﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    internal sealed class InlineHintsViewOptions
    {
        public static readonly Option2<bool> DisplayAllHintsWhilePressingAltF1 = new(
            "InlineHintsOptions_DisplayAllHintsWhilePressingAltF1", defaultValue: true);

        public static readonly PerLanguageOption2<bool> ColorHints = new(
            "InlineHintsOptions_ColorHints", defaultValue: true);
    }
}
