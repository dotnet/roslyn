// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    // A MethodImpl entry is a pair of implementing method and implemented
    // method. However, the implemented method is a MemberRef rather
    // than a MethodDef (e.g.: I<int>.M) and currently we are not mapping
    // MemberRefs between generations so it's not possible to track the
    // implemented method. Instead, recognizing that we do not support
    // changes to the set of implemented methods for a particular MethodDef,
    // and that we do not use the implementing methods anywhere, it's
    // sufficient to track a pair of implementing method and index.
    internal struct MethodImplKey : IEquatable<MethodImplKey>
    {
        internal MethodImplKey(int implementingMethod, int index)
        {
            Debug.Assert(implementingMethod > 0);
            Debug.Assert(index > 0);
            this.ImplementingMethod = implementingMethod;
            this.Index = index;
        }

        internal readonly int ImplementingMethod;
        internal readonly int Index;

        public override bool Equals(object obj)
        {
            return obj is MethodImplKey && Equals((MethodImplKey)obj);
        }

        public bool Equals(MethodImplKey other)
        {
            return this.ImplementingMethod == other.ImplementingMethod &&
                this.Index == other.Index;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.ImplementingMethod, this.Index);
        }
    }

    /// <summary>
    /// Represents a module from a previous compilation. Used in Edit and Continue
    /// to emit the differences in a subsequent compilation.
    /// </summary>
    public sealed class EmitBaseline
    {
        private static readonly ImmutableArray<int> s_emptyTableSizes = ImmutableArray.Create(new int[MetadataTokens.TableCount]);

        internal sealed class MetadataSymbols
        {
            public readonly IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> AnonymousTypes;
            public readonly object MetadataDecoder;

            public MetadataSymbols(IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypes, object metadataDecoder)
            {
                Debug.Assert(anonymousTypes != null);
                Debug.Assert(metadataDecoder != null);

                this.AnonymousTypes = anonymousTypes;
                this.MetadataDecoder = metadataDecoder;
            }
        }

        /// <summary>
        /// Creates an <see cref="EmitBaseline"/> from the metadata of the module before editing
        /// and from a function that maps from a method to an array of local names. 
        /// </summary>
        /// <param name="module">The metadata of the module before editing.</param>
        /// <param name="debugInformationProvider">
        /// A function that for a method handle returns Edit and Continue debug information emitted by the compiler into the PDB.
        /// </param>
        /// <returns>An <see cref="EmitBaseline"/> for the module.</returns>
        /// <remarks>
        /// Only the initial baseline is created using this method; subsequent baselines are created
        /// automatically when emitting the differences in subsequent compilations.
        /// 
        /// When an active method (one for which a frame is allocated on a stack) is updated the values of its local variables need to be preserved.
        /// The mapping of local variable names to their slots in the frame is not included in the metadata and thus needs to be provided by 
        /// <paramref name="debugInformationProvider"/>.
        /// 
        /// The <paramref name="debugInformationProvider"/> is only needed for the initial generation. The mapping for the subsequent generations
        /// is carried over through <see cref="EmitBaseline"/>. The compiler assigns slots to named local variables (including named temporary variables)
        /// it the order in which they appear in the source code. This property allows the compiler to reconstruct the local variable mapping 
        /// for the initial generation. A subsequent generation may add a new variable in between two variables of the previous generation. 
        /// Since the slots of the previous generation variables need to be preserved the only option is to add these new variables to the end.
        /// The slot ordering thus no longer matches the syntax ordering. It is therefore necessary to pass <see cref="EmitDifferenceResult.Baseline"/>
        /// to the next generation (rather than e.g. create new <see cref="EmitBaseline"/>s from scratch based on metadata produced by subsequent compilations).
        /// </remarks>
        public static EmitBaseline CreateInitialBaseline(ModuleMetadata module, Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> debugInformationProvider)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (!module.Module.HasIL)
            {
                throw new ArgumentException(CodeAnalysisResources.PEImageNotAvailable, nameof(module));
            }

            if (debugInformationProvider == null)
            {
                throw new ArgumentNullException(nameof(debugInformationProvider));
            }

            var reader = module.MetadataReader;
            var moduleVersionId = module.GetModuleVersionId();

            return new EmitBaseline(
                null,
                module,
                compilation: null,
                moduleBuilder: null,
                moduleVersionId: moduleVersionId,
                ordinal: 0,
                encId: default(Guid),
                typesAdded: new Dictionary<Cci.ITypeDefinition, int>(),
                eventsAdded: new Dictionary<Cci.IEventDefinition, int>(),
                fieldsAdded: new Dictionary<Cci.IFieldDefinition, int>(),
                methodsAdded: new Dictionary<Cci.IMethodDefinition, int>(),
                propertiesAdded: new Dictionary<Cci.IPropertyDefinition, int>(),
                eventMapAdded: new Dictionary<int, int>(),
                propertyMapAdded: new Dictionary<int, int>(),
                methodImplsAdded: new Dictionary<MethodImplKey, int>(),
                tableEntriesAdded: s_emptyTableSizes,
                blobStreamLengthAdded: 0,
                stringStreamLengthAdded: 0,
                userStringStreamLengthAdded: 0,
                guidStreamLengthAdded: 0,
                anonymousTypeMap: null, // Unset for initial metadata
                synthesizedMembers: ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>>.Empty,
                methodsAddedOrChanged: new Dictionary<int, AddedOrChangedMethodInfo>(),
                debugInformationProvider: debugInformationProvider,
                typeToEventMap: CalculateTypeEventMap(reader),
                typeToPropertyMap: CalculateTypePropertyMap(reader),
                methodImpls: CalculateMethodImpls(reader));
        }

        internal EmitBaseline InitialBaseline { get; }

        /// <summary>
        /// The original metadata of the module.
        /// </summary>
        public ModuleMetadata OriginalMetadata { get; }

        // Symbols hydrated from the original metadata. Lazy since we don't know the language at the time the baseline is constructed.
        internal MetadataSymbols LazyMetadataSymbols;

        internal readonly Compilation Compilation;
        internal readonly CommonPEModuleBuilder PEModuleBuilder;
        internal readonly Guid ModuleVersionId;

        /// <summary>
        /// Metadata generation ordinal. Zero for
        /// full metadata and non-zero for delta.
        /// </summary>
        internal readonly int Ordinal;

        /// <summary>
        /// Unique Guid for this delta, or default(Guid)
        /// if full metadata.
        /// </summary>
        internal readonly Guid EncId;

        internal readonly IReadOnlyDictionary<Cci.ITypeDefinition, int> TypesAdded;
        internal readonly IReadOnlyDictionary<Cci.IEventDefinition, int> EventsAdded;
        internal readonly IReadOnlyDictionary<Cci.IFieldDefinition, int> FieldsAdded;
        internal readonly IReadOnlyDictionary<Cci.IMethodDefinition, int> MethodsAdded;
        internal readonly IReadOnlyDictionary<Cci.IPropertyDefinition, int> PropertiesAdded;
        internal readonly IReadOnlyDictionary<int, int> EventMapAdded;
        internal readonly IReadOnlyDictionary<int, int> PropertyMapAdded;
        internal readonly IReadOnlyDictionary<MethodImplKey, int> MethodImplsAdded;

        internal readonly ImmutableArray<int> TableEntriesAdded;

        internal readonly int BlobStreamLengthAdded;
        internal readonly int StringStreamLengthAdded;
        internal readonly int UserStringStreamLengthAdded;
        internal readonly int GuidStreamLengthAdded;

        /// <summary>
        /// EnC metadata for methods added or updated since the initial generation, indexed by method row id.
        /// </summary>
        internal readonly IReadOnlyDictionary<int, AddedOrChangedMethodInfo> AddedOrChangedMethods;

        /// <summary>
        /// Local variable names for methods from metadata,
        /// indexed by method row.
        /// </summary>
        internal readonly Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> DebugInformationProvider;

        internal readonly ImmutableArray<int> TableSizes;
        internal readonly IReadOnlyDictionary<int, int> TypeToEventMap;
        internal readonly IReadOnlyDictionary<int, int> TypeToPropertyMap;
        internal readonly IReadOnlyDictionary<MethodImplKey, int> MethodImpls;
        private readonly IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> _anonymousTypeMap;
        internal readonly ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> SynthesizedMembers;

        private EmitBaseline(
            EmitBaseline initialBaseline,
            ModuleMetadata module,
            Compilation compilation,
            CommonPEModuleBuilder moduleBuilder,
            Guid moduleVersionId,
            int ordinal,
            Guid encId,
            IReadOnlyDictionary<Cci.ITypeDefinition, int> typesAdded,
            IReadOnlyDictionary<Cci.IEventDefinition, int> eventsAdded,
            IReadOnlyDictionary<Cci.IFieldDefinition, int> fieldsAdded,
            IReadOnlyDictionary<Cci.IMethodDefinition, int> methodsAdded,
            IReadOnlyDictionary<Cci.IPropertyDefinition, int> propertiesAdded,
            IReadOnlyDictionary<int, int> eventMapAdded,
            IReadOnlyDictionary<int, int> propertyMapAdded,
            IReadOnlyDictionary<MethodImplKey, int> methodImplsAdded,
            ImmutableArray<int> tableEntriesAdded,
            int blobStreamLengthAdded,
            int stringStreamLengthAdded,
            int userStringStreamLengthAdded,
            int guidStreamLengthAdded,
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> synthesizedMembers,
            IReadOnlyDictionary<int, AddedOrChangedMethodInfo> methodsAddedOrChanged,
            Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> debugInformationProvider,
            IReadOnlyDictionary<int, int> typeToEventMap,
            IReadOnlyDictionary<int, int> typeToPropertyMap,
            IReadOnlyDictionary<MethodImplKey, int> methodImpls)
        {
            Debug.Assert(module != null);
            Debug.Assert((ordinal == 0) == (encId == default(Guid)));
            Debug.Assert((ordinal == 0) == (initialBaseline == null));
            Debug.Assert(encId != module.GetModuleVersionId());
            Debug.Assert(debugInformationProvider != null);
            Debug.Assert(typeToEventMap != null);
            Debug.Assert(typeToPropertyMap != null);
            Debug.Assert(moduleVersionId != default(Guid));
            Debug.Assert(moduleVersionId == module.GetModuleVersionId());
            Debug.Assert(synthesizedMembers != null);

            Debug.Assert(tableEntriesAdded.Length == MetadataTokens.TableCount);

            // The size of each table is the total number of entries added in all previous
            // generations after the initial generation. Depending on the table, some of the
            // entries may not be available in the current generation (say, a synthesized type
            // from a method that was not recompiled for instance)
            Debug.Assert(tableEntriesAdded[(int)TableIndex.TypeDef] >= typesAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndex.Event] >= eventsAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndex.Field] >= fieldsAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndex.MethodDef] >= methodsAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndex.Property] >= propertiesAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndex.EventMap] >= eventMapAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndex.PropertyMap] >= propertyMapAdded.Count);

            var reader = module.Module.MetadataReader;

            this.InitialBaseline = initialBaseline ?? this;
            this.OriginalMetadata = module;
            this.Compilation = compilation;
            this.PEModuleBuilder = moduleBuilder;
            this.ModuleVersionId = moduleVersionId;
            this.Ordinal = ordinal;
            this.EncId = encId;

            this.TypesAdded = typesAdded;
            this.EventsAdded = eventsAdded;
            this.FieldsAdded = fieldsAdded;
            this.MethodsAdded = methodsAdded;
            this.PropertiesAdded = propertiesAdded;
            this.EventMapAdded = eventMapAdded;
            this.PropertyMapAdded = propertyMapAdded;
            this.MethodImplsAdded = methodImplsAdded;
            this.TableEntriesAdded = tableEntriesAdded;
            this.BlobStreamLengthAdded = blobStreamLengthAdded;
            this.StringStreamLengthAdded = stringStreamLengthAdded;
            this.UserStringStreamLengthAdded = userStringStreamLengthAdded;
            this.GuidStreamLengthAdded = guidStreamLengthAdded;
            _anonymousTypeMap = anonymousTypeMap;
            this.SynthesizedMembers = synthesizedMembers;
            this.AddedOrChangedMethods = methodsAddedOrChanged;

            this.DebugInformationProvider = debugInformationProvider;
            this.TableSizes = CalculateTableSizes(reader, this.TableEntriesAdded);
            this.TypeToEventMap = typeToEventMap;
            this.TypeToPropertyMap = typeToPropertyMap;
            this.MethodImpls = methodImpls;
        }

        internal EmitBaseline With(
            Compilation compilation,
            CommonPEModuleBuilder moduleBuilder,
            int ordinal,
            Guid encId,
            IReadOnlyDictionary<Cci.ITypeDefinition, int> typesAdded,
            IReadOnlyDictionary<Cci.IEventDefinition, int> eventsAdded,
            IReadOnlyDictionary<Cci.IFieldDefinition, int> fieldsAdded,
            IReadOnlyDictionary<Cci.IMethodDefinition, int> methodsAdded,
            IReadOnlyDictionary<Cci.IPropertyDefinition, int> propertiesAdded,
            IReadOnlyDictionary<int, int> eventMapAdded,
            IReadOnlyDictionary<int, int> propertyMapAdded,
            IReadOnlyDictionary<MethodImplKey, int> methodImplsAdded,
            ImmutableArray<int> tableEntriesAdded,
            int blobStreamLengthAdded,
            int stringStreamLengthAdded,
            int userStringStreamLengthAdded,
            int guidStreamLengthAdded,
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> synthesizedMembers,
            IReadOnlyDictionary<int, AddedOrChangedMethodInfo> addedOrChangedMethods,
            Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> debugInformationProvider)
        {
            Debug.Assert(_anonymousTypeMap == null || anonymousTypeMap != null);
            Debug.Assert(_anonymousTypeMap == null || anonymousTypeMap.Count >= _anonymousTypeMap.Count);

            return new EmitBaseline(
                this.InitialBaseline,
                this.OriginalMetadata,
                compilation,
                moduleBuilder,
                this.ModuleVersionId,
                ordinal,
                encId,
                typesAdded,
                eventsAdded,
                fieldsAdded,
                methodsAdded,
                propertiesAdded,
                eventMapAdded,
                propertyMapAdded,
                methodImplsAdded,
                tableEntriesAdded,
                blobStreamLengthAdded: blobStreamLengthAdded,
                stringStreamLengthAdded: stringStreamLengthAdded,
                userStringStreamLengthAdded: userStringStreamLengthAdded,
                guidStreamLengthAdded: guidStreamLengthAdded,
                anonymousTypeMap: anonymousTypeMap,
                synthesizedMembers: synthesizedMembers,
                methodsAddedOrChanged: addedOrChangedMethods,
                debugInformationProvider: debugInformationProvider,
                typeToEventMap: this.TypeToEventMap,
                typeToPropertyMap: this.TypeToPropertyMap,
                methodImpls: this.MethodImpls);
        }

        internal IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> AnonymousTypeMap
        {
            get
            {
                if (Ordinal > 0)
                {
                    return _anonymousTypeMap;
                }

                Debug.Assert(LazyMetadataSymbols != null);
                return LazyMetadataSymbols.AnonymousTypes;
            }
        }

        internal MetadataReader MetadataReader
        {
            get { return this.OriginalMetadata.MetadataReader; }
        }

        internal int BlobStreamLength
        {
            get { return this.BlobStreamLengthAdded + this.MetadataReader.GetHeapSize(HeapIndex.Blob); }
        }

        internal int StringStreamLength
        {
            get { return this.StringStreamLengthAdded + this.MetadataReader.GetHeapSize(HeapIndex.String); }
        }

        internal int UserStringStreamLength
        {
            get { return this.UserStringStreamLengthAdded + this.MetadataReader.GetHeapSize(HeapIndex.UserString); }
        }

        internal int GuidStreamLength
        {
            get { return this.GuidStreamLengthAdded + this.MetadataReader.GetHeapSize(HeapIndex.Guid); }
        }

        private static ImmutableArray<int> CalculateTableSizes(MetadataReader reader, ImmutableArray<int> delta)
        {
            var sizes = new int[MetadataTokens.TableCount];

            for (int i = 0; i < sizes.Length; i++)
            {
                sizes[i] = reader.GetTableRowCount((TableIndex)i) + delta[i];
            }

            return ImmutableArray.Create(sizes);
        }

        private static Dictionary<int, int> CalculateTypePropertyMap(MetadataReader reader)
        {
            var result = new Dictionary<int, int>();

            int rowId = 1;
            foreach (var parentType in reader.GetTypesWithProperties())
            {
                Debug.Assert(!parentType.IsNil);
                result.Add(reader.GetRowNumber(parentType), rowId);
                rowId++;
            }

            return result;
        }

        private static Dictionary<int, int> CalculateTypeEventMap(MetadataReader reader)
        {
            var result = new Dictionary<int, int>();

            int rowId = 1;
            foreach (var parentType in reader.GetTypesWithEvents())
            {
                Debug.Assert(!parentType.IsNil);
                result.Add(reader.GetRowNumber(parentType), rowId);
                rowId++;
            }

            return result;
        }

        private static Dictionary<MethodImplKey, int> CalculateMethodImpls(MetadataReader reader)
        {
            var result = new Dictionary<MethodImplKey, int>();
            int n = reader.GetTableRowCount(TableIndex.MethodImpl);
            for (int row = 1; row <= n; row++)
            {
                var methodImpl = reader.GetMethodImplementation(MetadataTokens.MethodImplementationHandle(row));
                // Hold on to the implementing method def but use a simple
                // index for the implemented method ref token. (We do not map
                // member refs currently, and since we don't allow changes to
                // the set of methods a method def implements, the actual
                // tokens of the implemented methods are not needed.)
                int methodDefRow = MetadataTokens.GetRowNumber(methodImpl.MethodBody);
                int index = 1;
                while (true)
                {
                    var key = new MethodImplKey(methodDefRow, index);
                    if (!result.ContainsKey(key))
                    {
                        result.Add(key, row);
                        break;
                    }
                    index++;
                }
            }
            return result;
        }

        internal int GetNextAnonymousTypeIndex(bool fromDelegates = false)
        {
            int nextIndex = 0;
            foreach (var pair in this.AnonymousTypeMap)
            {
                if (fromDelegates != pair.Key.IsDelegate)
                {
                    continue;
                }
                int index = pair.Value.UniqueIndex;
                if (index >= nextIndex)
                {
                    nextIndex = index + 1;
                }
            }

            return nextIndex;
        }
    }
}
