// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.ReferenceHighlighting;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    internal sealed class DefinitionHighlightTag : NavigableHighlightTag
    {
        public const string TagId = ReferenceHighlightingConstants.DefinitionTagId;

        public static readonly DefinitionHighlightTag Instance = new();

        private DefinitionHighlightTag()
            : base(TagId)
        {
        }
    }
}
