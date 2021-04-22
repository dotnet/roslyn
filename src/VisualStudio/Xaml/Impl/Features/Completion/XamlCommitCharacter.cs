// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion
{
    internal class XamlCommitCharacter
    {
        //
        // Summary:
        //     Gets or sets the commit character.
        public char Character { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the commit character should be inserted
        //     or not.
        public bool Insert { get; set; }
    }
}
