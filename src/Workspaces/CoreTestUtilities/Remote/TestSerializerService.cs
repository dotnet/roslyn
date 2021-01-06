// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.UnitTests.Remote
{
    internal sealed class TestSerializerService : SerializerService
    {
        private static readonly ImmutableDictionary<MetadataReference, string> s_wellKnownReferenceNames = ImmutableDictionary.Create<MetadataReference, string>(ReferenceEqualityComparer.Instance)
            .Add(TestBase.MscorlibRef_v46, nameof(TestBase.MscorlibRef_v46))
            .Add(TestBase.SystemRef_v46, nameof(TestBase.SystemRef_v46))
            .Add(TestBase.SystemCoreRef_v46, nameof(TestBase.SystemCoreRef_v46))
            .Add(TestBase.ValueTupleRef, nameof(TestBase.ValueTupleRef))
            .Add(TestBase.SystemRuntimeFacadeRef, nameof(TestBase.SystemRuntimeFacadeRef));
        private static readonly ImmutableDictionary<string, MetadataReference> s_wellKnownReferences = ImmutableDictionary.Create<string, MetadataReference>()
            .AddRange(s_wellKnownReferenceNames.Select(pair => KeyValuePairUtil.Create(pair.Value, pair.Key)));

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public TestSerializerService(HostWorkspaceServices workspaceServices)
            : base(workspaceServices)
        {
        }

        public override void WriteMetadataReferenceTo(MetadataReference reference, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
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
                base.WriteMetadataReferenceTo(reference, writer, context, cancellationToken);
            }
        }

        public override MetadataReference ReadMetadataReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            if (reader.ReadBoolean())
            {
                // this is a well-known reference
                return s_wellKnownReferences[reader.ReadString()];
            }
            else
            {
                return base.ReadMetadataReferenceFrom(reader, cancellationToken);
            }
        }

        [ExportWorkspaceServiceFactory(typeof(ISerializerService), layer: ServiceLayer.Test), Shared, PartNotDiscoverable]
        internal new sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new TestSerializerService(workspaceServices);
        }
    }
}
