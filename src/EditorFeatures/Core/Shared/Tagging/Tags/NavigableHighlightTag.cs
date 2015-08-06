// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
