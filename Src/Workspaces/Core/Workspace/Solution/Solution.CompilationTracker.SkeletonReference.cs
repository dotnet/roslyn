using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Solution
    {
        private partial class CompilationTracker
        {
            private class SkeletonReference
            {
                public static readonly SkeletonReference Empty = new SkeletonReference(assemblyBytes: ReadOnlyArray<byte>.Null, xmlDocCommentBytes: null, publicHash: 0, assemblyName: string.Empty);

                public readonly ReadOnlyArray<byte> AssemblyBytes;
                public readonly byte[] XmlDocCommentBytes;
                public readonly int PublicHash;
                public readonly string AssemblyName;

                private readonly ConcurrentDictionary<Tuple<string, bool>, MetadataReference> metadataReferences = new ConcurrentDictionary<Tuple<string, bool>, MetadataReference>();

                public SkeletonReference(ReadOnlyArray<byte> assemblyBytes, byte[] xmlDocCommentBytes, int publicHash, string assemblyName)
                {
                    this.AssemblyBytes = assemblyBytes;
                    this.XmlDocCommentBytes = xmlDocCommentBytes;
                    this.PublicHash = publicHash;
                    this.AssemblyName = assemblyName;
                }

                internal MetadataReference GetMetadataReference(string alias, bool embedInteropTypes)
                {
                    if (AssemblyBytes.IsNull)
                    {
                        return null;
                    }

                    var key = Tuple.Create(alias, embedInteropTypes);
                    return metadataReferences.GetOrAdd(key, k =>
                    {
                        XmlDocumentationProvider xmlDocProvider = XmlDocCommentBytes != null ? XmlDocumentationProvider.Create(XmlDocCommentBytes) : null;
                        return new MetadataImageReference(AssemblyBytes, documentation: xmlDocProvider, alias: k.Item1, embedInteropTypes: k.Item2, display: AssemblyName);
                    });
                }
            }
        }
    }
}