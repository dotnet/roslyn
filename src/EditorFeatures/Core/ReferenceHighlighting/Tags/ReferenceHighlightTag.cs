// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.ReferenceHighlighting;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    internal sealed class ReferenceHighlightTag : NavigableHighlightTag
    {
        public const string TagId = ReferenceHighlightingConstants.ReferenceTagId;

        public static readonly ReferenceHighlightTag Instance = new();

        private ReferenceHighlightTag()
            : base(TagId)
        {
        }
    }
}
