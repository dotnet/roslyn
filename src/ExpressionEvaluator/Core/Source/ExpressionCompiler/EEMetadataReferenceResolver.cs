// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class EEMetadataReferenceResolver : MetadataReferenceResolver
    {
        private readonly AssemblyIdentityComparer _identityComparer;
        private readonly Dictionary<string, ImmutableArray<(AssemblyIdentity, MetadataReference)>> _referencesByIdentity;

#if DEBUG
        internal readonly Dictionary<AssemblyIdentity, (AssemblyIdentity Identity, int Count)> Requests = new Dictionary<AssemblyIdentity, (AssemblyIdentity Identity, int Count)>();
#endif

        internal EEMetadataReferenceResolver(AssemblyIdentityComparer identityComparer, Dictionary<string, ImmutableArray<(AssemblyIdentity, MetadataReference)>> referencesByIdentity)
        {
            _identityComparer = identityComparer;
            _referencesByIdentity = referencesByIdentity;
        }

        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            ImmutableArray<(AssemblyIdentity, MetadataReference)> pairs;
            (AssemblyIdentity, MetadataReference) pair = default;
            if (_referencesByIdentity.TryGetValue(referenceIdentity.Name, out pairs))
            {
                // TODO: Use _identityComparer to return the appropriate version.
                pair = pairs[0];
            }
#if DEBUG
            (AssemblyIdentity Identity, int Count) request;
            if (!Requests.TryGetValue(referenceIdentity, out request))
            {
                request = (referenceIdentity, 0);
            }
            Requests[referenceIdentity] = (pair.Item1, request.Count + 1);
#endif
            return (PortableExecutableReference)pair.Item2;
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
