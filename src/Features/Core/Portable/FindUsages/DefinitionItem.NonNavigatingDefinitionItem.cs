﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal partial class DefinitionItem
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionItem"/> used for definitions
        /// that cannot be navigated to.  For example, C# and VB namespaces cannot be
        /// navigated to.
        /// </summary>
        private sealed class NonNavigatingDefinitionItem : DefinitionItem
        {
            internal override bool IsExternal => false;

            public NonNavigatingDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<TaggedText> originationParts,
                ImmutableDictionary<string, string> properties,
                bool displayIfNoReferences)
                : base(tags, displayParts, ImmutableArray<TaggedText>.Empty,
                       originationParts, ImmutableArray<DocumentSpan>.Empty,
                       properties, displayIfNoReferences)
            {
            }

            public override bool CanNavigateTo() => false;
            public override bool TryNavigateTo() => false;
        }
    }
}