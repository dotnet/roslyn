﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.UnitTests.Remote
{
    [Export(typeof(ISerializerOverrideService)), Shared, PartNotDiscoverable]
    internal sealed class TestSerializerOverrideService : ISerializerOverrideService
    {
        private static readonly ImmutableDictionary<MetadataReference, string> s_wellKnownReferenceNames = ImmutableDictionary.Create<MetadataReference, string>(ReferenceEqualityComparer.Instance)
            .Add(TestBase.MscorlibRef_v46, nameof(TestBase.MscorlibRef_v46))
            .Add(TestBase.SystemRef_v46, nameof(TestBase.SystemRef_v46))
            .Add(TestBase.SystemCoreRef_v46, nameof(TestBase.SystemCoreRef_v46))
            .Add(TestBase.ValueTupleRef, nameof(TestBase.ValueTupleRef))
            .Add(TestBase.SystemRuntimeFacadeRef, nameof(TestBase.SystemRuntimeFacadeRef));
        private static readonly ImmutableDictionary<string, MetadataReference> s_wellKnownReferences = ImmutableDictionary.Create<string, MetadataReference>()
            .AddRange(s_wellKnownReferenceNames.Select(pair => KeyValuePairUtil.Create(pair.Value, pair.Key)));

        /// <summary>
        /// Gate to serialize reads/writes to <see cref="_sharedTestGeneratorReferences"/>.
        /// </summary>
        private readonly object _gate = new();
        private ConcurrentDictionary<Guid, TestGeneratorReference> _sharedTestGeneratorReferences;

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
                    _sharedTestGeneratorReferences ??= new ConcurrentDictionary<Guid, TestGeneratorReference>();

                    return _sharedTestGeneratorReferences;
                }
            }

            set
            {
                lock (_gate)
                {
                    // If we're already being assigned the same set of references as before, we're fine as that won't change anything.
                    // Ideally, every time we created a new RemoteWorkspace we'd have a new MEF container; this would ensure that
                    // the assignment earlier before we create the RemoteWorkspace was always the first assignment. However the
                    // ExportProviderCache.cs in our unit tests hands out the same MEF container multpile times instead of implementing the expected
                    // contract. See https://github.com/dotnet/roslyn/issues/25863 for further details.
                    Contract.ThrowIfFalse(_sharedTestGeneratorReferences == null ||
                        _sharedTestGeneratorReferences == value, "We already have a shared set of references, we shouldn't be getting another one.");
                    _sharedTestGeneratorReferences = value;
                }
            }
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestSerializerOverrideService()
        {
        }

        public void WriteMetadataReferenceTo(MetadataReference reference, ObjectWriter writer, SolutionReplicationContext context, Action baseCall, CancellationToken cancellationToken)
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
                baseCall();
            }
        }

        public MetadataReference ReadMetadataReferenceFrom(ObjectReader reader, Func<MetadataReference> baseCall, CancellationToken cancellationToken)
        {
            if (reader.ReadBoolean())
            {
                // this is a well-known reference
                return s_wellKnownReferences[reader.ReadString()];
            }
            else
            {
                return baseCall();
            }
        }

        public void WriteAnalyzerReferenceTo(AnalyzerReference reference, ObjectWriter writer, Action baseCall, CancellationToken cancellationToken)
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
                baseCall();
            }
        }

        public AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, Func<AnalyzerReference> baseCall, CancellationToken cancellationToken)
        {
            var testGeneratorReferenceGuid = reader.ReadGuid();

            if (testGeneratorReferenceGuid != Guid.Empty)
            {
                Contract.ThrowIfFalse(_sharedTestGeneratorReferences.TryGetValue(testGeneratorReferenceGuid, out var generatorReference));
                return generatorReference;
            }
            else
            {
                return baseCall();
            }
        }
    }
}
