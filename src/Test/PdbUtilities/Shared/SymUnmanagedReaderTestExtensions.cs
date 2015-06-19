// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader
{
    internal static class SymUnmanagedReaderTestExtensions
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
                throw new InvalidOperationException(string.Format("Read only {0} of {1} sequence points.", numRead, numAvailable));
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

        public static ISymUnmanagedReader CreateReader(Stream pdbStream, object metadataImporter)
        {
            // NOTE: The product uses a different GUID (Microsoft.CodeAnalysis.ExpressionEvaluator.DkmUtilities.s_symUnmanagedReaderClassId).
            Guid corSymReaderSxS = new Guid("0A3976C5-4529-4ef8-B0B0-42EED37082CD");
            var reader = (ISymUnmanagedReader)Activator.CreateInstance(Type.GetTypeFromCLSID(corSymReaderSxS));
            int hr = reader.Initialize(metadataImporter, null, null, new ComStreamWrapper(pdbStream));
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            return reader;
        }
    }
}
