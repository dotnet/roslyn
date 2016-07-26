// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindReferences
{
    internal partial class DefinitionLocation
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionLocation"/> used for definitions
        /// that cannot be navigated to.  For example, C# and VB namespaces cannot be
        /// navigated to.
        /// </summary>
        private sealed class NonNavigatingDefinitionLocation : DefinitionLocation
        {
            private readonly ImmutableArray<TaggedText> _originationParts;

            public NonNavigatingDefinitionLocation(ImmutableArray<TaggedText> originationParts)
            {
                _originationParts = originationParts;
            }

            public override ImmutableArray<TaggedText> OriginationParts => _originationParts;

            public override bool CanNavigateTo() => false;
            public override bool TryNavigateTo() => false;
        }
    }
}