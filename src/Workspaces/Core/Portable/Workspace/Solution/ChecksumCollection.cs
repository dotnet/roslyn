// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// collection which children is checksum.
    /// </summary>
    internal abstract class ChecksumCollection : ChecksumWithChildren, IEnumerable<Checksum>
    {
        protected ChecksumCollection(WellKnownSynchronizationKind kind, Checksum[] checksums) : this(kind, (object[])checksums)
        {
        }

        protected ChecksumCollection(WellKnownSynchronizationKind kind, object[] checksums) : base(kind, checksums)
        {
        }

        public int Count => Children.Count;
        public Checksum this[int index] => (Checksum)Children[index];

        public IEnumerator<Checksum> GetEnumerator()
        {
            return this.Children.Cast<Checksum>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // we have a type for each kind so that we can distinguish these later
    internal class ProjectChecksumCollection : ChecksumCollection
    {
        public ProjectChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public ProjectChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.ProjectChecksumCollection, checksums) { }
    }

    internal class DocumentChecksumCollection : ChecksumCollection
    {
        public DocumentChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public DocumentChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.DocumentChecksumCollection, checksums) { }
    }

    internal class TextDocumentChecksumCollection : ChecksumCollection
    {
        public TextDocumentChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public TextDocumentChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.TextDocumentChecksumCollection, checksums) { }
    }

    internal class AnalyzerConfigDocumentChecksumCollection : ChecksumCollection
    {
        public AnalyzerConfigDocumentChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public AnalyzerConfigDocumentChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.AnalyzerConfigDocumentChecksumCollection, checksums) { }
    }

    internal class ProjectReferenceChecksumCollection : ChecksumCollection
    {
        public ProjectReferenceChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public ProjectReferenceChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.ProjectReferenceChecksumCollection, checksums) { }
    }

    internal class MetadataReferenceChecksumCollection : ChecksumCollection
    {
        public MetadataReferenceChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public MetadataReferenceChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.MetadataReferenceChecksumCollection, checksums) { }
    }

    internal class AnalyzerReferenceChecksumCollection : ChecksumCollection
    {
        public AnalyzerReferenceChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public AnalyzerReferenceChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.AnalyzerReferenceChecksumCollection, checksums) { }
    }
}
