// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class DeltaPeWriter : PeWriter
    {
        private readonly EmitBaseline previousGeneration;
        private readonly Guid encId;
        private readonly DefinitionMap definitionMap;
        private readonly SymbolChanges changes;

        private readonly DefinitionIndex<ITypeDefinition> typeDefs;
        private readonly DefinitionIndex<IEventDefinition> eventDefs;
        private readonly DefinitionIndex<IFieldDefinition> fieldDefs;
        private readonly DefinitionIndex<IMethodDefinition> methodDefs;
        private readonly DefinitionIndex<IPropertyDefinition> propertyDefs;
        private readonly ParameterDefinitionIndex parameterDefs;
        private readonly List<KeyValuePair<IMethodDefinition, IParameterDefinition>> parameterDefList;
        private readonly GenericParameterIndex genericParameters;
        private readonly EventOrPropertyMapIndex eventMap;
        private readonly EventOrPropertyMapIndex propertyMap;
        private readonly MethodImplIndex methodImpls;

        private readonly HeapOrReferenceIndex<IAssemblyReference> assemblyRefIndex;
        private readonly HeapOrReferenceIndex<string> moduleRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeMemberReference> memberRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference> methodSpecIndex;
        private readonly HeapOrReferenceIndex<ITypeReference> typeRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeReference> typeSpecIndex;
        private readonly HeapOrReferenceIndex<uint> standAloneSignatureIndex;
        private readonly Dictionary<IMethodDefinition, ImmutableArray<LocalDefinition>> localMap;

        private uint unalignedStringStreamLength;

        public DeltaPeWriter(
            EmitContext context,
            CommonMessageProvider messageProvider,
            PdbWriter pdbWriter,
            EmitBaseline previousGeneration,
            Guid encId,
            DefinitionMap definitionMap,
            SymbolChanges changes,
            CancellationToken cancellationToken) :
            base(context, messageProvider, pdbWriter, false, false, false, cancellationToken)
        {
            Debug.Assert(previousGeneration != null);
            Debug.Assert(encId != default(Guid));
            Debug.Assert(encId != previousGeneration.EncId);

            this.previousGeneration = previousGeneration;
            this.encId = encId;
            this.definitionMap = definitionMap;
            this.changes = changes;

            this.guidWriter.Pad(this.previousGeneration.GuidStreamLength);

            var sizes = previousGeneration.TableSizes;

            this.typeDefs = new DefinitionIndex<ITypeDefinition>(this.TryGetExistingTypeDefIndex, (uint)sizes[(int)TableIndex.TypeDef]);
            this.eventDefs = new DefinitionIndex<IEventDefinition>(this.TryGetExistingEventDefIndex, (uint)sizes[(int)TableIndex.Event]);
            this.fieldDefs = new DefinitionIndex<IFieldDefinition>(this.TryGetExistingFieldDefIndex, (uint)sizes[(int)TableIndex.Field]);
            this.methodDefs = new DefinitionIndex<IMethodDefinition>(this.TryGetExistingMethodDefIndex, (uint)sizes[(int)TableIndex.MethodDef]);
            this.propertyDefs = new DefinitionIndex<IPropertyDefinition>(this.TryGetExistingPropertyDefIndex, (uint)sizes[(int)TableIndex.Property]);
            this.parameterDefs = new ParameterDefinitionIndex((uint)sizes[(int)TableIndex.Param]);
            this.parameterDefList = new List<KeyValuePair<IMethodDefinition, IParameterDefinition>>();
            this.genericParameters = new GenericParameterIndex((uint)sizes[(int)TableIndex.GenericParam]);
            this.eventMap = new EventOrPropertyMapIndex(this.TryGetExistingEventMapIndex, (uint)sizes[(int)TableIndex.EventMap]);
            this.propertyMap = new EventOrPropertyMapIndex(this.TryGetExistingPropertyMapIndex, (uint)sizes[(int)TableIndex.PropertyMap]);
            this.methodImpls = new MethodImplIndex(this, (uint)sizes[(int)TableIndex.MethodImpl]);

            this.assemblyRefIndex = new HeapOrReferenceIndex<IAssemblyReference>(this, AssemblyReferenceComparer.Instance, lastRowId: (uint)sizes[(int)TableIndex.AssemblyRef]);
            this.moduleRefIndex = new HeapOrReferenceIndex<string>(this, lastRowId: (uint)sizes[(int)TableIndex.ModuleRef]);
            this.memberRefIndex = new InstanceAndStructuralReferenceIndex<ITypeMemberReference>(this, new MemberRefComparer(this), lastRowId: (uint)sizes[(int)TableIndex.MemberRef]);
            this.methodSpecIndex = new InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference>(this, new MethodSpecComparer(this), lastRowId: (uint)sizes[(int)TableIndex.MethodSpec]);
            this.typeRefIndex = new HeapOrReferenceIndex<ITypeReference>(this, lastRowId: (uint)sizes[(int)TableIndex.TypeRef]);
            this.typeSpecIndex = new InstanceAndStructuralReferenceIndex<ITypeReference>(this, new TypeSpecComparer(this), lastRowId: (uint)sizes[(int)TableIndex.TypeSpec]);
            this.standAloneSignatureIndex = new HeapOrReferenceIndex<uint>(this, lastRowId: (uint)sizes[(int)TableIndex.StandAloneSig]);

            this.localMap = new Dictionary<IMethodDefinition, ImmutableArray<LocalDefinition>>();
        }

        private ImmutableArray<int> GetDeltaTableSizes()
        {
            var sizes = new int[MetadataTokens.TableCount];
            this.GetTableSizes(sizes);

            sizes[(int)TableIndex.TypeRef] = this.typeRefIndex.Rows.Count;
            sizes[(int)TableIndex.TypeDef] = this.typeDefs.GetAdded().Count;
            sizes[(int)TableIndex.Field] = this.fieldDefs.GetAdded().Count;
            sizes[(int)TableIndex.MethodDef] = this.methodDefs.GetAdded().Count;
            sizes[(int)TableIndex.Param] = this.parameterDefs.GetAdded().Count;
            sizes[(int)TableIndex.MemberRef] = this.memberRefIndex.Rows.Count;
            sizes[(int)TableIndex.StandAloneSig] = this.standAloneSignatureIndex.Rows.Count;
            sizes[(int)TableIndex.EventMap] = this.eventMap.GetAdded().Count;
            sizes[(int)TableIndex.Event] = this.eventDefs.GetAdded().Count;
            sizes[(int)TableIndex.PropertyMap] = this.propertyMap.GetAdded().Count;
            sizes[(int)TableIndex.Property] = this.propertyDefs.GetAdded().Count;
            sizes[(int)TableIndex.MethodImpl] = this.methodImpls.GetAdded().Count;
            sizes[(int)TableIndex.ModuleRef] = this.moduleRefIndex.Rows.Count;
            sizes[(int)TableIndex.TypeSpec] = this.typeSpecIndex.Rows.Count;
            sizes[(int)TableIndex.AssemblyRef] = this.assemblyRefIndex.Rows.Count;
            sizes[(int)TableIndex.GenericParam] = this.genericParameters.GetAdded().Count;
            sizes[(int)TableIndex.MethodSpec] = this.methodSpecIndex.Rows.Count;

            return ImmutableArray.Create(sizes);
        }

        internal EmitBaseline GetDelta(
            EmitBaseline baseline,
            Microsoft.CodeAnalysis.Compilation compilation,
            Guid encId)
        {
            Debug.Assert(this.unalignedStringStreamLength > 0); // OnSerializedMetadataTables should have been called.

            var moduleBuilder = (CommonPEModuleBuilder)this.module;
            var locals = new Dictionary<uint, ImmutableArray<EncLocalInfo>>();
            foreach (var pair in this.localMap)
            {
                var methodDef = pair.Key;
                var methodIndex = this.GetMethodDefIndex(methodDef);
                var localOffsetsAndKinds = this.definitionMap.GetLocalInfo(methodDef, pair.Value);
                locals.Add(methodIndex, localOffsetsAndKinds);
            }

            var previousTableSizes = this.previousGeneration.TableEntriesAdded;
            var deltaTableSizes = this.GetDeltaTableSizes();
            var tableSizes = new int[MetadataTokens.TableCount];

            for (int i = 0; i < tableSizes.Length; i++)
            {
                tableSizes[i] = previousTableSizes[i] + deltaTableSizes[i];
            }

            return baseline.With(
                compilation,
                moduleBuilder,
                baseline.Ordinal + 1,
                encId,
                typesAdded: AddRange(this.previousGeneration.TypesAdded, this.typeDefs.GetAdded()),
                eventsAdded: AddRange(this.previousGeneration.EventsAdded, this.eventDefs.GetAdded()),
                fieldsAdded: AddRange(this.previousGeneration.FieldsAdded, this.fieldDefs.GetAdded()),
                methodsAdded: AddRange(this.previousGeneration.MethodsAdded, this.methodDefs.GetAdded()),
                propertiesAdded: AddRange(this.previousGeneration.PropertiesAdded, this.propertyDefs.GetAdded()),
                eventMapAdded: AddRange(this.previousGeneration.EventMapAdded, this.eventMap.GetAdded()),
                propertyMapAdded: AddRange(this.previousGeneration.PropertyMapAdded, this.propertyMap.GetAdded()),
                methodImplsAdded: AddRange(this.previousGeneration.MethodImplsAdded, this.methodImpls.GetAdded()),
                tableEntriesAdded: ImmutableArray.Create(tableSizes),
                // Blob stream is concatenated aligned.
                blobStreamLengthAdded: (int)this.blobWriter.BaseStream.Length + this.previousGeneration.BlobStreamLengthAdded,
                // String stream is concatenated unaligned.
                stringStreamLengthAdded: (int)this.unalignedStringStreamLength + this.previousGeneration.StringStreamLengthAdded,
                // UserString stream is concatenated aligned.
                userStringStreamLengthAdded: (int)this.userStringWriter.BaseStream.Length + this.previousGeneration.UserStringStreamLengthAdded,
                // Guid stream is always aligned (the size if a multiple of 16 = sizeof(Guid))
                guidStreamLengthAdded: (int)this.guidWriter.BaseStream.Length + this.previousGeneration.GuidStreamLengthAdded,
                anonymousTypeMap: ((IPEDeltaAssemblyBuilder)moduleBuilder).GetAnonymousTypeMap(),
                localsForMethodsAddedOrChanged: AddRange(locals, this.previousGeneration.LocalsForMethodsAddedOrChanged, replace: true),
                localNames: baseline.LocalNames);
        }

        private static IReadOnlyDictionary<K, V> AddRange<K, V>(IReadOnlyDictionary<K, V> a, IReadOnlyDictionary<K, V> b, bool replace = false)
        {
            if (a.Count == 0)
            {
                return b;
            }
            if (b.Count == 0)
            {
                return a;
            }
            var result = new Dictionary<K, V>();
            foreach (var pair in a)
            {
                result.Add(pair.Key, pair.Value);
            }
            foreach (var pair in b)
            {
                Debug.Assert(replace || !a.ContainsKey(pair.Key));
                result[pair.Key] = pair.Value;
            }
            return result;
        }

        /// <summary>
        /// Return tokens for all modified methods.
        /// </summary>
        public void GetMethodTokens(ICollection<uint> methods)
        {
            foreach (var def in this.methodDefs.GetRows())
            {
                if (!this.methodDefs.IsAddedNotChanged(def))
                {
                    methods.Add(TokenTypeIds.MethodDef | this.methodDefs[def]);
                }
            }
        }

        protected override ushort Generation
        {
            get { return (ushort)(this.previousGeneration.Ordinal + 1); }
        }

        protected override System.Guid EncId
        {
            get { return this.encId; }
        }

        protected override Guid EncBaseId
        {
            get { return this.previousGeneration.EncId; }
        }

        protected override bool CompressMetadataStream
        {
            get { return false; }
        }

        protected override uint GetEventDefIndex(IEventDefinition def)
        {
            return this.eventDefs[def];
        }

        protected override IReadOnlyList<IEventDefinition> GetEventDefs()
        {
            return this.eventDefs.GetRows();
        }

        protected override uint GetFieldDefIndex(IFieldDefinition def)
        {
            return this.fieldDefs[def];
        }

        protected override IReadOnlyList<IFieldDefinition> GetFieldDefs()
        {
            return this.fieldDefs.GetRows();
        }

        protected override bool TryGetTypeDefIndex(ITypeDefinition def, out uint index)
        {
            return this.typeDefs.TryGetValue(def, out index);
        }

        protected override uint GetTypeDefIndex(ITypeDefinition def)
        {
            return this.typeDefs[def];
        }

        protected override ITypeDefinition GetTypeDef(int index)
        {
            return this.typeDefs[index];
        }

        protected override IReadOnlyList<ITypeDefinition> GetTypeDefs()
        {
            return this.typeDefs.GetRows();
        }

        protected override bool TryGetMethodDefIndex(IMethodDefinition def, out uint index)
        {
            return this.methodDefs.TryGetValue(def, out index);
        }

        protected override uint GetMethodDefIndex(IMethodDefinition def)
        {
            return this.methodDefs[def];
        }

        protected override IMethodDefinition GetMethodDef(int index)
        {
            return this.methodDefs[index];
        }

        protected override IReadOnlyList<IMethodDefinition> GetMethodDefs()
        {
            return this.methodDefs.GetRows();
        }

        protected override uint GetPropertyDefIndex(IPropertyDefinition def)
        {
            return this.propertyDefs[def];
        }

        protected override IReadOnlyList<IPropertyDefinition> GetPropertyDefs()
        {
            return this.propertyDefs.GetRows();
        }

        protected override uint GetParameterDefIndex(IParameterDefinition def)
        {
            return this.parameterDefs[def];
        }

        protected override IReadOnlyList<IParameterDefinition> GetParameterDefs()
        {
            return this.parameterDefs.GetRows();
        }

        protected override uint GetGenericParameterIndex(IGenericParameter def)
        {
            return this.genericParameters[def];
        }

        protected override IReadOnlyList<IGenericParameter> GetGenericParameters()
        {
            return this.genericParameters.GetRows();
        }

        protected override uint GetFieldDefIndex(INamedTypeDefinition typeDef)
        {
            // Fields are associated with the
            // type through the EncLog table.
            return 0u;
        }

        protected override uint GetMethodDefIndex(INamedTypeDefinition typeDef)
        {
            // Methods are associated with the
            // type through the EncLog table.
            return 0u;
        }

        protected override uint GetParameterDefIndex(IMethodDefinition methodDef)
        {
            // Parameters are associated with the
            // method through the EncLog table.
            return 0u;
        }

        protected override uint GetOrAddAssemblyRefIndex(IAssemblyReference reference)
        {
            return this.assemblyRefIndex.GetOrAdd(reference);
        }

        protected override IReadOnlyList<IAssemblyReference> GetAssemblyRefs()
        {
            return this.assemblyRefIndex.Rows;
        }

        protected override uint GetOrAddModuleRefIndex(string reference)
        {
            return this.moduleRefIndex.GetOrAdd(reference);
        }

        protected override IReadOnlyList<string> GetModuleRefs()
        {
            return this.moduleRefIndex.Rows;
        }

        protected override uint GetOrAddMemberRefIndex(ITypeMemberReference reference)
        {
            return this.memberRefIndex.GetOrAdd(reference);
        }

        protected override IReadOnlyList<ITypeMemberReference> GetMemberRefs()
        {
            return this.memberRefIndex.Rows;
        }

        protected override uint GetOrAddMethodSpecIndex(IGenericMethodInstanceReference reference)
        {
            return this.methodSpecIndex.GetOrAdd(reference);
        }

        protected override IReadOnlyList<IGenericMethodInstanceReference> GetMethodSpecs()
        {
            return this.methodSpecIndex.Rows;
        }

        protected override bool TryGetTypeRefIndex(ITypeReference reference, out uint index)
        {
            return this.typeRefIndex.TryGetValue(reference, out index);
        }

        protected override uint GetOrAddTypeRefIndex(ITypeReference reference)
        {
            return this.typeRefIndex.GetOrAdd(reference);
        }

        protected override IReadOnlyList<ITypeReference> GetTypeRefs()
        {
            return this.typeRefIndex.Rows;
        }

        protected override uint GetOrAddTypeSpecIndex(ITypeReference reference)
        {
            return this.typeSpecIndex.GetOrAdd(reference);
        }

        protected override IReadOnlyList<ITypeReference> GetTypeSpecs()
        {
            return this.typeSpecIndex.Rows;
        }

        protected override uint GetOrAddStandAloneSignatureIndex(uint blobIndex)
        {
            return this.standAloneSignatureIndex.GetOrAdd(blobIndex);
        }

        protected override IReadOnlyList<uint> GetStandAloneSignatures()
        {
            return this.standAloneSignatureIndex.Rows;
        }

        protected override IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(IModule module)
        {
            return this.changes.GetTopLevelTypes(this.Context);
        }

        protected override void CreateIndicesForModule()
        {
            base.CreateIndicesForModule();
            var module = (IPEDeltaAssemblyBuilder)this.module;
            module.OnCreatedIndices(this.Context.Diagnostics);
        }

        protected override void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef)
        {
            switch (this.changes.GetChange(typeDef))
            {
                case SymbolChange.Added:
                    this.typeDefs.Add(typeDef);
                    var typeParameters = this.GetConsolidatedTypeParameters(typeDef);
                    if (typeParameters != null)
                    {
                        foreach (var typeParameter in typeParameters)
                        {
                            this.genericParameters.Add(typeParameter);
                        }
                    }
                    break;
                case SymbolChange.Updated:
                    this.typeDefs.AddUpdated(typeDef);
                    break;
                case SymbolChange.None:
                    // No changes to type or members.
                    return;
            }

            uint typeIndex;
            var ok = this.typeDefs.TryGetValue(typeDef, out typeIndex);
            Debug.Assert(ok);

            foreach (var eventDef in typeDef.Events)
            {
                uint eventMapIndex;
                if (!this.eventMap.TryGetValue(typeIndex, out eventMapIndex))
                {
                    this.eventMap.Add(typeIndex);
                }

                this.AddDefIfNecessary(this.eventDefs, eventDef);
            }

            foreach (var fieldDef in typeDef.GetFields(this.Context))
            {
                this.AddDefIfNecessary(this.fieldDefs, fieldDef);
            }

            foreach (var methodDef in typeDef.GetMethods(this.Context))
            {
                if (this.AddDefIfNecessary(this.methodDefs, methodDef))
                {
                    foreach (var paramDef in this.GetParametersToEmit(methodDef))
                    {
                        this.parameterDefs.Add(paramDef);
                        this.parameterDefList.Add(KeyValuePair.Create(methodDef, paramDef));
                    }

                    if (methodDef.GenericParameterCount > 0)
                    {
                        foreach (var typeParameter in methodDef.GenericParameters)
                        {
                            this.genericParameters.Add(typeParameter);
                        }
                    }
                }
            }

            foreach (var propertyDef in typeDef.GetProperties(this.Context))
            {
                uint propertyMapIndex;
                if (!this.propertyMap.TryGetValue(typeIndex, out propertyMapIndex))
                {
                    this.propertyMap.Add(typeIndex);
                }

                this.AddDefIfNecessary(this.propertyDefs, propertyDef);
            }

            var implementingMethods = ArrayBuilder<uint>.GetInstance();

            // First, visit all IMethodImplementations and add to this.methodImplList.
            foreach (var methodImpl in typeDef.GetExplicitImplementationOverrides(Context))
            {
                var methodDef = (IMethodDefinition)methodImpl.ImplementingMethod.AsDefinition(this.Context);
                uint methodDefIndex;
                ok = this.methodDefs.TryGetValue(methodDef, out methodDefIndex);
                Debug.Assert(ok);

                // If there are N existing MethodImpl entries for this MethodDef,
                // those will be index:1, ..., index:N, so it's sufficient to check for index:1.
                uint methodImplIndex;
                var key = new MethodImplKey(methodDefIndex, index: 1);
                if (!this.methodImpls.TryGetValue(key, out methodImplIndex))
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
                    uint methodImplIndex;
                    var key = new MethodImplKey(methodDefIndex, index);
                    if (!this.methodImpls.TryGetValue(key, out methodImplIndex))
                    {
                        this.methodImpls.Add(key);
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
            switch (this.changes.GetChange(def))
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

        protected override uint GetBlobStreamOffset()
        {
            return (uint)this.previousGeneration.BlobStreamLength;
        }

        protected override uint GetStringStreamOffset()
        {
            return (uint)this.previousGeneration.StringStreamLength;
        }

        protected override uint GetUserStringStreamOffset()
        {
            return (uint)this.previousGeneration.UserStringStreamLength;
        }

        protected override ReferenceIndexer CreateReferenceVisitor()
        {
            return new DeltaReferenceIndexer(this);
        }

        protected override void OnSerializedMethodBody(IMethodBody body)
        {
            var method = body.MethodDefinition;
            if (!method.IsImplicitlyDeclared)
            {
                var locals = body.LocalVariables;
                this.localMap[method] = (locals == null) ? ImmutableArray<LocalDefinition>.Empty : ImmutableArray.CreateRange(locals.Cast<LocalDefinition>());
            }
        }

        protected override void OnBeforeHeapsAligned()
        {
            // Capture the size of the String heap before the heaps
            // are aligned to 4-byte boundaries since the String heap deltas are
            // concatenated to the unaligned heap.
            Debug.Assert(this.unalignedStringStreamLength == 0);
            this.unalignedStringStreamLength = this.stringWriter.BaseStream.Length;
        }

        protected override void PopulateEncLogTableRows(List<EncLogRow> table)
        {
            // The EncLog table is a log of all the operations needed
            // to update the previous metadata. That means all
            // new references must be added to the EncLog.
            var previousSizes = this.previousGeneration.TableSizes;
            var deltaSizes = this.GetDeltaTableSizes();

            PopulateEncLogTableRows(table, TableIndex.AssemblyRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.ModuleRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.MemberRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.MethodSpec, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.TypeRef, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.TypeSpec, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.StandAloneSig, previousSizes, deltaSizes);

            PopulateEncLogTableRows(table, this.typeDefs, TokenTypeIds.TypeDef);
            PopulateEncLogTableRows(table, TableIndex.EventMap, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.PropertyMap, previousSizes, deltaSizes);

            PopulateEncLogTableEventsOrProperties(table, this.eventDefs, TokenTypeIds.Event, EncFuncCode.AddEvent, this.eventMap, TokenTypeIds.EventMap);
            PopulateEncLogTableFieldsOrMethods(table, this.fieldDefs, TokenTypeIds.FieldDef, EncFuncCode.AddField);
            PopulateEncLogTableFieldsOrMethods(table, this.methodDefs, TokenTypeIds.MethodDef, EncFuncCode.AddMethod);
            PopulateEncLogTableEventsOrProperties(table, this.propertyDefs, TokenTypeIds.Property, EncFuncCode.AddProperty, this.propertyMap, TokenTypeIds.PropertyMap);

            PopulateEncLogTableParameters(table);

            PopulateEncLogTableRows(table, TableIndex.Constant, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.CustomAttribute, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.DeclSecurity, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.ClassLayout, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.FieldLayout, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.MethodSemantics, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.MethodImpl, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.ImplMap, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.FieldRva, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.NestedClass, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.GenericParam, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.InterfaceImpl, previousSizes, deltaSizes);
            PopulateEncLogTableRows(table, TableIndex.GenericParamConstraint, previousSizes, deltaSizes);
        }

        private void PopulateEncLogTableEventsOrProperties<T>(
            List<EncLogRow> table,
            DefinitionIndex<T> index,
            uint tokenType,
            EncFuncCode addCode,
            EventOrPropertyMapIndex map,
            uint mapTokenType)
            where T : ITypeDefinitionMember
        {
            foreach (var member in index.GetRows())
            {
                uint typeIndex = this.typeDefs[member.ContainingTypeDefinition];
                Debug.Assert(typeIndex > 0);

                uint mapIndex;
                var ok = map.TryGetValue(typeIndex, out mapIndex);
                Debug.Assert(ok);

                uint row = index[member];
                uint mapToken = mapTokenType | mapIndex;
                table.Add(new EncLogRow() { Token = mapToken, FuncCode = addCode });

                uint token = tokenType | row;
                table.Add(new EncLogRow() { Token = token, FuncCode = EncFuncCode.Delta });
            }
        }

        private void PopulateEncLogTableFieldsOrMethods<T>(
            List<EncLogRow> table,
            DefinitionIndex<T> index,
            uint tokenType,
            EncFuncCode addCode)
            where T : ITypeDefinitionMember
        {
            foreach (var member in index.GetRows())
            {
                if (index.IsAddedNotChanged(member))
                {
                    uint typeToken = TokenTypeIds.TypeDef | this.typeDefs[(INamedTypeDefinition)member.ContainingTypeDefinition];
                    table.Add(new EncLogRow() { Token = typeToken, FuncCode = addCode });
                }

                uint token = tokenType | index[member];
                table.Add(new EncLogRow() { Token = token, FuncCode = EncFuncCode.Delta });
            }
        }

        private void PopulateEncLogTableParameters(List<EncLogRow> table)
        {
            var parameterFirstId = this.parameterDefs.FirstRowId;
            for (int i = 0; i < this.parameterDefList.Count; i++)
            {
                var methodDef = this.parameterDefList[i].Key;
                uint methodToken = TokenTypeIds.MethodDef | this.methodDefs[methodDef];
                table.Add(new EncLogRow() { Token = methodToken, FuncCode = EncFuncCode.AddParameter });

                uint paramRowId = (uint)(parameterFirstId + i);
                uint token = TokenTypeIds.ParamDef | paramRowId;
                table.Add(new EncLogRow() { Token = token, FuncCode = EncFuncCode.Delta });
            }
        }

        private static void PopulateEncLogTableRows<T>(List<EncLogRow> table, DefinitionIndex<T> index, uint tokenType)
            where T : IDefinition
        {
            foreach (var member in index.GetRows())
            {
                uint token = tokenType | index[member];
                table.Add(new EncLogRow() { Token = token, FuncCode = EncFuncCode.Delta });
            }
        }

        private static void PopulateEncLogTableRows(
            List<EncLogRow> table,
            TableIndex tableIndex,
            ImmutableArray<int> previousSizes,
            ImmutableArray<int> deltaSizes)
        {
            PopulateEncLogTableRows(table, ((uint)tableIndex) << 24, (uint)previousSizes[(int)tableIndex] + 1, deltaSizes[(int)tableIndex]);
        }

        private static void PopulateEncLogTableRows(List<EncLogRow> table, uint tokenType, uint firstRowId, int nTokens)
        {
            for (int i = 0; i < nTokens; i++)
            {
                table.Add(new EncLogRow() { Token = tokenType | (firstRowId + (uint)i), FuncCode = EncFuncCode.Delta });
            }
        }

        protected override void PopulateEncMapTableRows(List<EncMapRow> table)
        {
            // The EncMap table maps from offset in each table in the delta
            // metadata to token. As such, the EncMap is a concatenated
            // list of all tokens in all tables from the delta sorted by table
            // and, within each table, sorted by row.
            var tokens = ArrayBuilder<uint>.GetInstance();
            var previousSizes = this.previousGeneration.TableSizes;
            var deltaSizes = this.GetDeltaTableSizes();

            AddReferencedTokens(tokens, TableIndex.AssemblyRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.ModuleRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.MemberRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.MethodSpec, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.TypeRef, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.TypeSpec, previousSizes, deltaSizes);
            AddReferencedTokens(tokens, TableIndex.StandAloneSig, previousSizes, deltaSizes);

            AddDefinitionTokens(tokens, this.typeDefs, TokenTypeIds.TypeDef);
            AddDefinitionTokens(tokens, this.eventDefs, TokenTypeIds.Event);
            AddDefinitionTokens(tokens, this.fieldDefs, TokenTypeIds.FieldDef);
            AddDefinitionTokens(tokens, this.methodDefs, TokenTypeIds.MethodDef);
            AddDefinitionTokens(tokens, this.propertyDefs, TokenTypeIds.Property);

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

            tokens.Sort();

            // Should not be any duplicates.
            Debug.Assert(tokens.Distinct().Count() == tokens.Count);

            foreach (var token in tokens)
            {
                table.Add(new EncMapRow() { Token = token });
            }

            tokens.Free();

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

            var tableSizes = new int[MetadataTokens.TableCount];
            this.GetTableSizes(tableSizes);
            for (uint i = 0; i < tableSizes.Length; i++)
            {
                if (handledTables.Contains((TableIndex)i))
                {
                    continue;
                }
                Debug.Assert(tableSizes[i] == 0);
            }
#endif
        }

        private static void AddReferencedTokens(
            ArrayBuilder<uint> builder,
            TableIndex tableIndex,
            ImmutableArray<int> previousSizes,
            ImmutableArray<int> deltaSizes)
        {
            AddReferencedTokens(builder, ((uint)tableIndex) << 24, (uint)previousSizes[(int)tableIndex] + 1, deltaSizes[(int)tableIndex]);
        }

        private static void AddReferencedTokens(ArrayBuilder<uint> builder, uint tokenType, uint firstRowId, int nTokens)
        {
            for (int i = 0; i < nTokens; i++)
            {
                builder.Add(tokenType | (firstRowId + (uint)i));
            }
        }

        private static void AddDefinitionTokens<T>(ArrayBuilder<uint> tokens, DefinitionIndex<T> index, uint tokenType)
            where T : IDefinition
        {
            foreach (var member in index.GetRows())
            {
                tokens.Add(tokenType | index[member]);
            }
        }

        protected override void PopulateEventMapTableRows(List<EventMapRow> table)
        {
            foreach (var typeId in this.eventMap.GetRows())
            {
                var r = new EventMapRow();
                r.Parent = typeId;
                r.EventList = this.eventMap[typeId];
                table.Add(r);
            }
        }

        protected override void PopulatePropertyMapTableRows(List<PropertyMapRow> table)
        {
            foreach (var typeId in this.propertyMap.GetRows())
            {
                var r = new PropertyMapRow();
                r.Parent = typeId;
                r.PropertyList = this.propertyMap[typeId];
                table.Add(r);
            }
        }

        private abstract class DefinitionIndexBase<T>
        {
            protected readonly Dictionary<T, uint> added; // Definitions added in this generation.
            protected readonly List<T> rows; // Rows in this generation, containing adds and updates.
            private readonly uint firstRowId; // First row in this generation.
            private bool frozen;

            public DefinitionIndexBase(uint lastRowId)
            {
                this.added = new Dictionary<T, uint>();
                this.rows = new List<T>();
                this.firstRowId = lastRowId + 1;
            }

            public abstract bool TryGetValue(T item, out uint index);

            public uint this[T item]
            {
                get
                {
                    uint token;
                    this.TryGetValue(item, out token);
                    Debug.Assert(token > 0);
                    return token;
                }
            }

            // A method rather than a property since it freezes the table.
            public IReadOnlyDictionary<T, uint> GetAdded()
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

            public uint FirstRowId
            {
                get { return this.firstRowId; }
            }

            public uint NextRowId
            {
                get { return (uint)this.added.Count + this.firstRowId; }
            }

            public bool IsFrozen
            {
                get { return this.frozen; }
            }

            protected virtual void OnFrozen()
            {
#if DEBUG
                // Verify the rows are sorted.
                uint prev = 0;
                foreach (var row in this.rows)
                {
                    var next = this.added[row];
                    Debug.Assert(prev < next);
                    prev = next;
                }
#endif
            }

            private void Freeze()
            {
                if (!this.frozen)
                {
                    this.frozen = true;
                    this.OnFrozen();
                }
            }
        }

        private sealed class DefinitionIndex<T> : DefinitionIndexBase<T> where T : IDefinition
        {
            public delegate bool TryGetExistingIndex(T item, out uint index);

            private readonly TryGetExistingIndex tryGetExistingIndex;
            // Map of row id to def for all defs. This could be an array indexed
            // by row id but the array could be large and sparsely populated
            // if there are many defs in the previous generation but few
            // references to those defs in the current generation.
            private readonly Dictionary<uint, T> map;

            public DefinitionIndex(TryGetExistingIndex tryGetExistingIndex, uint lastRowId) :
                base(lastRowId)
            {
                this.tryGetExistingIndex = tryGetExistingIndex;
                this.map = new Dictionary<uint, T>();
            }

            public override bool TryGetValue(T item, out uint index)
            {
                if (this.added.TryGetValue(item, out index))
                {
                    return true;
                }
                if (this.tryGetExistingIndex(item, out index))
                {
#if DEBUG
                    T other;
                    Debug.Assert(!this.map.TryGetValue(index, out other) || ((object)other == (object)item));
#endif
                    this.map[index] = item;
                    return true;
                }
                return false;
            }

            public T this[int index]
            {
                get
                {
                    uint rowId = (uint)index + 1;
                    return this.map[rowId];
                }
            }

            public void Add(T item)
            {
                Debug.Assert(!this.IsFrozen);

                uint index = this.NextRowId;
                this.added.Add(item, index);
                this.map[index] = item;
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
                int ix = (int)this[x];
                int iy = (int)this[y];
                return ix - iy;
            }
        }

        private bool TryGetExistingTypeDefIndex(ITypeDefinition item, out uint index)
        {
            if (this.previousGeneration.TypesAdded.TryGetValue(item, out index))
            {
                return true;
            }

            TypeHandle handle;
            if (this.definitionMap.TryGetTypeHandle(item, out handle))
            {
                index = (uint)MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingEventDefIndex(IEventDefinition item, out uint index)
        {
            if (this.previousGeneration.EventsAdded.TryGetValue(item, out index))
            {
                return true;
            }

            EventHandle handle;
            if (this.definitionMap.TryGetEventHandle(item, out handle))
            {
                index = (uint)MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingFieldDefIndex(IFieldDefinition item, out uint index)
        {
            if (this.previousGeneration.FieldsAdded.TryGetValue(item, out index))
            {
                return true;
            }

            FieldHandle handle;
            if (this.definitionMap.TryGetFieldHandle(item, out handle))
            {
                index = (uint)MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingMethodDefIndex(IMethodDefinition item, out uint index)
        {
            if (this.previousGeneration.MethodsAdded.TryGetValue(item, out index))
            {
                return true;
            }

            MethodHandle handle;
            if (this.definitionMap.TryGetMethodHandle(item, out handle))
            {
                index = (uint)MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingPropertyDefIndex(IPropertyDefinition item, out uint index)
        {
            if (this.previousGeneration.PropertiesAdded.TryGetValue(item, out index))
            {
                return true;
            }

            PropertyHandle handle;
            if (this.definitionMap.TryGetPropertyHandle(item, out handle))
            {
                index = (uint)MetadataTokens.GetRowNumber(handle);
                Debug.Assert(index > 0);
                return true;
            }

            index = 0;
            return false;
        }

        private bool TryGetExistingEventMapIndex(uint item, out uint index)
        {
            if (this.previousGeneration.EventMapAdded.TryGetValue(item, out index))
            {
                return true;
            }
            if (this.previousGeneration.TypeToEventMap.TryGetValue(item, out index))
            {
                return true;
            }
            index = 0;
            return false;
        }

        private bool TryGetExistingPropertyMapIndex(uint item, out uint index)
        {
            if (this.previousGeneration.PropertyMapAdded.TryGetValue(item, out index))
            {
                return true;
            }
            if (this.previousGeneration.TypeToPropertyMap.TryGetValue(item, out index))
            {
                return true;
            }
            index = 0;
            return false;
        }

        private bool TryGetExistingMethodImplIndex(MethodImplKey item, out uint index)
        {
            if (this.previousGeneration.MethodImplsAdded.TryGetValue(item, out index))
            {
                return true;
            }
            if (this.previousGeneration.MethodImpls.TryGetValue(item, out index))
            {
                return true;
            }
            index = 0;
            return false;
        }

        private sealed class ParameterDefinitionIndex : DefinitionIndexBase<IParameterDefinition>
        {
            public ParameterDefinitionIndex(uint lastRowId) :
                base(lastRowId)
            {
            }

            public override bool TryGetValue(IParameterDefinition item, out uint index)
            {
                return this.added.TryGetValue(item, out index);
            }

            public void Add(IParameterDefinition item)
            {
                Debug.Assert(!this.IsFrozen);

                uint index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class GenericParameterIndex : DefinitionIndexBase<IGenericParameter>
        {
            public GenericParameterIndex(uint lastRowId) :
                base(lastRowId)
            {
            }

            public override bool TryGetValue(IGenericParameter item, out uint index)
            {
                return this.added.TryGetValue(item, out index);
            }

            public void Add(IGenericParameter item)
            {
                Debug.Assert(!this.IsFrozen);

                uint index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class EventOrPropertyMapIndex : DefinitionIndexBase<uint>
        {
            public delegate bool TryGetExistingIndex(uint item, out uint index);

            private readonly TryGetExistingIndex tryGetExistingIndex;

            public EventOrPropertyMapIndex(TryGetExistingIndex tryGetExistingIndex, uint lastRowId) :
                base(lastRowId)
            {
                this.tryGetExistingIndex = tryGetExistingIndex;
            }

            public override bool TryGetValue(uint item, out uint index)
            {
                if (this.added.TryGetValue(item, out index))
                {
                    return true;
                }
                if (this.tryGetExistingIndex(item, out index))
                {
                    return true;
                }
                index = 0;
                return false;
            }

            public void Add(uint item)
            {
                Debug.Assert(!this.IsFrozen);

                uint index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class MethodImplIndex : DefinitionIndexBase<MethodImplKey>
        {
            private readonly DeltaPeWriter writer;

            public MethodImplIndex(DeltaPeWriter writer, uint lastRowId) :
                base(lastRowId)
            {
                this.writer = writer;
            }

            public override bool TryGetValue(MethodImplKey item, out uint index)
            {
                if (this.added.TryGetValue(item, out index))
                {
                    return true;
                }
                if (this.writer.TryGetExistingMethodImplIndex(item, out index))
                {
                    return true;
                }
                index = 0;
                return false;
            }

            public void Add(MethodImplKey item)
            {
                Debug.Assert(!this.IsFrozen);

                uint index = this.NextRowId;
                this.added.Add(item, index);
                this.rows.Add(item);
            }
        }

        private sealed class DeltaReferenceIndexer : ReferenceIndexer
        {
            private readonly SymbolChanges changes;

            public DeltaReferenceIndexer(DeltaPeWriter writer) :
                base(writer)
            {
                this.changes = writer.changes;
            }

            public override void Visit(IAssembly assembly)
            {
                this.Visit((IModule)assembly);
            }

            public override void Visit(IModule module)
            {
                this.module = module;
                this.Visit(((DeltaPeWriter)this.peWriter).GetTopLevelTypes(module));
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

            public override void Visit(IMethodDefinition method)
            {
                Debug.Assert(this.ShouldVisit(method));
                base.Visit(method);
            }

            public override void Visit(IMethodImplementation methodImplementation)
            {
                // Unless the implementing method was added,
                // the method implementation already exists.
                var methodDef = (IMethodDefinition)methodImplementation.ImplementingMethod.AsDefinition(this.Context);
                if (this.changes.GetChange(methodDef) == SymbolChange.Added)
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
                return this.changes.GetChange(def) != SymbolChange.None;
            }
        }
    }
}
