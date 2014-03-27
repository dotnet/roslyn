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
                    if (string.Equals(Path.GetExtension(path), ".netmodule", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new MetadataFileReference(path, MetadataImageKind.Module);
                    }
                    else
                    {
                        yield return new MetadataFileReference(path, MetadataImageKind.Assembly);
                    }
                }
                else if (item is ImmutableArray<byte>)
                {
                    yield return new MetadataImageReference((ImmutableArray<byte>)item);
                }
                else if ((bytes = item as byte[]) != null)
                {
                    yield return new MetadataImageReference(bytes.AsImmutableOrNull());
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
