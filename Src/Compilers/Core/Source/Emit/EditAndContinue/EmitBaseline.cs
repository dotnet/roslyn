// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    public delegate ImmutableArray<string> LocalVariableNameProvider(uint methodIndex);

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
        internal MethodImplKey(uint implementingMethod, int index)
        {
            Debug.Assert(implementingMethod > 0);
            Debug.Assert(index > 0);
            this.ImplementingMethod = implementingMethod;
            this.Index = index;
        }

        internal readonly uint ImplementingMethod;
        internal readonly int Index;

        public bool Equals(MethodImplKey other)
        {
            return this.ImplementingMethod == other.ImplementingMethod &&
                this.Index == other.Index;
        }

        public override int GetHashCode()
        {
            return Hash.Combine((int)this.ImplementingMethod, this.Index);
        }
    }

    /// <summary>
    /// Represents a module from a previous compilation. Used in Edit and Continue
    /// to emit the differences in a subsequent compilation.
    /// </summary>
    public sealed class EmitBaseline
    {
        private static readonly ImmutableArray<int> EmptyTableSizes = ImmutableArray.Create(new int[MetadataTokens.TableCount]);

        /// <summary>
        /// Creates an <see cref="EmitBaseline"/> from the metadata of the module before editing
        /// and from a function that maps from a method to an array of local names. 
        /// </summary>
        /// <param name="module">The metadata of the module before editing.</param>
        /// <param name="localNames">
        /// A function that returns the array of local names given a method index from the module metadata.
        /// </param>
        /// <returns>An <see cref="EmitBaseline"/> for the module.</returns>
        /// <remarks>
        /// Only the initial baseline is created using this method; subsequent baselines are created
        /// automatically when emitting the differences in subsequent compilations.
        /// 
        /// When an active method (one for which a frame is allocated on a stack) is updated the values of its local variables need to be preserved.
        /// The mapping of local variable names to their slots in the frame is not included in the metadata and thus needs to be provided by 
        /// <paramref name="localNames"/>.
        /// 
        /// The <see cref="LocalVariableNameProvider"/> is only needed for the initial generation. The mapping for the subsequent generations
        /// is carried over through <see cref="EmitBaseline"/>. The compiler assigns slots to named local variables (including named temporary variables)
        /// it the order in which they appear in the source code. This property allows the compiler to reconstruct the local variable mapping 
        /// for the initial generation. A subsequent generation may add a new variable in between two variables of the previous generation. 
        /// Since the slots of the previous generation variables need to be preserved the only option is to add these new variables to the end.
        /// The slot ordering thus no longer matches the syntax ordering. It is therefore necessary to pass <see cref="EmitDifferenceResult.Baseline"/>
        /// to the next generation (rather than e.g. create new <see cref="EmitBaseline"/>s from scratch based on metadata produced by subsequent compilations).
        /// </remarks>
        public static EmitBaseline CreateInitialBaseline(ModuleMetadata module, LocalVariableNameProvider localNames)
        {
            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            if (!module.Module.HasIL)
            {
                throw new ArgumentException(CodeAnalysisResources.PEImageNotAvailable, "module");
            }

            if (localNames == null)
            {
                throw new ArgumentNullException("localNames");
            }

            var reader = module.MetadataReader;
            var moduleVersionId = module.GetModuleVersionId();

            return new EmitBaseline(
                module,
                compilation: null,
                moduleBuilder: null,
                moduleVersionId: moduleVersionId,
                ordinal: 0,
                encId: default(Guid),
                typesAdded: new Dictionary<ITypeDefinition, uint>(),
                eventsAdded: new Dictionary<IEventDefinition, uint>(),
                fieldsAdded: new Dictionary<IFieldDefinition, uint>(),
                methodsAdded: new Dictionary<IMethodDefinition, uint>(),
                propertiesAdded: new Dictionary<IPropertyDefinition, uint>(),
                eventMapAdded: new Dictionary<uint, uint>(),
                propertyMapAdded: new Dictionary<uint, uint>(),
                methodImplsAdded: new Dictionary<MethodImplKey, uint>(),
                tableEntriesAdded: EmptyTableSizes,
                blobStreamLengthAdded: 0,
                stringStreamLengthAdded: 0,
                userStringStreamLengthAdded: 0,
                guidStreamLengthAdded: 0,
                anonymousTypeMap: null, // Unset for initial metadata
                localsForMethodsAddedOrChanged: new Dictionary<uint, ImmutableArray<EncLocalInfo>>(),
                localNames: localNames,
                typeToEventMap: reader.CalculateTypeEventMap(),
                typeToPropertyMap: reader.CalculateTypePropertyMap(),
                methodImpls: CalculateMethodImpls(reader));
        }

        /// <summary>
        /// The original metadata of the module.
        /// </summary>
        public readonly ModuleMetadata OriginalMetadata;

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

        internal readonly IReadOnlyDictionary<ITypeDefinition, uint> TypesAdded;
        internal readonly IReadOnlyDictionary<IEventDefinition, uint> EventsAdded;
        internal readonly IReadOnlyDictionary<IFieldDefinition, uint> FieldsAdded;
        internal readonly IReadOnlyDictionary<IMethodDefinition, uint> MethodsAdded;
        internal readonly IReadOnlyDictionary<IPropertyDefinition, uint> PropertiesAdded;
        internal readonly IReadOnlyDictionary<uint, uint> EventMapAdded;
        internal readonly IReadOnlyDictionary<uint, uint> PropertyMapAdded;
        internal readonly IReadOnlyDictionary<MethodImplKey, uint> MethodImplsAdded;

        internal readonly ImmutableArray<int> TableEntriesAdded;

        internal readonly int BlobStreamLengthAdded;
        internal readonly int StringStreamLengthAdded;
        internal readonly int UserStringStreamLengthAdded;
        internal readonly int GuidStreamLengthAdded;

        /// <summary>
        /// Map from syntax to local variable for methods added or updated
        /// since the initial generation, indexed by method row.
        /// </summary>
        internal readonly IReadOnlyDictionary<uint, ImmutableArray<EncLocalInfo>> LocalsForMethodsAddedOrChanged;

        /// <summary>
        /// Local variable names for methods from metadata,
        /// indexed by method row.
        /// </summary>
        internal readonly LocalVariableNameProvider LocalNames;

        internal readonly ImmutableArray<int> TableSizes;
        internal readonly IReadOnlyDictionary<uint, uint> TypeToEventMap;
        internal readonly IReadOnlyDictionary<uint, uint> TypeToPropertyMap;
        internal readonly IReadOnlyDictionary<MethodImplKey, uint> MethodImpls;
        internal readonly IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> AnonymousTypeMap;

        private EmitBaseline(
            ModuleMetadata module,
            Compilation compilation,
            CommonPEModuleBuilder moduleBuilder,
            Guid moduleVersionId,
            int ordinal,
            Guid encId,
            IReadOnlyDictionary<ITypeDefinition, uint> typesAdded,
            IReadOnlyDictionary<IEventDefinition, uint> eventsAdded,
            IReadOnlyDictionary<IFieldDefinition, uint> fieldsAdded,
            IReadOnlyDictionary<IMethodDefinition, uint> methodsAdded,
            IReadOnlyDictionary<IPropertyDefinition, uint> propertiesAdded,
            IReadOnlyDictionary<uint, uint> eventMapAdded,
            IReadOnlyDictionary<uint, uint> propertyMapAdded,
            IReadOnlyDictionary<MethodImplKey, uint> methodImplsAdded,
            ImmutableArray<int> tableEntriesAdded,
            int blobStreamLengthAdded,
            int stringStreamLengthAdded,
            int userStringStreamLengthAdded,
            int guidStreamLengthAdded,
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            IReadOnlyDictionary<uint, ImmutableArray<EncLocalInfo>> localsForMethodsAddedOrChanged,
            LocalVariableNameProvider localNames,
            IReadOnlyDictionary<uint, uint> typeToEventMap,
            IReadOnlyDictionary<uint, uint> typeToPropertyMap,
            IReadOnlyDictionary<MethodImplKey, uint> methodImpls)
        {
            Debug.Assert(module != null);
            Debug.Assert((ordinal == 0) == (encId == default(Guid)));
            Debug.Assert(encId != module.GetModuleVersionId());
            Debug.Assert(localNames != null);
            Debug.Assert(typeToEventMap != null);
            Debug.Assert(typeToPropertyMap != null);
            Debug.Assert(moduleVersionId != default(Guid));
            Debug.Assert(moduleVersionId == module.GetModuleVersionId());

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
            this.AnonymousTypeMap = anonymousTypeMap;
            this.LocalsForMethodsAddedOrChanged = localsForMethodsAddedOrChanged;

            this.LocalNames = localNames;
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
            IReadOnlyDictionary<ITypeDefinition, uint> typesAdded,
            IReadOnlyDictionary<IEventDefinition, uint> eventsAdded,
            IReadOnlyDictionary<IFieldDefinition, uint> fieldsAdded,
            IReadOnlyDictionary<IMethodDefinition, uint> methodsAdded,
            IReadOnlyDictionary<IPropertyDefinition, uint> propertiesAdded,
            IReadOnlyDictionary<uint, uint> eventMapAdded,
            IReadOnlyDictionary<uint, uint> propertyMapAdded,
            IReadOnlyDictionary<MethodImplKey, uint> methodImplsAdded,
            ImmutableArray<int> tableEntriesAdded,
            int blobStreamLengthAdded,
            int stringStreamLengthAdded,
            int userStringStreamLengthAdded,
            int guidStreamLengthAdded,
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            IReadOnlyDictionary<uint, ImmutableArray<EncLocalInfo>> localsForMethodsAddedOrChanged,
            LocalVariableNameProvider localNames)
        {
            Debug.Assert((this.AnonymousTypeMap == null) || (anonymousTypeMap != null));
            Debug.Assert((this.AnonymousTypeMap == null) || (anonymousTypeMap.Count >= this.AnonymousTypeMap.Count));

            return new EmitBaseline(
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
                localsForMethodsAddedOrChanged: localsForMethodsAddedOrChanged,
                localNames: localNames,
                typeToEventMap: this.TypeToEventMap,
                typeToPropertyMap: this.TypeToPropertyMap,
                methodImpls: this.MethodImpls);
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

        private static Dictionary<MethodImplKey, uint> CalculateMethodImpls(MetadataReader reader)
        {
            var result = new Dictionary<MethodImplKey, uint>();
            int n = reader.GetTableRowCount(TableIndex.MethodImpl);
            for (int row = 1; row <= n; row++)
            {
                var methodImpl = reader.GetMethodImplementation(MetadataTokens.MethodImplementationHandle(row));
                // Hold on to the implementing method def but use a simple
                // index for the implemented method ref token. (We do not map
                // member refs currently, and since we don't allow changes to
                // the set of methods a method def implements, the actual
                // tokens of the implemented methods are not needed.)
                var methodDefRow = (uint)MetadataTokens.GetRowNumber(methodImpl.MethodBody);
                int index = 1;
                while (true)
                {
                    var key = new MethodImplKey(methodDefRow, index);
                    if (!result.ContainsKey(key))
                    {
                        result.Add(key, (uint)row);
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
