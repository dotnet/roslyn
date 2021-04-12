// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        /// <summary>
        /// Looks for metadata references among the assembly file references given to the compilation when constructed.
        /// When scripts are included into a project we don't want #r's to reference other assemblies than those 
        /// specified explicitly in the project references.
        /// </summary>
        internal sealed class ExistingReferencesResolver : MetadataReferenceResolver, IEquatable<ExistingReferencesResolver>
        {
            private readonly MetadataReferenceResolver _resolver;
            private readonly ImmutableArray<MetadataReference> _availableReferences;
            private readonly Lazy<HashSet<AssemblyIdentity>> _lazyAvailableReferences;

            public ExistingReferencesResolver(MetadataReferenceResolver resolver, ImmutableArray<MetadataReference> availableReferences)
            {
                Debug.Assert(resolver != null);
                Debug.Assert(availableReferences != null);

                _resolver = resolver;
                _availableReferences = availableReferences;

                // Delay reading assembly identities until they are actually needed (only when #r is encountered).
                _lazyAvailableReferences = new Lazy<HashSet<AssemblyIdentity>>(() => new HashSet<AssemblyIdentity>(
                    from reference in _availableReferences
                    let identity = TryGetIdentity(reference)
                    where identity != null
                    select identity!));
            }

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
            {
                var resolvedReferences = _resolver.ResolveReference(reference, baseFilePath, properties);
                return resolvedReferences.WhereAsArray(r => _lazyAvailableReferences.Value.Contains(TryGetIdentity(r)!));
            }

            private static AssemblyIdentity? TryGetIdentity(MetadataReference metadataReference)
            {
                var peReference = metadataReference as PortableExecutableReference;
                if (peReference == null || peReference.Properties.Kind != MetadataImageKind.Assembly)
                {
                    return null;
                }

                try
                {
                    PEAssembly assembly = ((AssemblyMetadata)peReference.GetMetadataNoCopy()).GetAssembly()!;
                    return assembly.Identity;
                }
                catch (Exception e) when (e is BadImageFormatException || e is IOException)
                {
                    // ignore, metadata reading errors are reported by the compiler for the existing references
                    return null;
                }
            }

            public override int GetHashCode()
            {
                return _resolver.GetHashCode();
            }

            public bool Equals(ExistingReferencesResolver? other)
            {
                return
                    other is object &&
                    _resolver.Equals(other._resolver) &&
                    _availableReferences.SequenceEqual(other._availableReferences);
            }

            public override bool Equals(object? other) => other is ExistingReferencesResolver obj && Equals(obj);
        }
    }
}
