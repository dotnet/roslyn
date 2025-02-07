// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

#pragma warning disable CA1416 // Validate platform compatibility

namespace Microsoft.CodeAnalysis.UnitTests.Remote
{
#if NET
    [SupportedOSPlatform("windows")]
#endif
    [method: Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    internal sealed class TestSerializerService(
        ConcurrentDictionary<Guid, TestGeneratorReference> sharedTestGeneratorReferences,
        SolutionServices workspaceServices)
        : SerializerService(workspaceServices)
    {
        private static readonly ImmutableDictionary<MetadataReference, string?> s_wellKnownReferenceNames = ImmutableDictionary.Create<MetadataReference, string?>(ReferenceEqualityComparer.Instance)
            .Add(TestBase.MscorlibRef_v46, nameof(TestBase.MscorlibRef_v46))
            .Add(TestBase.SystemRef_v46, nameof(TestBase.SystemRef_v46))
            .Add(TestBase.SystemCoreRef_v46, nameof(TestBase.SystemCoreRef_v46))
            .Add(TestBase.ValueTupleRef, nameof(TestBase.ValueTupleRef))
            .Add(TestBase.SystemRuntimeFacadeRef, nameof(TestBase.SystemRuntimeFacadeRef));
        private static readonly ImmutableDictionary<string, MetadataReference> s_wellKnownReferences = ImmutableDictionary.Create<string, MetadataReference>()
            .AddRange(s_wellKnownReferenceNames.Select(pair => KeyValuePairUtil.Create(pair.Value!, pair.Key)));

        private readonly ConcurrentDictionary<Guid, TestGeneratorReference> _sharedTestGeneratorReferences = sharedTestGeneratorReferences;

        protected override void WriteMetadataReferenceTo(MetadataReference reference, ObjectWriter writer)
        {
            var wellKnownReferenceName = s_wellKnownReferenceNames.GetValueOrDefault(reference, null);
            if (wellKnownReferenceName is not null)
            {
                writer.WriteBoolean(true);
                writer.WriteString(wellKnownReferenceName);
            }
            else
            {
                writer.WriteBoolean(false);
                base.WriteMetadataReferenceTo(reference, writer);
            }
        }

        protected override MetadataReference ReadMetadataReferenceFrom(ObjectReader reader)
            => reader.ReadBoolean()
                ? s_wellKnownReferences[reader.ReadRequiredString()]
                : base.ReadMetadataReferenceFrom(reader);

        protected override Checksum CreateChecksum(AnalyzerReference reference)
        {
#if NET
            // If we're in the oop side and we're being asked to produce our local checksum (so we can compare it to the
            // host checksum), then we want to just defer to the underlying analyzer reference of our isolated reference.
            // This underlying reference corresponds to the reference that the host has, and we do not want to make any
            // changes as long as they're both in agreement.
            if (reference is IsolatedAnalyzerFileReference { UnderlyingAnalyzerFileReference: var underlyingReference })
                reference = underlyingReference;
#endif

            return reference switch
            {
                TestGeneratorReference generatorReference => generatorReference.Checksum,
                TestAnalyzerReferenceByLanguage analyzerReferenceByLanguage => analyzerReferenceByLanguage.Checksum,
                _ => base.CreateChecksum(reference)
            };
        }

        protected override void WriteAnalyzerReferenceTo(AnalyzerReference reference, ObjectWriter writer)
        {
            if (reference is TestGeneratorReference generatorReference)
            {
                // It's a test reference, we'll just store it in a map and then just write out our GUID
                _sharedTestGeneratorReferences.TryAdd(generatorReference.Guid, generatorReference);
                writer.WriteGuid(generatorReference.Guid);
            }
            else
            {
                writer.WriteGuid(Guid.Empty);
                base.WriteAnalyzerReferenceTo(reference, writer);
            }
        }

        protected override AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader)
        {
            var testGeneratorReferenceGuid = reader.ReadGuid();

            if (testGeneratorReferenceGuid != Guid.Empty)
            {
                Contract.ThrowIfFalse(_sharedTestGeneratorReferences.TryGetValue(testGeneratorReferenceGuid, out var generatorReference));
                return generatorReference;
            }
            else
            {
                return base.ReadAnalyzerReferenceFrom(reader);
            }
        }

        [ExportWorkspaceServiceFactory(typeof(ISerializerService), layer: ServiceLayer.Test), Shared, PartNotDiscoverable]
        [Export(typeof(Factory))]
        [method: ImportingConstructor]
        [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        internal new sealed class Factory() : IWorkspaceServiceFactory
        {
            private ConcurrentDictionary<Guid, TestGeneratorReference>? _sharedTestGeneratorReferences;

            /// <summary>
            /// Gate to serialize reads/writes to <see cref="_sharedTestGeneratorReferences"/>.
            /// </summary>
            private readonly object _gate = new();

            /// <summary>
            /// In unit tests that are testing OOP, we want to be able to share test generator references directly
            /// from one side to another. We'll create multiple instances of the TestSerializerService -- one for each
            /// workspace and in different MEF compositions, but we'll share the generator references across them. This
            /// property allows the shared dictionary to be read/set.
            /// </summary>
            public ConcurrentDictionary<Guid, TestGeneratorReference> SharedTestGeneratorReferences
            {
                get
                {
                    lock (_gate)
                    {
                        _sharedTestGeneratorReferences ??= [];

                        return _sharedTestGeneratorReferences;
                    }
                }

                set
                {
                    lock (_gate)
                    {
                        // If we're already being assigned the same set of references as before, we're fine as that
                        // won't change anything. Ideally, every time we created a new RemoteWorkspace we'd have a new
                        // MEF container; this would ensure that the assignment earlier before we create the
                        // RemoteWorkspace was always the first assignment. However the ExportProviderCache.cs in our
                        // unit tests hands out the same MEF container multiple times instead of implementing the
                        // expected contract. See https://github.com/dotnet/roslyn/issues/25863 for further details.
                        Contract.ThrowIfFalse(_sharedTestGeneratorReferences == null ||
                            _sharedTestGeneratorReferences == value, "We already have a shared set of references, we shouldn't be getting another one.");
                        _sharedTestGeneratorReferences = value;
                    }
                }
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new TestSerializerService(SharedTestGeneratorReferences, workspaceServices.SolutionServices);
        }
    }
}
