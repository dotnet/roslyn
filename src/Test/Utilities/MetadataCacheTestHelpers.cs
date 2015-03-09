// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    internal class MetadataCacheTestHelpers
    {
        internal static IEnumerable<MetadataReference> CreateMetadataReferences(params object[] pathsOrBytesOrCompilations)
        {
            foreach (var item in pathsOrBytesOrCompilations)
            {
                string path;
                byte[] bytes;
                Compilation compilation;

                if ((path = item as string) != null)
                {
                    var kind = (string.Equals(Path.GetExtension(path), ".netmodule", StringComparison.OrdinalIgnoreCase)) ?
                        MetadataImageKind.Module : MetadataImageKind.Assembly;

                    yield return MetadataReference.CreateFromFile(path, new MetadataReferenceProperties(kind));
                }
                else if (item is ImmutableArray<byte>)
                {
                    yield return MetadataReference.CreateFromImage((ImmutableArray<byte>)item);
                }
                else if ((bytes = item as byte[]) != null)
                {
                    yield return MetadataReference.CreateFromImage(bytes.AsImmutableOrNull());
                }
                else if ((compilation = item as Compilation) != null)
                {
                    yield return compilation.ToMetadataReference();
                }
                else
                {
                    throw new NotSupportedException("Expected string, byte[], ImmutableArray<byte> or Compilation.");
                }
            }
        }
    }
}
