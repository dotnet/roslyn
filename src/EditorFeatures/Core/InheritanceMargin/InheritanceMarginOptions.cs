// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal class InheritanceMarginOptions
    {
        public static readonly PerLanguageOption2<bool> ShowInheritanceMargin =
            new(nameof(InheritanceMarginOptions),
                nameof(ShowInheritanceMargin),
                defaultValue: false,
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowInheritanceMargin"));
    }
}
