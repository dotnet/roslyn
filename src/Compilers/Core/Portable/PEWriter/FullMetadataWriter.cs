// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal sealed class FullMetadataWriter : MetadataWriter
    {
        private readonly DefinitionIndex<ITypeDefinition> _typeDefs;
        private readonly DefinitionIndex<IEventDefinition> _eventDefs;
        private readonly DefinitionIndex<IFieldDefinition> _fieldDefs;
        private readonly DefinitionIndex<IMethodDefinition> _methodDefs;
        private readonly DefinitionIndex<IPropertyDefinition> _propertyDefs;
        private readonly DefinitionIndex<IParameterDefinition> _parameterDefs;
        private readonly DefinitionIndex<IGenericParameter> _genericParameters;

        private readonly Dictionary<ITypeDefinition, int> _fieldDefIndex;
        private readonly Dictionary<ITypeDefinition, int> _methodDefIndex;
        private readonly SegmentedDictionary<IMethodDefinition, int> _parameterListIndex;

        private readonly HeapOrReferenceIndex<AssemblyIdentity> _assemblyRefIndex;
        private readonly HeapOrReferenceIndex<string> _moduleRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeMemberReference> _memberRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference> _methodSpecIndex;
        private readonly TypeReferenceIndex _typeRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeReference> _typeSpecIndex;
        private readonly HeapOrReferenceIndex<BlobHandle> _standAloneSignatureIndex;

        public static MetadataWriter Create(
            EmitContext context,
            CommonMessageProvider messageProvider,
            bool metadataOnly,
            bool deterministic,
            bool emitTestCoverageData,
            bool hasPdbStream,
            CancellationToken cancellationToken)
        {
            var builder = new MetadataBuilder();
            MetadataBuilder? debugBuilderOpt;
            switch (context.Module.DebugInformationFormat)
            {
                case DebugInformationFormat.PortablePdb:
                    debugBuilderOpt = hasPdbStream ? new MetadataBuilder() : null;
                    break;

                case DebugInformationFormat.Embedded:
                    debugBuilderOpt = metadataOnly ? null : new MetadataBuilder();
                    break;

                default:
                    debugBuilderOpt = null;
                    break;
            }

            var dynamicAnalysisDataWriterOpt = emitTestCoverageData ?
                new DynamicAnalysisDataWriter(context.Module.DebugDocumentCount, context.Module.HintNumberOfMethodDefinitions) :
                null;

            return new FullMetadataWriter(context, builder, debugBuilderOpt, dynamicAnalysisDataWriterOpt, messageProvider, metadataOnly, deterministic,
                emitTestCoverageData, cancellationToken);
        }

        private FullMetadataWriter(
            EmitContext context,
            MetadataBuilder builder,
            MetadataBuilder? debugBuilderOpt,
            DynamicAnalysisDataWriter? dynamicAnalysisDataWriterOpt,
            CommonMessageProvider messageProvider,
            bool metadataOnly,
            bool deterministic,
            bool emitTestCoverageData,
            CancellationToken cancellationToken)
            : base(builder, debugBuilderOpt, dynamicAnalysisDataWriterOpt, context, messageProvider, metadataOnly, deterministic,
                  emitTestCoverageData, cancellationToken)
        {
            // EDMAURER make some intelligent guesses for the initial sizes of these things.
            int numMethods = this.module.HintNumberOfMethodDefinitions;
            int numTypeDefsGuess = numMethods / 6;
            int numFieldDefsGuess = numTypeDefsGuess * 4;
            int numPropertyDefsGuess = numMethods / 4;

            _typeDefs = new DefinitionIndex<ITypeDefinition>(numTypeDefsGuess);
            _eventDefs = new DefinitionIndex<IEventDefinition>(0);
            _fieldDefs = new DefinitionIndex<IFieldDefinition>(numFieldDefsGuess);
            _methodDefs = new DefinitionIndex<IMethodDefinition>(numMethods);
            _propertyDefs = new DefinitionIndex<IPropertyDefinition>(numPropertyDefsGuess);
            _parameterDefs = new DefinitionIndex<IParameterDefinition>(numMethods);
            _genericParameters = new DefinitionIndex<IGenericParameter>(0);

            _fieldDefIndex = new Dictionary<ITypeDefinition, int>(numTypeDefsGuess, ReferenceEqualityComparer.Instance);
            _methodDefIndex = new Dictionary<ITypeDefinition, int>(numTypeDefsGuess, ReferenceEqualityComparer.Instance);
            _parameterListIndex = new SegmentedDictionary<IMethodDefinition, int>(numMethods, ReferenceEqualityComparer.Instance);

            _assemblyRefIndex = new HeapOrReferenceIndex<AssemblyIdentity>(this);
            _moduleRefIndex = new HeapOrReferenceIndex<string>(this);
            _memberRefIndex = new InstanceAndStructuralReferenceIndex<ITypeMemberReference>(this, new MemberRefComparer(this));
            _methodSpecIndex = new InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference>(this, new MethodSpecComparer(this));
            _typeRefIndex = new TypeReferenceIndex(this);
            _typeSpecIndex = new InstanceAndStructuralReferenceIndex<ITypeReference>(this, new TypeSpecComparer(this));
            _standAloneSignatureIndex = new HeapOrReferenceIndex<BlobHandle>(this);
        }

        protected override ushort Generation
        {
            get { return 0; }
        }

        protected override Guid EncId
        {
            get { return Guid.Empty; }
        }

        protected override Guid EncBaseId
        {
            get { return Guid.Empty; }
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
            return _typeDefs.Rows;
        }

        protected override EventDefinitionHandle GetEventDefinitionHandle(IEventDefinition def)
        {
            return MetadataTokens.EventDefinitionHandle(_eventDefs[def]);
        }

        protected override IReadOnlyList<IEventDefinition> GetEventDefs()
        {
            return _eventDefs.Rows;
        }

        protected override FieldDefinitionHandle GetFieldDefinitionHandle(IFieldDefinition def)
        {
            return MetadataTokens.FieldDefinitionHandle(_fieldDefs[def]);
        }

        protected override IReadOnlyList<IFieldDefinition> GetFieldDefs()
        {
            return _fieldDefs.Rows;
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

        protected override IMethodDefinition GetMethodDef(MethodDefinitionHandle handle)
        {
            return _methodDefs[MetadataTokens.GetRowNumber(handle)];
        }

        protected override IReadOnlyList<IMethodDefinition> GetMethodDefs()
        {
            return _methodDefs.Rows;
        }

        protected override PropertyDefinitionHandle GetPropertyDefIndex(IPropertyDefinition def)
        {
            return MetadataTokens.PropertyDefinitionHandle(_propertyDefs[def]);
        }

        protected override IReadOnlyList<IPropertyDefinition> GetPropertyDefs()
        {
            return _propertyDefs.Rows;
        }

        protected override ParameterHandle GetParameterHandle(IParameterDefinition def)
        {
            return MetadataTokens.ParameterHandle(_parameterDefs[def]);
        }

        protected override IReadOnlyList<IParameterDefinition> GetParameterDefs()
        {
            return _parameterDefs.Rows;
        }

        protected override IReadOnlyList<IGenericParameter> GetGenericParameters()
        {
            return _genericParameters.Rows;
        }

        protected override FieldDefinitionHandle GetFirstFieldDefinitionHandle(INamedTypeDefinition typeDef)
        {
            return MetadataTokens.FieldDefinitionHandle(_fieldDefIndex[typeDef]);
        }

        protected override MethodDefinitionHandle GetFirstMethodDefinitionHandle(INamedTypeDefinition typeDef)
        {
            return MetadataTokens.MethodDefinitionHandle(_methodDefIndex[typeDef]);
        }

        protected override ParameterHandle GetFirstParameterHandle(IMethodDefinition methodDef)
        {
            return MetadataTokens.ParameterHandle(_parameterListIndex[methodDef]);
        }

        protected override AssemblyReferenceHandle GetOrAddAssemblyReferenceHandle(IAssemblyReference reference)
        {
            return MetadataTokens.AssemblyReferenceHandle(_assemblyRefIndex.GetOrAdd(reference.Identity));
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

        protected override ReferenceIndexer CreateReferenceVisitor()
        {
            return new FullReferenceIndexer(this);
        }

        protected override void ReportReferencesToAddedSymbols()
        {
            // noop
        }

        private sealed class FullReferenceIndexer : ReferenceIndexer
        {
            internal FullReferenceIndexer(MetadataWriter metadataWriter)
                : base(metadataWriter)
            {
            }
        }

        protected override void PopulateEventMapTableRows()
        {
            ITypeDefinition? lastParent = null;
            foreach (IEventDefinition eventDef in this.GetEventDefs())
            {
                if (eventDef.ContainingTypeDefinition == lastParent)
                {
                    continue;
                }

                lastParent = eventDef.ContainingTypeDefinition;

                metadata.AddEventMap(
                    declaringType: GetTypeDefinitionHandle(lastParent),
                    eventList: GetEventDefinitionHandle(eventDef));
            }
        }

        protected override void PopulatePropertyMapTableRows()
        {
            ITypeDefinition? lastParent = null;
            foreach (IPropertyDefinition propertyDef in this.GetPropertyDefs())
            {
                if (propertyDef.ContainingTypeDefinition == lastParent)
                {
                    continue;
                }

                lastParent = propertyDef.ContainingTypeDefinition;

                metadata.AddPropertyMap(
                    declaringType: GetTypeDefinitionHandle(lastParent),
                    propertyList: GetPropertyDefIndex(propertyDef));
            }
        }

        protected override void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef)
        {
            _typeDefs.Add(typeDef);

            IEnumerable<IGenericTypeParameter> typeParameters = this.GetConsolidatedTypeParameters(typeDef);
            if (typeParameters != null)
            {
                foreach (IGenericTypeParameter genericParameter in typeParameters)
                {
                    _genericParameters.Add(genericParameter);
                }
            }

            foreach (MethodImplementation methodImplementation in typeDef.GetExplicitImplementationOverrides(Context))
            {
                this.methodImplList.Add(methodImplementation);
            }

            foreach (IEventDefinition eventDef in typeDef.GetEvents(Context))
            {
                _eventDefs.Add(eventDef);
            }

            _fieldDefIndex.Add(typeDef, _fieldDefs.NextRowId);
            foreach (IFieldDefinition fieldDef in typeDef.GetFields(Context))
            {
                _fieldDefs.Add(fieldDef);
            }

            _methodDefIndex.Add(typeDef, _methodDefs.NextRowId);
            foreach (IMethodDefinition methodDef in typeDef.GetMethods(Context))
            {
                this.CreateIndicesFor(methodDef);
                _methodDefs.Add(methodDef);
            }

            foreach (IPropertyDefinition propertyDef in typeDef.GetProperties(Context))
            {
                _propertyDefs.Add(propertyDef);
            }
        }

        private void CreateIndicesFor(IMethodDefinition methodDef)
        {
            _parameterListIndex.Add(methodDef, _parameterDefs.NextRowId);

            foreach (var paramDef in this.GetParametersToEmit(methodDef))
            {
                _parameterDefs.Add(paramDef);
            }

            if (methodDef.GenericParameterCount > 0)
            {
                foreach (IGenericMethodParameter genericParameter in methodDef.GenericParameters)
                {
                    _genericParameters.Add(genericParameter);
                }
            }
        }

        private readonly struct DefinitionIndex<T> where T : class, IReference
        {
            // IReference to RowId
            private readonly SegmentedDictionary<T, int> _index;
            private readonly SegmentedList<T> _rows;

            public DefinitionIndex(int capacity)
            {
                _index = new SegmentedDictionary<T, int>(capacity, ReferenceEqualityComparer.Instance);
                _rows = new SegmentedList<T>(capacity);
            }

            public bool TryGetValue(T item, out int rowId)
            {
                return _index.TryGetValue(item, out rowId);
            }

            public int this[T item]
            {
                get { return _index[item]; }
            }

            public T this[int rowId]
            {
                get { return _rows[rowId - 1]; }
            }

            public IReadOnlyList<T> Rows
            {
                get { return _rows; }
            }

            public int NextRowId
            {
                get { return _rows.Count + 1; }
            }

            public void Add(T item)
            {
                _index.Add(item, NextRowId);
                _rows.Add(item);
            }
        }
    }
}
