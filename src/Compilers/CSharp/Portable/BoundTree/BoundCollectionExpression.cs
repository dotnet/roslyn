// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpression
    {
        private partial void Validate()
        {
#if DEBUG
            var collectionCreation = this.CollectionCreation;
            while (collectionCreation is BoundConversion conversion)
                collectionCreation = conversion.Operand;

            Debug.Assert(collectionCreation
                            is null
                            or BoundObjectCreationExpression
                            or BoundCall
                            or BoundNewT
                            or BoundBadExpression);

            if (collectionCreation is BoundCall boundCall)
            {
                Debug.Assert(
                    boundCall.Arguments is [.., BoundValuePlaceholder { Kind: BoundKind.ValuePlaceholder } placeHolder] &&
                    placeHolder == this.CollectionBuilderElementsPlaceholder);
            }

            if (this.CollectionTypeKind == CollectionExpressionTypeKind.CollectionBuilder)
            {
                Debug.Assert(this.CollectionCreation is not null);
                Debug.Assert(this.CollectionBuilderMethod is not null);
                Debug.Assert(this.CollectionBuilderElementsPlaceholder is not null);
            }
#endif
        }
    }

    internal partial class BoundCollectionExpressionBase
    {
        private partial void Validate()
        {
            foreach (var element in Elements)
                Debug.Assert(element is not BoundUnconvertedWithElement);
        }

        /// <summary>
        /// Returns true if the collection expression contains any spreads.
        /// </summary>
        /// <param name="numberIncludingLastSpread">The number of elements up to and including the
        /// last spread element. If the length of the collection expression is known, this is the number
        /// of elements evaluated before any are added to the collection instance in lowering.</param>
        /// <param name="hasKnownLength"><see langword="true"/> if all the spread elements are countable.</param>
        internal bool HasSpreadElements(out int numberIncludingLastSpread, out bool hasKnownLength)
        {
            hasKnownLength = true;
            numberIncludingLastSpread = 0;
            for (int i = 0; i < Elements.Length; i++)
            {
                if (Elements[i] is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    numberIncludingLastSpread = i + 1;
                    if (spreadElement.LengthOrCount is null)
                    {
                        hasKnownLength = false;
                    }
                }
            }
            return numberIncludingLastSpread > 0;
        }

        public new bool IsParamsArrayOrCollection
        {
            get
            {
                return base.IsParamsArrayOrCollection;
            }
            init
            {
                base.IsParamsArrayOrCollection = value;
            }
        }
    }
}
