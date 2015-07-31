// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.DiaSymReader
{
    public static class SymUnmanagedReaderTestExtensions
    {
        internal static ImmutableArray<SymUnmanagedSequencePoint> GetSequencePoints(this ISymUnmanagedMethod method)
        {
            // NB: method.GetSequencePoints(0, out numAvailable, ...) always returns 0.
            int numAvailable;
            int hr = method.GetSequencePointCount(out numAvailable);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            if (numAvailable == 0)
            {
                return ImmutableArray<SymUnmanagedSequencePoint>.Empty;
            }

            int[] offsets = new int[numAvailable];
            ISymUnmanagedDocument[] documents = new ISymUnmanagedDocument[numAvailable];
            int[] startLines = new int[numAvailable];
            int[] startColumns = new int[numAvailable];
            int[] endLines = new int[numAvailable];
            int[] endColumns = new int[numAvailable];

            int numRead;
            hr = method.GetSequencePoints(numAvailable, out numRead, offsets, documents, startLines, startColumns, endLines, endColumns);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException($"Read only {numRead} of {numAvailable} sequence points.");
            }

            var builder = ArrayBuilder<SymUnmanagedSequencePoint>.GetInstance(numRead);
            for (int i = 0; i < numRead; i++)
            {
                builder.Add(new SymUnmanagedSequencePoint(
                    offsets[i],
                    documents[i],
                    startLines[i],
                    startColumns[i],
                    endLines[i],
                    endColumns[i]));
            }

            return builder.ToImmutableAndFree();
        }
    }
}
