// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class EEMetadataReferenceResolver : MetadataReferenceResolver
    {
        private readonly Dictionary<AssemblyIdentity, MetadataReference> _referencesByIdentity;

#if DEBUG
        internal readonly Dictionary<AssemblyIdentity, int> Requests = new Dictionary<AssemblyIdentity, int>();
#endif

        internal EEMetadataReferenceResolver(Dictionary<AssemblyIdentity, MetadataReference> referencesByIdentity)
        {
            _referencesByIdentity = referencesByIdentity;
        }

        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
#if DEBUG
            int n;
            Requests.TryGetValue(referenceIdentity, out n);
            Requests[referenceIdentity] = n + 1;
#endif
            MetadataReference reference;
            _referencesByIdentity.TryGetValue(referenceIdentity, out reference);
            return (PortableExecutableReference)reference;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool Equals(object other)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override int GetHashCode()
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
