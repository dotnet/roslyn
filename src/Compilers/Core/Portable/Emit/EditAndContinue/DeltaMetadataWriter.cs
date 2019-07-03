// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class DeltaMetadataWriter : MetadataWriter
    {
        private readonly EmitBaseline _previousGeneration;
        private readonly Guid _encId;
        private readonly DefinitionMap _definitionMap;
        private readonly SymbolChanges _changes;

        private readonly DefinitionIndex<ITypeDefinition> _typeDefs;
        private readonly DefinitionIndex<IEventDefinition> _eventDefs;
        private readonly DefinitionIndex<IFieldDefinition> _fieldDefs;
        private readonly DefinitionIndex<IMethodDefinition> _methodDefs;
        private readonly DefinitionIndex<IPropertyDefinition> _propertyDefs;
        private readonly ParameterDefinitionIndex _parameterDefs;
        private readonly List<KeyValuePair<IMethodDefinition, IParameterDefinition>> _parameterDefList;
        private readonly GenericParameterIndex _genericParameters;
        private readonly EventOrPropertyMapIndex _eventMap;
        private readonly EventOrPropertyMapIndex _propertyMap;
        private readonly MethodImplIndex _methodImpls;

        private readonly HeapOrReferenceIndex<AssemblyIdentity> _assemblyRefIndex;
        private readonly HeapOrReferenceIndex<string> _moduleRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeMemberReference> _memberRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference> _methodSpecIndex;
        private readonly HeapOrReferenceIndex<ITypeReference> _typeRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeReference> _typeSpecIndex;
        private readonly HeapOrReferenceIndex<BlobHandle> _standAloneSignatureIndex;
        private readonly Dictionary<IMethodDefinition, AddedOrChangedMethodInfo> _addedOrChangedMethods;

        public DeltaMetadataWriter(
            EmitContext context,
            CommonMessageProvider messageProvider,
            EmitBaseline previousGeneration,
            Guid encId,
            DefinitionMap definitionMap,
            SymbolChanges changes,
            CancellationToken cancellationToken)
            : base(metadata: MakeTablesBuilder(previousGeneration),
                   debugMetadataOpt: (context.Module.DebugInformationFormat == DebugInformationFormat.PortablePdb) ? new MetadataBuilder() : null,
                   dynamicAnalysisDataWriterOpt: null,
                   context: context,
                   messageProvider: messageProvider,
                   metadataOnly: false,
                   deterministic: false,
                   emitTestCoverageData: false,
                   cancellationToken: cancellationToken)
        {
            Debug.Assert(previousGeneration != null);
            Debug.Assert(encId != default(Guid));
            Debug.Assert(encId != previousGeneration.EncId);
            Debug.Assert(context.Module.DebugInformationFormat != DebugInformationFormat.Embedded);

            _previousGeneration = previousGeneration;
            _encId = encId;
            _definitionMap = definitionMap;
            _changes = changes;

            var sizes = previousGeneration.TableSizes;

            _typeDefs = new DefinitionIndex<ITypeDefinition>(this.TryGetExistingTypeDefIndex, sizes[(int)TableIndex.TypeDef]);
            _eventDefs = new DefinitionIndex<IEventDefinition>(this.TryGetExistingEventDefIndex, sizes[(int)TableIndex.Event]);
            _fieldDefs = new DefinitionIndex<IFieldDefinition>(this.TryGetExistingFieldDefIndex, sizes[(int)TableIndex.Field]);
            _methodDefs = new DefinitionIndex<IMethodDefinition>(this.TryGetExistingMethodDefIndex, sizes[(int)TableIndex.MethodDef]);
            _propertyDefs = new DefinitionIndex<IPropertyDefinition>(this.TryGetExistingPropertyDefIndex, sizes[(int)TableIndex.Property]);
            _parameterDefs = new ParameterDefinitionIndex(sizes[(int)TableIndex.Param]);
            _parameterDefList = new List<KeyValuePair<IMethodDefinition, IParameterDefinition>>();
            _genericParameters = new GenericParameterIndex(sizes[(int)TableIndex.GenericParam]);
            _eventMap = new EventOrPropertyMapIndex(this.TryGetExistingEventMapIndex, sizes[(int)TableIndex.EventMap]);
            _propertyMap = new EventOrPropertyMapIndex(this.TryGetExistingPropertyMapIndex, sizes[(int)TableIndex.PropertyMap]);
            _methodImpls = new MethodImplIndex(this, sizes[(int)TableIndex.MethodImpl]);

            _assemblyRefIndex = new HeapOrReferenceIndex<AssemblyIdentity>(this, lastRowId: sizes[(int)TableIndex.AssemblyRef]);
            _moduleRefIndex = new HeapOrReferenceIndex<string>(this, lastRowId: sizes[(int)TableIndex.ModuleRef]);
            _memberRefIndex = new InstanceAndStructuralReferenceIndex<ITypeMemberReference>(this, new MemberRefComparer(this), lastRowId: sizes[(int)TableIndex.MemberRef]);
            _methodSpecIndex = new InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference>(this, new MethodSpecComparer(this), lastRowId: sizes[(int)TableIndex.MethodSpec]);
            _typeRefIndex = new HeapOrReferenceIndex<ITypeReference>(this, lastRowId: sizes[(int)TableIndex.TypeRef]);
            _typeSpecIndex = new InstanceAndStructuralReferenceIndex<ITypeReference>(this, new TypeSpecComparer(this), lastRowId: sizes[(int)TableIndex.TypeSpec]);
            _standAloneSignatureIndex = new HeapOrReferenceIndex<BlobHandle>(this, lastRowId: sizes[(int)TableIndex.StandAloneSig]);

            _addedOrChangedMethods = new Dictionary<IMethodDefinition, AddedOrChangedMethodInfo>();
        }

        private static MetadataBuilder MakeTablesBuilder(EmitBaseline previousGeneration)
        {
            return new MetadataBuilder(
                previousGeneration.UserStringStreamLength,
                previousGeneration.StringStreamLength,
                previousGeneration.BlobStreamLength,
                previousGeneration.GuidStreamLength);
        }

        private ImmutableArray<int> GetDeltaTableSizes(ImmutableArray<int> rowCounts)
        {
            var sizes = new int[MetadataTokens.TableCount];

            rowCounts.CopyTo(sizes);

            sizes[(int)TableIndex.TypeRef] = _typeRefIndex.Rows.Count;
            sizes[(int)TableIndex.TypeDef] = _typeDefs.GetAdded().Count;
            sizes[(int)TableIndex.Field] = _fieldDefs.GetAdded().Count;
            sizes[(int)TableIndex.MethodDef] = _methodDefs.GetAdded().Count;
            sizes[(int)TableIndex.Param] = _parameterDefs.GetAdded().Count;
            sizes[(int)TableIndex.MemberRef] = _memberRefIndex.Rows.Count;
            sizes[(int)TableIndex.StandAloneSig] = _standAloneSignatureIndex.Rows.Count;
            sizes[(int)TableIndex.EventMap] = _eventMap.GetAdded().Count;
            sizes[(int)TableIndex.Event] = _eventDefs.GetAdded().Count;
            sizes[(int)TableIndex.PropertyMap] = _propertyMap.GetAdded().Count;
            sizes[(int)TableIndex.Property] = _propertyDefs.GetAdded().Count;
            sizes[(int)TableIndex.MethodImpl] = _methodImpls.GetAdded().Count;
            sizes[(int)TableIndex.ModuleRef] = _moduleRefIndex.Rows.Count;
            sizes[(int)TableIndex.TypeSpec] = _typeSpecIndex.Rows.Count;
            sizes[(int)TableIndex.AssemblyRef] = _assemblyRefIndex.Rows.Count;
            sizes[(int)TableIndex.GenericParam] = _genericParameters.GetAdded().Count;
            sizes[(int)TableIndex.MethodSpec] = _methodSpecIndex.Rows.Count;

            return ImmutableArray.Create(sizes);
        }

        internal EmitBaseline GetDelta(EmitBaseline baseline, Compilation compilation, Guid encId, MetadataSizes metadataSizes)
        {
            var addedOrChangedMethodsByIndex = new Dictionary<int, AddedOrChangedMethodInfo>();
            foreach (var pair in _addedOrChangedMethods)
            {
                addedOrChangedMethodsByIndex.Add(MetadataTokens.GetRowNumber(GetMethodDefinitionHandle(pair.Key)), pair.Value);
            }

            var previousTableSizes = _previousGeneration.TableEntriesAdded;
            var deltaTableSizes = GetDeltaTableSizes(metadataSizes.RowCounts);
            var tableSizes = new int[MetadataTokens.TableCount];

            for (int i = 0; i < tableSizes.Length; i++)
            {
                tableSizes[i] = previousTableSizes[i] + deltaTableSizes[i];
            }

            // If the previous generation is 0 (metadata) get the synthesized members from the current compilation's builder,
            // otherwise members from the current compilation have already been merged into the baseline.
            var synthesizedMembers = (baseline.Ordinal == 0) ? module.GetAllSynthesizedMembers() : baseline.SynthesizedMembers;

            return baseline.With(
                compilation,
                module,
                baseline.Ordinal + 1,
                encId,
                typesAdded: AddRange(_previousGeneration.TypesAdded, _typeDefs.GetAdded()),
                eventsAdded: AddRange(_previousGeneration.EventsAdded, _eventDefs.GetAdded()),
                fieldsAdded: AddRange(_previousGeneration.FieldsAdded, _fieldDefs.GetAdded()),
                methodsAdded: AddRange(_previousGeneration.MethodsAdded, _methodDefs.GetAdded()),
                propertiesAdded: AddRange(_previousGeneration.PropertiesAdded, _propertyDefs.GetAdded()),
                eventMapAdded: AddRange(_previousGeneration.EventMapAdded, _eventMap.GetAdded()),
                propertyMapAdded: AddRange(_previousGeneration.PropertyMapAdded, _propertyMap.GetAdded()),
                methodImplsAdded: AddRange(_previousGeneration.MethodImplsAdded, _methodImpls.GetAdded()),
                tableEntriesAdded: ImmutableArray.Create(tableSizes),
                // Blob stream is concatenated aligned.
                blobStreamLengthAdded: metadataSizes.GetAlignedHeapSize(HeapIndex.Blob) + _previousGeneration.BlobStreamLengthAdded,
                // String stream is concatenated unaligned.
                stringStreamLengthAdded: metadataSizes.HeapSizes[(int)HeapIndex.String] + _previousGeneration.StringStreamLengthAdded,
                // UserString stream is concatenated aligned.
                userStringStreamLengthAdded: metadataSizes.GetAlignedHeapSize(HeapIndex.UserString) + _previousGeneration.UserStringStreamLengthAdded,
                // Guid stream accumulates on the GUID heap unlike other heaps, so the previous generations are already included.
                guidStreamLengthAdded: metadataSizes.HeapSizes[(int)HeapIndex.Guid],
                anonymousTypeMap: ((IPEDeltaAssemblyBuilder)module).GetAnonymousTypeMap(),
                synthesizedMembers: synthesizedMembers,
                addedOrChangedMethods: AddRange(_previousGeneration.AddedOrChangedMethods, addedOrChangedMethodsByIndex, replace: true),
                debugInformationProvider: baseline.DebugInformationProvider,
                localSignatureProvider: baseline.LocalSignatureProvider);
        }

        private static IReadOnlyDictionary<K, V> AddRange<K, V>(IReadOnlyDictionary<K, V> previous, IReadOnlyDictionary<K, V> current, bool replace = false)
        {
            if (previous.Count == 0)
            {
                return current;
            }

            if (current.Count == 0)
            {
                return previous;
            }

            var result = new Dictionary<K, V>();
            foreach (var pair in previous)
            {
                result.Add(pair.Key, pair.Value);
            }

            foreach (var pair in current)
            {
                Debug.Assert(replace || !previous.ContainsKey(pair.Key));
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        /// <summary>
        /// Return tokens for all modified debuggable methods.
        /// </summary>
        public void GetMethodTokens(ICollection<MethodDefinitionHandle> methods)
        {
            foreach (var def in _methodDefs.GetRows())
            {
                // The debugger tries to remap all modified methods, which requires presence of sequence points.
                if (!_methodDefs.IsAddedNotChanged(def) && def.GetBody(Context)?.SequencePoints.Length > 0)
                {
                    methods.Add(MetadataTokens.MethodDefinitionHandle(_methodDefs[def]));
                }
            }
        }

        protected override ushort Generation
        {
            get { return (ushort)(_previousGeneration.Ordinal + 1); }
        }

        protected override Guid EncId
        {
            get { return _encId; }
        }

        protected override Guid EncBaseId
        {
            get { return _previousGeneration.EncId; }
        }

        protected override EventDefinitionHandle GetEventDefinitionHandle(IEventDefinition def)
        {
            return MetadataTokens.EventDefinitionHandle(_eventDefs[def]);
        }

        protected override IReadOnlyList<IEventDefinition> GetEventDefs()
        {
            return _eventDefs.GetRows();
        }

        protected override FieldDefinitionHandle GetFieldDefinitionHandle(IFieldDefinition def)
        {
            return MetadataTokens.FieldDefinitionHandle(_fieldDefs[def]);
        }

        protected override IReadOnlyList<IFieldDefinition> GetFieldDefs()
        {
            return _fieldDefs.GetRows();
        }

        protected override bool TryGetTypeDefinitionHandle(ITypeDefinition def, out TypeDefinitionHandle handle)
        {
            int index;
            bool result = _typeDefs.TryGetValue(def, out index);
            handle = MetadataTokens.TypeDefinitionHandle(index);
            return result;
        }

        protected override TypeDefinitionHandle GetTypeDefinitionHandle(ITypeDefinition def)
        {
            return MetadataTokens.TypeDefinitionHandle(_typeDefs[def]);
        }

        protected override ITypeDefinition GetTypeDef(TypeDefinitionHandle handle)
        {
            return _typeDefs[MetadataTokens.GetRowNumber(handle)];
        }

        protected override IReadOnlyList<ITypeDefinition> GetTypeDefs()
        {
            return _typeDefs.GetRows();
        }

        protected override bool TryGetMethodDefinitionHandle(IMethodDefinition def, out MethodDefinitionHandle handle)
        {
            int index;
            bool result = _methodDefs.TryGetValue(def, out index);
            handle = MetadataTokens.MethodDefinitionHandle(index);
            return result;
        }

        protected override MethodDefinitionHandle GetMethodDefinitionHandle(IMethodDefinition def)
        {
            return MetadataTokens.MethodDefinitionHandle(_methodDefs[def]);
        }

        protected override IMethodDefinition GetMethodDef(MethodDefinitionHandle index)
        {
            return _methodDefs[MetadataTokens.GetRowNumber(index)];
        }

        protected override IReadOnlyList<IMethodDefinition> GetMethodDefs()
        {
            return _methodDefs.GetRows();
        }

        protected override PropertyDefinitionHandle GetPropertyDefIndex(IPropertyDefinition def)
        {
            return MetadataTokens.PropertyDefinitionHandle(_propertyDefs[def]);
        }

        protected override IReadOnlyList<IPropertyDefinition> GetPropertyDefs()
        {
            return _propertyDefs.GetRows();
        }

        protected override ParameterHandle GetParameterHandle(IParameterDefinition def)
        {
            return MetadataTokens.ParameterHandle(_parameterDefs[def]);
        }

        protected override IReadOnlyList<IParameterDefinition> GetParameterDefs()
        {
            return _parameterDefs.GetRows();
        }

        protected override IReadOnlyList<IGenericParameter> GetGenericParameters()
        {
            return _genericParameters.GetRows();
        }

        protected override FieldDefinitionHandle GetFirstFieldDefinitionHandle(INamedTypeDefinition typeDef)
        {
            // Fields are associated with the
            // type through the EncLog table.
            return default(FieldDefinitionHandle);
        }

        protected override MethodDefinitionHandle GetFirstMethodDefinitionHandle(INamedTypeDefinition typeDef)
        {
            // Methods are associated with the
            // type through the EncLog table.
            return default(MethodDefinitionHandle);
        }

        protected override ParameterHandle GetFirstParameterHandle(IMethodDefinition methodDef)
        {
            // Parameters are associated with the
            // method through the EncLog table.
            return default(ParameterHandle);
        }

        protected override AssemblyReferenceHandle GetOrAddAssemblyReferenceHandle(IAssemblyReference reference)
        {
            var identity = reference.Identity;
            var versionPattern = reference.AssemblyVersionPattern;

            if ((object)versionPattern != null)
            {
                identity = _previousGeneration.InitialBaseline.LazyMetadataSymbols.AssemblyReferenceIdentityMap[identity.WithVersion(versionPattern)];
            }

            return MetadataTokens.AssemblyReferenceHandle(_assemblyRefIndex.GetOrAdd(identity));
        }

        protected override IReadOnlyList<AssemblyIdentity> GetAssemblyRefs()
        {
            return _assemblyRefIndex.Rows;
        }

        protected override ModuleReferenceHandle GetOrAddModuleReferenceHandle(string reference)
        {
            return MetadataTokens.ModuleReferenceHandle(_moduleRefIndex.GetOrAdd(reference));
        }

        protected override IReadOnlyList<string> GetModuleRefs()
        {
            return _moduleRefIndex.Rows;
        }

        protected override MemberReferenceHandle GetOrAddMemberReferenceHandle(ITypeMemberReference reference)
        {
            return MetadataTokens.MemberReferenceHandle(_memberRefIndex.GetOrAdd(reference));
        }

        protected override IReadOnlyList<ITypeMemberReference> GetMemberRefs()
        {
            return _memberRefIndex.Rows;
        }

        protected override MethodSpecificationHandle GetOrAddMethodSpecificationHandle(IGenericMethodInstanceReference reference)
        {
            return MetadataTokens.MethodSpecificationHandle(_methodSpecIndex.GetOrAdd(reference));
        }

        protected override IReadOnlyList<IGenericMethodInstanceReference> GetMethodSpecs()
        {
            return _methodSpecIndex.Rows;
        }

        protected override int GreatestMethodDefIndex => _methodDefs.NextRowId;

        protected override bool TryGetTypeReferenceHandle(ITypeReference reference, out TypeReferenceHandle handle)
        {
            int index;
            bool result = _typeRefIndex.TryGetValue(reference, out index);
            handle = MetadataTokens.TypeReferenceHandle(index);
            return result;
        }

        protected override TypeReferenceHandle GetOrAddTypeReferenceHandle(ITypeReference reference)
        {
            return MetadataTokens.TypeReferenceHandle(_typeRefIndex.GetOrAdd(reference));
        }

        protected override IReadOnlyList<ITypeReference> GetTypeRefs()
        {
            return _typeRefIndex.Rows;
        }

        protected override TypeSpecificationHandle GetOrAddTypeSpecificationHandle(ITypeReference reference)
        {
            return MetadataTokens.TypeSpecificationHandle(_typeSpecIndex.GetOrAdd(reference));
        }

        protected override IReadOnlyList<ITypeReference> GetTypeSpecs()
        {
            return _typeSpecIndex.Rows;
        }

        protected override StandaloneSignatureHandle GetOrAddStandaloneSignatureHandle(BlobHandle blobIndex)
        {
            return MetadataTokens.StandaloneSignatureHandle(_standAloneSignatureIndex.GetOrAdd(blobIndex));
        }

        protected override IReadOnlyList<BlobHandle> GetStandaloneSignatureBlobHandles()
        {
            return _standAloneSignatureIndex.Rows;
        }

        protected override void OnIndicesCreated()
        {
            var module = (IPEDeltaAssemblyBuilder)this.module;
            module.OnCreatedIndices(this.Context.Diagnostics);
        }

        protected override void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef)
        {
            var change = _changes.GetChange(typeDef);
            switch (change)
            {
                case SymbolChange.Added:
                    _typeDefs.Add(typeDef);
                    var typeParameters = this.GetConsolidatedTypeParameters(typeDef);
                    if (typeParameters != null)
                    {
                        foreach (var typeParameter in typeParameters)
                        {
                            _genericParameters.Add(typeParameter);
                        }
                    }
                    break;

                case SymbolChange.Updated:
                    _typeDefs.AddUpdated(typeDef);
                    break;

                case SymbolChange.ContainsChanges:
                    // Members changed.
                    break;

                case SymbolChange.None:
                    // No changes to type.
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(change);
            }

            int typeIndex;
            var ok = _typeDefs.TryGetValue(typeDef, out typeIndex);
            Debug.Assert(ok);

            foreach (var eventDef in typeDef.GetEvents(this.Context))
            {
                int eventMapIndex;
                if (!_eventMap.TryGetValue(typeIndex, out eventMapIndex))
                {
                    _eventMap.Add(typeIndex);
                }

                this.AddDefIfNecessary(_eventDefs, eventDef);
            }

            foreach (var fieldDef in typeDef.GetFields(this.Context))
            {
                this.AddDefIfNecessary(_fieldDefs, fieldDef);
            }

            foreach (var methodDef in typeDef.GetMethods(this.Context))
            {
                if (this.AddDefIfNecessary(_methodDefs, methodDef))
                {
                    foreach (var paramDef in this.GetParametersToEmit(methodDef))
                    {
                        _parameterDefs.Add(paramDef);
                        _parameterDefList.Add(KeyValuePairUtil.Create(methodDef, paramDef));
                    }

                    if (methodDef.GenericParameterCount > 0)
                    {
                        foreach (var typeParameter in methodDef.GenericParameters)
                        {
                            _genericParameters.Add(typeParameter);
                        }
                    }
                }
            }

            foreach (var propertyDef in typeDef.GetProperties(this.Context))
            {
                int propertyMapIndex;
                if (!_propertyMap.TryGetValue(typeIndex, out propertyMapIndex))
                {
                    _propertyMap.Add(typeIndex);
                }

                this.AddDefIfNecessary(_propertyDefs, propertyDef);
            }

            var implementingMethods = ArrayBuilder<int>.GetInstance();

            // First, visit all MethodImplementations and add to this.methodImplList.
            foreach (var methodImpl in typeDef.GetExplicitImplementationOverrides(Context))
            {
                var methodDef = (IMethodDefinition)methodImpl.ImplementingMethod.AsDefinition(this.Context);
                int methodDefIndex;
                ok = _methodDefs.TryGetValue(methodDef, out methodDefIndex);
                Debug.Assert(ok);

                // If there are N existing MethodImpl entries for this MethodDef,
                // those will be index:1, ..., index:N, so it's sufficient to check for index:1.
                int methodImplIndex;
                var key = new MethodImplKey(methodDefIndex, index: 1);
                if (!_methodImpls.TryGetValue(key, out methodImplIndex))
                {
                    implementingMethods.Add(methodDefIndex);
                    this.methodImplList.Add(methodImpl);
                }
            }

            // Next, add placeholders to this.methodImpls for items added above.
            foreach (var methodDefIndex in implementingMethods)
            {
                int index = 1;
                while (true)
                {
                    int methodImplIndex;
                    var key = new MethodImplKey(methodDefIndex, index);
                    if (!_methodImpls.TryGetValue(key, out methodImplIndex))
                    {
                        _methodImpls.Add(key);
                        break;
                    }

                    index++;
                }
            }

            implementingMethods.Free();
        }

        private bool AddDefIfNecessary<T>(DefinitionIndex<T> defIndex, T def)
            where T : IDefinition
        {
            switch (_changes.GetChange(def))
            {
                case SymbolChange.Added:
                    defIndex.Add(def);
                    return true;
                case SymbolChange.Updated:
                    defIndex.AddUpdated(def);
                    return false;
                case SymbolChange.ContainsChanges:
                    Debug.Assert(def is INestedTypeDefinition);
                    // Changes to members within nested type only.
                    return false;
                default:
                    // No changes to member or container.
                    return false;
            }
        }

        protected override ReferenceIndexer CreateReferenceVisitor()
        {
            return new DeltaReferenceIndexer(this);
        }

        protected override void ReportReferencesToAddedSymbols()
        {
            foreach (var typeRef in GetTypeRefs())
            {
                ReportReferencesToAddedSymbol(typeRef as ISymbol);
            }

            foreach (var memberRef in GetMemberRefs())
            {
                ReportReferencesToAddedSymbol(memberRef as ISymbol);
            }
        }

        private void ReportReferencesToAddedSymbol(ISymbol symbolOpt)
        {
            if (symbolOpt != null && _changes.IsAdded(symbolOpt))
            {
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(
                    this.messageProvider.ERR_EncReferenceToAddedMember,
                    GetSymbolLocation(symbolOpt),
                    symbolOpt.Name,
                    symbolOpt.ContainingAssembly.Name));
            }
        }

        protected override StandaloneSignatureHandle SerializeLocalVariablesSignature(IMethodBody body)
        {
            StandaloneSignatureHandle localSignatureHandle;
            var localVariables = body.LocalVariables;
            var encInfos = ArrayBuilder<EncLocalInfo>.GetInstance();

            if (localVariables.Length > 0)
            {
                var writer = PooledBlobBuilder.GetInstance();
                var encoder = new BlobEncoder(writer).LocalVariableSignature(localVariables.Length);

                foreach (ILocalDefinition local in localVariables)
                {
                    var signature = local.Signature;
                    if (signature == null)
                    {
                        int start = writer.Count;
                        SerializeLocalVariableType(encoder.AddVariable(), local);
                        signature = writer.ToArray(start, writer.Count - start);
                    }
                    else
                    {
                        writer.WriteBytes(signature);
                    }

                    encInfos.Add(CreateEncLocalInfo(local, signature));
                }

                BlobHandle blobIndex = metadata.GetOrAddBlob(writer);

                localSignatureHandle = GetOrAddStandaloneSignatureHandle(blobIndex);
                writer.Free();
            }
            else
            {
                localSignatureHandle = default;
            }

            var info = new AddedOrChangedMethodInfo(
                body.MethodId,
                encInfos.ToImmutable(),
                body.LambdaDebugInfo,
                body.ClosureDebugInfo,
                body.StateMachineTypeName,
                body.StateMachineHoistedLocalSlots,
                body.StateMachineAwaiterSlots);

            _addedOrChangedMethods.Add(body.MethodDefinition, info);

            encInfos.Free();
            return localSignatureHandle;
        }

        private EncLocalInfo CreateEncLocalInfo(ILocalDefinition localDef, byte[] signature)
        {
            if (localDef.SlotInfo.Id.IsNone)
            {
                return new EncLocalInfo(signature);
            }

            // local type is already translated, but not recursively
            ITypeReference translatedType = localDef.Type;
            ITypeSymbol typeSymbol = translatedType as ITypeSymbol;
            if (typeSymbol != null)
            {
                translatedType = Context.Module.EncTranslateType(typeSymbol, Context.Diagnostics);
            }

            return new EncLocalInfo(localDef.SlotInfo, translatedType, localDef.Constraints, signature);
        }

        protected override void PopulateEncLogTableRows(ImmutableArray<int> rowCounts)
        {
            // The EncLog table is a log of all the operations needed
            // to update the previous metadata. That means all
            // new references must be added to the EncLog.
            var previousSizes = _previousGeneration.TableSizes;
            var deltaSizes = this.GetDeltaTableSizes(rowCounts);

            PopulateEncLogTableRows(TableIndex.AssemblyRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.ModuleRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.MemberRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.MethodSpec, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.TypeRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.TypeSpec, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.StandAloneSig, previousSizes, deltaSizes);

            PopulateEncLogTableRows(_typeDefs, TableIndex.TypeDef);
            PopulateEncLogTableRows(TableIndex.EventMap, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.PropertyMap, previousSizes, deltaSizes);

            PopulateEncLogTableEventsOrProperties(_eventDefs, TableIndex.Event, EditAndContinueOperation.AddEvent, _eventMap, TableIndex.EventMap);
            PopulateEncLogTableFieldsOrMethods(_fieldDefs, TableIndex.Field, EditAndContinueOperation.AddField);
            PopulateEncLogTableFieldsOrMethods(_methodDefs, TableIndex.MethodDef, EditAndContinueOperation.AddMethod);
            PopulateEncLogTableEventsOrProperties(_propertyDefs, TableIndex.Property, EditAndContinueOperation.AddProperty, _propertyMap, TableIndex.PropertyMap);

            PopulateEncLogTableParameters();

            PopulateEncLogTableRows(TableIndex.Constant, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.CustomAttribute, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.DeclSecurity, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.ClassLayout, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.FieldLayout, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.MethodSemantics, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.MethodImpl, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.ImplMap, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.FieldRva, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.NestedClass, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.GenericParam, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.InterfaceImpl, previousSizes, deltaSizes);
            PopulateEncLogTableRows(TableIndex.GenericParamConstraint, previousSizes, deltaSizes);
        }

        private void PopulateEncLogTableEventsOrProperties<T>(
            DefinitionIndex<T> index,
            TableIndex table,
            EditAndContinueOperation addCode,
            EventOrPropertyMapIndex map,
            TableIndex mapTable)
            where T : ITypeDefinitionMember
        {
            foreach (var member in index.GetRows())
            {
                if (index.IsAddedNotChanged(member))
                {
                    int typeIndex = _typeDefs[member.ContainingTypeDefinition];
                    Debug.Assert(typeIndex > 0);

                    int mapRowId;
                    var ok = map.TryGetValue(typeIndex, out mapRowId);
                    Debug.Assert(ok);

                    metadata.AddEncLogEntry(
                        entity: MetadataTokens.Handle(mapTable, mapRowId),
                        code: addCode);
                }

                metadata.AddEncLogEntry(
                    entity: MetadataTokens.Handle(table, index[member]),
                    code: EditAndContinueOperation.Default);
            }
        }

        private void PopulateEncLogTableFieldsOrMethods<T>(
            DefinitionIndex<T> index,
            TableIndex tableIndex,
            EditAndContinueOperation addCode)
            where T : ITypeDefinitionMember
        {
            foreach (var member in index.GetRows())
            {
                if (index.IsAddedNotChanged(member))
                {
                    metadata.AddEncLogEntry(
                        entity: MetadataTokens.TypeDefinitionHandle(_typeDefs[(INamedTypeDefinition)member.ContainingTypeDefinition]),
                        code: addCode);
                }

                metadata.AddEncLogEntry(
                    entity: MetadataTokens.Handle(tableIndex, index[member]),
                    code: EditAndContinueOperation.Default);
            }
        }

        private void PopulateEncLogTableParameters()
        {
            var parameterFirstId = _parameterDefs.FirstRowId;
            for (int i = 0; i < _parameterDefList.Count; i++)
            {
                var methodDef = _parameterDefList[i].Key;

                metadata.AddEncLogEntry(
                    entity: MetadataTokens.MethodDefinitionHandle(_methodDefs[methodDef]),
                    code: EditAndContinueOperation.AddParameter);

                metadata.AddEncLogEntry(
                    entity: MetadataTokens.ParameterHandle(parameterFirstId + i),
                    code: EditAndContinueOperation.Default);
            }
        }

        private void PopulateEncLogTableRows<T>(DefinitionIndex<T> index, TableIndex tableIndex)
            where T : IDefinition
        {
            foreach (var member in index.GetRows())
            {
                metadata.AddEncLogEntry(
                    entity: MetadataTokens.Handle(tableIndex, index[member]),
                    code: EditAndContinueOperation.Default);
            }
        }

        private void PopulateEncLogTableRows(TableIndex tableIndex, ImmutableArray<int> previousSizes, ImmutableArray<int> deltaSizes)
        {
            PopulateEncLogTableRows(tableIndex, previousSizes[(int)tableIndex] + 1, deltaSizes[(int)tableIndex]);
        }

        private void PopulateEncLogTableRows(TableIndex tableIndex, int firstRowId, int tokenCount)
        {
            for (int i = 0; i < tokenCount; i++)
            {
                metadata.AddEncLogEntry(
                    entity: MetadataTokens.Handle(tableIndex, firstRowId + i),
                    code: EditAndContinueOperation.Default);
            }
        }

        protected override void PopulateEncMapTableRows(ImmutableArray<int> rowCounts)
        {
            // The EncMap table maps from offset in each table in the delta
            // metadata to token. As such, the EncMap is a concatenated
            // list of all tokens in all tables from the delta sorted by table
            // and, within each table, sorted by row.
            var tokens = ArrayBuilder<EntityHandle>.GetInstance();
            var previousSizes = _previousGeneration.TableSizes;
            var deltaSizes = this.GetDeltaTableSizes(rowCounts);

            AddReferencedTokens(tokens, TableIndex.AssemblyRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.ModuleRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.MemberRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.MethodSpec, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.TypeRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.TypeSpec, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.StandAloneSig, previousSizes, deltaSizes);

            AddDefinitionTokens(tokens, _typeDefs, TableIndex.TypeDef);
            AddDefinitionTokens(tokens, _eventDefs, TableIndex.Event);
            AddDefinitionTokens(tokens, _fieldDefs, TableIndex.Field);
            AddDefinitionTokens(tokens, _methodDefs, TableIndex.MethodDef);
            AddDefinitionTokens(tokens, _propertyDefs, TableIndex.Property);

            AddReferencedTokens(tokens, TableIndex.Param, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.Constant, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.CustomAttribute, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.DeclSecurity, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.ClassLayout, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.FieldLayout, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.EventMap, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.PropertyMap, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.MethodSemantics, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.MethodImpl, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.ImplMap, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.FieldRva, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.NestedClass, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.GenericParam, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.InterfaceImpl, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.GenericParamConstraint, previousSizes, deltaSizes);

            tokens.Sort(HandleComparer.Default);

            // Should not be any duplicates.
            Debug.Assert(tokens.Distinct().Count() == tokens.Count);

            foreach (var token in tokens)
            {
                metadata.AddEncMapEntry(token);
            }

            tokens.Free();

            // Populate Portable PDB EncMap table with MethodDebugInformation mapping,
            // which corresponds 1:1 to MethodDef mapping.
            if (_debugMetadataOpt != null)
            {
                var debugTokens = ArrayBuilder<EntityHandle>.GetInstance();
                AddDefinitionTokens(debugTokens, _methodDefs, TableIndex.MethodDebugInformation);
                debugTokens.Sort(HandleComparer.Default);

                // Should not be any duplicates.
                Debug.Assert(debugTokens.Distinct().Count() == debugTokens.Count);

                foreach (var token in debugTokens)
                {
                    _debugMetadataOpt.AddEncMapEntry(token);
                }

                debugTokens.Free();
            }

#if DEBUG
            // The following tables are either represented in the EncMap
            // or specifically ignored. The rest should be empty.
            var handledTables = new TableIndex[]
            {
                TableIndex.Module,
                TableIndex.TypeRef,
                TableIndex.TypeDef,
                TableIndex.Field,
                TableIndex.MethodDef,
                TableIndex.Param,
                TableIndex.MemberRef,
                TableIndex.Constant,
                TableIndex.CustomAttribute,
                TableIndex.DeclSecurity,
                TableIndex.ClassLayout,
                TableIndex.FieldLayout,
                TableIndex.StandAloneSig,
                TableIndex.EventMap,
                TableIndex.Event,
                TableIndex.PropertyMap,
                TableIndex.Property,
                TableIndex.MethodSemantics,
                TableIndex.MethodImpl,
                TableIndex.ModuleRef,
                TableIndex.TypeSpec,
                TableIndex.ImplMap,
                // FieldRva is not needed since we only emit fields with explicit mapping
                // for <PrivateImplementationDetails> and that class is not used in ENC.
                // If we need FieldRva in the future, we'll need a corresponding test.
                // (See EditAndContinueTests.FieldRva that was deleted in this change.)
                //TableIndex.FieldRva,
                TableIndex.EncLog,
                TableIndex.EncMap,
                TableIndex.Assembly,
                TableIndex.AssemblyRef,
                TableIndex.MethodSpec,
                TableIndex.NestedClass,
                TableIndex.GenericParam,
                TableIndex.InterfaceImpl,
                TableIndex.GenericParamConstraint,
            };

            for (int i = 0; i < rowCounts.Length; i++)
            {
                if (handledTables.Contains((TableIndex)i))
                {
                    continue;
                }

                Debug.Assert(rowCounts[i] == 0);
            }
#endif
        }

        private static void AddReferencedTokens(
            ArrayBuilder<EntityHandle> builder,
            TableIndex tableIndex,
            ImmutableArray<int> previousSizes,
            ImmutableArray<int> deltaSizes)
        {
            AddReferencedTokens(builder, tableIndex, previousSizes[(int)tableIndex] + 1, deltaSizes[(int)tableIndex]);
        }

        private static void AddReferencedTokens(ArrayBuilder<EntityHandle> builder, TableIndex tableIndex, int firstRowId, int nTokens)
        {
            for (int i = 0; i < nTokens; i++)
            {
                builder.Add(MetadataTokens.Handle(tableIndex, firstRowId + i));
            }
        }

        private static void AddDefinitionTokens<T>(ArrayBuilder<EntityHandle> tokens, DefinitionIndex<T> index, TableIndex tableIndex)
            where T : IDefinition
        {
            foreach (var member in index.GetRows())
            {
                tokens.Add(MetadataTokens.Handle(tableIndex, index[member]));
            }
        }

        protected override void PopulateEventMapTableRows()
        {
            foreach (var typeId in _eventMap.GetRows())
            {
                metadata.AddEventMap(
                    declaringType: MetadataTokens.TypeDefinitionHandle(typeId),
                    eventList: MetadataTokens.EventDefinitionHandle(_eventMap[typeId]));
            }
        }

        protected override void PopulatePropertyMapTableRows()
        {
            foreach (var typeId in _propertyMap.GetRows())
            {
                metadata.AddPropertyMap(
                    declaringType: MetadataTokens.TypeDefinitionHandle(typeId),
                    propertyList: MetadataTokens.PropertyDefinitionHandle(_propertyMap[typeId]));
            }
        }

        private abstract class DefinitionIndexBase<T>
        {
            protected readonly Dictionary<T, int> added; // Definitions added in this generation.
            protected readonly List<T> rows; // Rows in this generation, containing adds and updates.
            private readonly int _firstRowId; // First row in this generation.
            private bool _frozen;

            public DefinitionIndexBase(int lastRowId)
            {
                this.added = new Dictionary<T, int>();
                this.rows = new List<T>();
                _firstRowId = lastRowId + 1;
            }

            public abstract bool TryGetValue(T item, out int index);

            public int this[T item]
            {
                get
                {
                    int token;
                    this.TryGetValue(item, out token);

                    // Fails if we are attempting to make a change that should have been reported as rude,
                    // e.g. the corresponding definitions type don't match, etc.
                    Debug.Assert(token > 0);

                    return token;
                }
            }

            // A method rather than a property since it freezes the table.
            public IReadOnlyDictionary<T, int> GetAdded()
            {
                this.Freeze();
                return this.added;
            }

            // A method rather than a property since it freezes the table.
            public IReadOnlyList<T> GetRows()
            {
                this.Freeze();
                return this.rows;
            }

            public int FirstRowId
            {
                get { return _firstRowId; }
            }

            public int NextRowId
            {
                get { return this.added.Count + _firstRowId; }
            }

            public bool IsFrozen
            {
                get { return _frozen; }
            }

            protected virtual void OnFrozen()
            {
#if DEBUG
                // Verify the rows are sorted.
                int prev = 0;
                foreach (var row in this.rows)
                {
                    int next = this.added[row];
                    Debug.Assert(prev < next);
                    prev = next;
                }
#endif
            }

            private void Freeze()
            {
                if (!_frozen)
                {
                    _frozen = true;
                    this.OnFrozen();
                }
            }
        }

        private sealed class DefinitionIndex<T> : DefinitionIndexBase<T> where T : IDefinition
        {
            public delegate bool TryGetExistingIndex(T item, out int index);

            private readonly TryGetExistingIndex _tryGetExistingIndex;

            // Map of row id to def for all defs. This could be an array indexed
            // by row id but the array could be large and sparsely populated
            // if there are many defs in the previous generation but few
            // references to those defs in the current generation.
            private readonly Dictionary<int, T> _map;

            public DefinitionIndex(TryGetExistingIndex tryGetExistingIndex, int lastRowId)
                : base(lastRowId)
            {
                _tryGetExistingIndex = tryGetExistingIndex;
                _map = new Dictionary<int, T>();
            }

            public override bool TryGetValue(T item, out int index)
            {
                if (this.added.TryGetValue(item, out index))
                {
                    return true;
                }

                if (_tryGetExistingIndex(item, out index))
                {
#if DEBUG
                    T other;
                    Debug.Assert(!_map.TryGetValue(index, out other) || ((object)other == (object)item));
#endif
                    _map[index] = item;
                    return true;
                }
                return false;
            }

            public T this[int rowId]
            {
                get
                {
                    return _map[rowId];
                }
            }

            public void Add(T item)
            {
                Debug.Assert(!this.IsFrozen);

                int index = this.NextRowId;
                this.added.Add(item, index);
                _map[index] = item;
                this.rows.Add(item);
            }

            /// <summary>
            /// Add an item from a previous generation
            /// that has been updated in this generation.
            /// </summary>
            public void AddUpdated(T item)
            {
                Debug.Assert(!this.IsFrozen);
                this.rows.Add(item);
            }

            public bool IsAddedNotChanged(T item)
            {
                return this.added.ContainsKey(item);
            }

            protected override void OnFrozen()
            {
                this.rows.Sort(this.CompareRows);
            }

            private int CompareRows(T x, T y)
            {
                return this[x] - this[y];
            }
        }

        private bool TryGetExistingTypeDefIndex(ITypeDefinition item, out int index)
        {
            if (_previousGeneration.TypesAdded.TryGetValue(item, out index))
            {
                return true;
            }

            TypeDefinitionHandle handle;
            if (_definitionMap.TryGetTypeHandle(item, out handle))
            {
                index = MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingEventDefIndex(IEventDefinition item, out int index)
        {
            if (_previousGeneration.EventsAdded.TryGetValue(item, out index))
            {
                return true;
            }

            EventDefinitionHandle handle;
            if (_definitionMap.TryGetEventHandle(item, out handle))
            {
                index = MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingFieldDefIndex(IFieldDefinition item, out int index)
        {
            if (_previousGeneration.FieldsAdded.TryGetValue(item, out index))
            {
                return true;
            }

            FieldDefinitionHandle handle;
            if (_definitionMap.TryGetFieldHandle(item, out handle))
            {
                index = MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingMethodDefIndex(IMethodDefinition item, out int index)
        {
            if (_previousGeneration.MethodsAdded.TryGetValue(item, out index))
            {
                return true;
            }

            MethodDefinitionHandle handle;
            if (_definitionMap.TryGetMethodHandle(item, out handle))
            {
                index = MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingPropertyDefIndex(IPropertyDefinition item, out int index)
        {
            if (_previousGeneration.PropertiesAdded.TryGetValue(item, out index))
            {
                return true;
            }

            PropertyDefinitionHandle handle;
            if (_definitionMap.TryGetPropertyHandle(item, out handle))
            {
                index = MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingEventMapIndex(int item, out int index)
        {
            if (_previousGeneration.EventMapAdded.TryGetValue(item, out index))
            {
                return true;
            }

            if (_previousGeneration.TypeToEventMap.TryGetValue(item, out index))
            {
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingPropertyMapIndex(int item, out int index)
        {
            if (_previousGeneration.PropertyMapAdded.TryGetValue(item, out index))
            {
                return true;
            }

            if (_previousGeneration.TypeToPropertyMap.TryGetValue(item, out index))
            {
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingMethodImplIndex(MethodImplKey item, out int index)
        {
            if (_previousGeneration.MethodImplsAdded.TryGetValue(item, out index))
            {
                return true;
            }

            if (_previousGeneration.MethodImpls.TryGetValue(item, out index))
            {
                return true;
            }

            index = 0;
            return false;
        }

        private sealed class ParameterDefinitionIndex : DefinitionIndexBase<IParameterDefinition>
        {
            public ParameterDefinitionIndex(int lastRowId)
                : base(lastRowId)
            {
            }

            public override bool TryGetValue(IParameterDefinition item, out int index)
            {
                return this.added.TryGetValue(item, out index);
            }

            public void Add(IParameterDefinition item)
            {
                Debug.Assert(!this.IsFrozen);

                int index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class GenericParameterIndex : DefinitionIndexBase<IGenericParameter>
        {
            public GenericParameterIndex(int lastRowId)
                : base(lastRowId)
            {
            }

            public override bool TryGetValue(IGenericParameter item, out int index)
            {
                return this.added.TryGetValue(item, out index);
            }

            public void Add(IGenericParameter item)
            {
                Debug.Assert(!this.IsFrozen);

                int index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class EventOrPropertyMapIndex : DefinitionIndexBase<int>
        {
            public delegate bool TryGetExistingIndex(int item, out int index);

            private readonly TryGetExistingIndex _tryGetExistingIndex;

            public EventOrPropertyMapIndex(TryGetExistingIndex tryGetExistingIndex, int lastRowId)
                : base(lastRowId)
            {
                _tryGetExistingIndex = tryGetExistingIndex;
            }

            public override bool TryGetValue(int item, out int index)
            {
                if (this.added.TryGetValue(item, out index))
                {
                    return true;
                }

                if (_tryGetExistingIndex(item, out index))
                {
                    return true;
                }

                index = 0;
                return false;
            }

            public void Add(int item)
            {
                Debug.Assert(!this.IsFrozen);

                int index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class MethodImplIndex : DefinitionIndexBase<MethodImplKey>
        {
            private readonly DeltaMetadataWriter _writer;

            public MethodImplIndex(DeltaMetadataWriter writer, int lastRowId)
                : base(lastRowId)
            {
                _writer = writer;
            }

            public override bool TryGetValue(MethodImplKey item, out int index)
            {
                if (this.added.TryGetValue(item, out index))
                {
                    return true;
                }

                if (_writer.TryGetExistingMethodImplIndex(item, out index))
                {
                    return true;
                }

                index = 0;
                return false;
            }

            public void Add(MethodImplKey item)
            {
                Debug.Assert(!this.IsFrozen);

                int index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class DeltaReferenceIndexer : ReferenceIndexer
        {
            private readonly SymbolChanges _changes;

            public DeltaReferenceIndexer(DeltaMetadataWriter writer)
                : base(writer)
            {
                _changes = writer._changes;
            }

            public override void Visit(CommonPEModuleBuilder module)
            {
                Visit(module.GetTopLevelTypeDefinitions(metadataWriter.Context));
            }

            public override void Visit(IEventDefinition eventDefinition)
            {
                Debug.Assert(this.ShouldVisit(eventDefinition));
                base.Visit(eventDefinition);
            }

            public override void Visit(IFieldDefinition fieldDefinition)
            {
                Debug.Assert(this.ShouldVisit(fieldDefinition));
                base.Visit(fieldDefinition);
            }

            public override void Visit(ILocalDefinition localDefinition)
            {
                if (localDefinition.Signature == null)
                {
                    base.Visit(localDefinition);
                }
            }

            public override void Visit(IMethodDefinition method)
            {
                Debug.Assert(this.ShouldVisit(method));
                base.Visit(method);
            }

            public override void Visit(Cci.MethodImplementation methodImplementation)
            {
                // Unless the implementing method was added,
                // the method implementation already exists.
                var methodDef = (IMethodDefinition)methodImplementation.ImplementingMethod.AsDefinition(this.Context);
                if (_changes.GetChange(methodDef) == SymbolChange.Added)
                {
                    base.Visit(methodImplementation);
                }
            }

            public override void Visit(INamespaceTypeDefinition namespaceTypeDefinition)
            {
                Debug.Assert(this.ShouldVisit(namespaceTypeDefinition));
                base.Visit(namespaceTypeDefinition);
            }

            public override void Visit(INestedTypeDefinition nestedTypeDefinition)
            {
                Debug.Assert(this.ShouldVisit(nestedTypeDefinition));
                base.Visit(nestedTypeDefinition);
            }

            public override void Visit(IPropertyDefinition propertyDefinition)
            {
                Debug.Assert(this.ShouldVisit(propertyDefinition));
                base.Visit(propertyDefinition);
            }

            public override void Visit(ITypeDefinition typeDefinition)
            {
                if (this.ShouldVisit(typeDefinition))
                {
                    base.Visit(typeDefinition);
                }
            }

            public override void Visit(ITypeDefinitionMember typeMember)
            {
                if (this.ShouldVisit(typeMember))
                {
                    base.Visit(typeMember);
                }
            }

            private bool ShouldVisit(IDefinition def)
            {
                return _changes.GetChange(def) != SymbolChange.None;
            }
        }
    }
}
