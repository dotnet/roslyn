// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpression
    {
        private partial void Validate()
        {
            var collectionCreation = this.GetUnconvertedCollectionCreation();
            var collectionBuilderElementsPlaceholder = this.CollectionBuilderElementsPlaceholder;

            if (collectionCreation is BoundCall boundCall)
            {
                Debug.Assert(this.CollectionTypeKind == CollectionExpressionTypeKind.CollectionBuilder);
                Debug.Assert(boundCall.Arguments is [.., BoundCollectionBuilderElementsPlaceholder placeHolder] &&
                    placeHolder == collectionBuilderElementsPlaceholder);
                Debug.Assert(collectionBuilderElementsPlaceholder.Type!.IsReadOnlySpan());
            }
            else
            {
                Debug.Assert(collectionBuilderElementsPlaceholder is null);
            }

            if (this.CollectionTypeKind == CollectionExpressionTypeKind.CollectionBuilder)
            {
                Debug.Assert(collectionCreation is BoundCall);
                Debug.Assert(this.CollectionCreation is not null);
                Debug.Assert(this.CollectionBuilderMethod is not null);
                Debug.Assert(collectionBuilderElementsPlaceholder is not null);
            }
            else
            {
                Debug.Assert(collectionBuilderElementsPlaceholder is null);
            }
        }

        /// <summary>
        /// Returns <see cref="CollectionCreation"/> with any outer <see cref="BoundConversion"/> nodes unwrapped. The
        /// final returned <see cref="BoundExpression"/> will either be:
        /// <list type="bullet">
        /// <item><see langword="null"/> (when no <c>with(...)</c> element is present),</item>
        /// <item>a <see cref="BoundObjectCreationExpression"/> (when targeting a collection with a constructor),</item>
        /// <item>a <see cref="BoundCall"/> (when targeting a CollectionBuilder method),</item>
        /// <item>a <see cref="BoundNewT"/> (when targeting a type parameter with the <c>new()</c> constraint,</item>
        /// <item>or a <see cref="BoundBadExpression"/> in the case of errors where the <c>with(...)</c> element does
        /// not bind properly.</item>
        /// </list>
        /// </summary>
        public BoundExpression? GetUnconvertedCollectionCreation()
        {
            var collectionCreation = this.CollectionCreation;
            while (collectionCreation is BoundConversion conversion)
                collectionCreation = conversion.Operand;

            Debug.Assert(collectionCreation
                is null
                or BoundObjectCreationExpression
                or BoundCall
                or BoundNewT
                or BoundBadExpression);

            return collectionCreation;
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
