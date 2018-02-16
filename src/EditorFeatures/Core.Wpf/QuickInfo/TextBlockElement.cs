// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    /// <summary>
    /// Holds the <see cref="TextBlock"/> for a particular <see cref="Microsoft.CodeAnalysis.QuickInfo.QuickInfoSectionKinds">QuickInfoSectionKind</see>.
    /// </summary>
    internal class TextBlockElement
    {
        public string Kind { get; }
        public TextBlock Block { get; }

        public TextBlockElement(string kind, TextBlock block)
        {
            Kind = kind;
            Block = block;
        }
    }
}
