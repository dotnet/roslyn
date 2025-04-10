// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpressionWithElement
    {
        internal void AddToArguments(AnalyzedArguments analyzedArguments)
        {
            int previousLength = analyzedArguments.Arguments.Count;
            int addedLength = Arguments.Length;

            addRange(analyzedArguments.Arguments, previousLength, fillValue: null!, Arguments, addedLength);
            addRange(analyzedArguments.Names, previousLength, fillValue: null, ArgumentNamesOpt, addedLength);
            addRange(analyzedArguments.RefKinds, previousLength, fillValue: RefKind.None, ArgumentRefKindsOpt, addedLength);

            static void addRange<T>(ArrayBuilder<T> builder, int previousLength, T fillValue, ImmutableArray<T> addedOpt, int addedLength)
            {
                Debug.Assert(builder.Count == 0 || builder.Count == previousLength);
                Debug.Assert(addedOpt.IsDefault || addedOpt.Length == addedLength);

                if (addedOpt.IsDefault)
                {
                    if (builder.Count > 0)
                    {
                        builder.AddMany(fillValue, addedLength);
                    }
                }
                else
                {
                    if (builder.Count == 0)
                    {
                        builder.AddMany(fillValue, previousLength);
                    }
                    builder.AddRange(addedOpt);
                }

                Debug.Assert(builder.Count == 0 || builder.Count == previousLength + addedLength);
            }
        }
    }
}
