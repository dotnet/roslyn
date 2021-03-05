// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.InheritanceChainMargin
{
    internal class InheritanceMarginTag : IGlyphTag
    {
        public readonly TaggedText DescriptionText;
        public readonly int LineNumber;
        public readonly Func<Task> NavigationFunc;

        private InheritanceMarginTag(
            TaggedText descriptionText,
            int lineNumber,
            Func<Task> navigationFunc)
        {
            DescriptionText = descriptionText;
            LineNumber = lineNumber;
            NavigationFunc = navigationFunc;
        }
    }
}
