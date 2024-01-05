// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Microsoft.Cci
{
    /// <summary>
    /// This PEBuilder adds an .mvid section.
    /// </summary>
    internal sealed class ExtendedPEBuilder
        : ManagedPEBuilder
    {
        private const string MvidSectionName = ".mvid";
        public const int SizeOfGuid = 16;

        // When the section is built with a placeholder, the placeholder blob is saved for later fixing up.
        private Blob _mvidSectionFixup = default(Blob);

        // Only include the .mvid section in ref assemblies
        private readonly bool _withMvidSection;

        public ExtendedPEBuilder(
            PEHeaderBuilder header,
            MetadataRootBuilder metadataRootBuilder,
            BlobBuilder ilStream,
            BlobBuilder mappedFieldData,
            BlobBuilder managedResources,
            ResourceSectionBuilder nativeResources,
            DebugDirectoryBuilder debugDirectoryBuilder,
            int strongNameSignatureSize,
            MethodDefinitionHandle entryPoint,
            CorFlags flags,
            Func<IEnumerable<Blob>, BlobContentId> deterministicIdProvider,
            bool withMvidSection)
            : base(header, metadataRootBuilder, ilStream, mappedFieldData, managedResources, nativeResources,
                  debugDirectoryBuilder, strongNameSignatureSize, entryPoint, flags, deterministicIdProvider)
        {
            _withMvidSection = withMvidSection;
        }

        protected override ImmutableArray<Section> CreateSections()
        {
            var baseSections = base.CreateSections();

            if (_withMvidSection)
            {
                var builder = ArrayBuilder<Section>.GetInstance(baseSections.Length + 1);

                builder.Add(new Section(MvidSectionName, SectionCharacteristics.MemRead |
                    SectionCharacteristics.ContainsInitializedData |
                    SectionCharacteristics.MemDiscardable));

                builder.AddRange(baseSections);
                return builder.ToImmutableAndFree();
            }
            else
            {
                return baseSections;
            }
        }

        protected override BlobBuilder SerializeSection(string name, SectionLocation location)
        {
            if (name.Equals(MvidSectionName, StringComparison.Ordinal))
            {
                Debug.Assert(_withMvidSection);
                return SerializeMvidSection();
            }

            return base.SerializeSection(name, location);
        }

        internal BlobContentId Serialize(BlobBuilder peBlob, out Blob mvidSectionFixup)
        {
            var result = base.Serialize(peBlob);
            mvidSectionFixup = _mvidSectionFixup;
            return result;
        }

        private BlobBuilder SerializeMvidSection()
        {
            var sectionBuilder = new BlobBuilder();

            // The guid will be filled in later:
            _mvidSectionFixup = sectionBuilder.ReserveBytes(SizeOfGuid);
            var mvidWriter = new BlobWriter(_mvidSectionFixup);
            mvidWriter.WriteBytes(0, _mvidSectionFixup.Length);
            Debug.Assert(mvidWriter.RemainingBytes == 0);

            return sectionBuilder;
        }
    }
}
