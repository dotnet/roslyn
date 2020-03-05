﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct AssemblyReaders
    {
        public readonly MetadataReader MetadataReader;
        public readonly object SymReader;

        public AssemblyReaders(MetadataReader metadataReader, object symReader)
        {
            MetadataReader = metadataReader;
            SymReader = symReader;
        }
    }
}
