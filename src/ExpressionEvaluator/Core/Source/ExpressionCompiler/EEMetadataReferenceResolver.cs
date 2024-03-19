// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class EEMetadataReferenceResolver : MetadataReferenceResolver
    {
        private readonly AssemblyIdentityComparer _identityComparer;
        private readonly IReadOnlyDictionary<string, ImmutableArray<(AssemblyIdentity Identity, MetadataReference Reference)>> _referencesBySimpleName;

#if DEBUG
        internal readonly Dictionary<AssemblyIdentity, (AssemblyIdentity? Identity, int Count)> Requests =
            new Dictionary<AssemblyIdentity, (AssemblyIdentity? Identity, int Count)>();
#endif

        internal EEMetadataReferenceResolver(
            AssemblyIdentityComparer identityComparer,
            IReadOnlyDictionary<string, ImmutableArray<(AssemblyIdentity Identity, MetadataReference Reference)>> referencesBySimpleName)
        {
            _identityComparer = identityComparer;
            _referencesBySimpleName = referencesBySimpleName;
        }

        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            (AssemblyIdentity? Identity, MetadataReference? Reference) result = default;
            if (_referencesBySimpleName.TryGetValue(referenceIdentity.Name, out var references))
            {
                result = GetBestMatch(references, referenceIdentity);
            }
#if DEBUG
            if (!Requests.TryGetValue(referenceIdentity, out var request))
            {
                request = (referenceIdentity, 0);
            }
            Requests[referenceIdentity] = (result.Identity, request.Count + 1);
#endif
            return (PortableExecutableReference?)result.Reference;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
            => throw ExceptionUtilities.Unreachable();

        public override bool Equals(object? other)
            => throw ExceptionUtilities.Unreachable();

        public override int GetHashCode()
            => throw ExceptionUtilities.Unreachable();

        private (AssemblyIdentity? Identity, MetadataReference? Reference) GetBestMatch(
            ImmutableArray<(AssemblyIdentity Identity, MetadataReference Reference)> references,
            AssemblyIdentity referenceIdentity)
        {
            (AssemblyIdentity? Identity, MetadataReference? Reference) best = default;
            foreach (var pair in references)
            {
                var identity = pair.Identity;
                var compareResult = _identityComparer.Compare(referenceIdentity, identity);
                switch (compareResult)
                {
                    case AssemblyIdentityComparer.ComparisonResult.NotEquivalent:
                        break;
                    case AssemblyIdentityComparer.ComparisonResult.Equivalent:
                        return pair;
                    case AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion:
                        if (best.Identity is null || identity.Version > best.Identity.Version)
                        {
                            best = pair;
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(compareResult);
                }
            }

            return best;
        }
    }
}
