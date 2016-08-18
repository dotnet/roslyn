// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// this represents hierarchical checksum of solution
    /// </summary>
    internal class SolutionChecksumObject : ChecksumObjectWithChildren
    {
        public const string Name = nameof(SolutionChecksumObject);

        public SolutionChecksumObject(Serializer serializer, params object[] children) :
            base(serializer, Name, children)
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public ChecksumCollection Projects => (ChecksumCollection)Children[1];
    }

    internal class ProjectChecksumObject : ChecksumObjectWithChildren
    {
        public const string Name = nameof(ProjectChecksumObject);

        public ProjectChecksumObject(Serializer serializer, params object[] children) :
            base(serializer, Name, children)
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public Checksum CompilationOptions => (Checksum)Children[1];
        public Checksum ParseOptions => (Checksum)Children[2];

        public ChecksumCollection Documents => (ChecksumCollection)Children[3];

        public ChecksumCollection ProjectReferences => (ChecksumCollection)Children[4];
        public ChecksumCollection MetadataReferences => (ChecksumCollection)Children[5];
        public ChecksumCollection AnalyzerReferences => (ChecksumCollection)Children[6];

        public ChecksumCollection AdditionalDocuments => (ChecksumCollection)Children[7];
    }

    internal class DocumentChecksumObject : ChecksumObjectWithChildren
    {
        public const string Name = nameof(DocumentChecksumObject);

        public DocumentChecksumObject(Serializer serializer, params object[] children) :
            base(serializer, Name, children)
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public Checksum Text => (Checksum)Children[1];
    }

    /// <summary>
    /// Collection of checksums of checksum objects.
    /// </summary>
    internal class ChecksumCollection : ChecksumObjectWithChildren, IEnumerable<Checksum>
    {
        public ChecksumCollection(Serializer serializer, string kind, object[] children) :
            base(serializer, kind, children)
        {
        }

        public int Count => Children.Length;

        public Checksum this[int index] => (Checksum)Children[index];

        IEnumerator IEnumerable.GetEnumerator() => Children.GetEnumerator();

        public IEnumerator<Checksum> GetEnumerator()
        {
            foreach (var child in Children)
            {
                yield return (Checksum)child;
            }
        }
    }
}
