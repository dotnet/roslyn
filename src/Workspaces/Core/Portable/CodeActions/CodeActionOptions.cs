// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Options available to code fixes that are supplied by the IDE (i.e. not stored in editorconfig).
    /// </summary>
    internal readonly record struct CodeActionOptions(
        bool IsBlocking,
        bool SearchReferenceAssemblies,
        bool HideAdvancedMembers)
    {
        public static readonly CodeActionOptions Default = new(
            IsBlocking: false,
            SearchReferenceAssemblies: true,
            HideAdvancedMembers: false);
    }
}
