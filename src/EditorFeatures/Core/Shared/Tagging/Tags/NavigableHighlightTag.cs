// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    /// <summary>
    /// The base type of any text marker tags that can be navigated with Ctrl+Shift+Up and Ctrl+Shift+Down.
    /// </summary>
    /// <remarks>
    /// Unless you are writing code relating to reference or keyword highlighting, you should not be using
    /// this type.</remarks>
    internal abstract class NavigableHighlightTag : TextMarkerTag
    {
        protected NavigableHighlightTag(string type) : base(type)
        {
        }
    }
}
