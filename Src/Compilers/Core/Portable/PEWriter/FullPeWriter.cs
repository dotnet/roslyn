// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal sealed class FullPeWriter : PeWriter
    {
        private readonly DefinitionIndex<ITypeDefinition> typeDefs;
        private readonly DefinitionIndex<IEventDefinition> eventDefs;
        private readonly DefinitionIndex<IFieldDefinition> fieldDefs;
        private readonly DefinitionIndex<IMethodDefinition> methodDefs;
        private readonly DefinitionIndex<IPropertyDefinition> propertyDefs;
        private readonly DefinitionIndex<IParameterDefinition> parameterDefs;
        private readonly DefinitionIndex<IGenericParameter> genericParameters;

        private readonly Dictionary<ITypeDefinition, uint> fieldDefIndex;
        private readonly Dictionary<ITypeDefinition, uint> methodDefIndex;
        private readonly Dictionary<IMethodDefinition, uint> parameterListIndex;

        private readonly HeapOrReferenceIndex<IAssemblyReference> assemblyRefIndex;
        private readonly HeapOrReferenceIndex<string> moduleRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeMemberReference> memberRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference> methodSpecIndex;
        private readonly HeapOrReferenceIndex<ITypeReference> typeRefIndex;
        private readonly InstanceAndStructuralReferenceIndex<ITypeReference> typeSpecIndex;
        private readonly HeapOrReferenceIndex<uint> standAloneSignatureIndex;

        public FullPeWriter(
            EmitContext context,
            CommonMessageProvider messageProvider,
            PdbWriter pdbWriter,
            bool allowMissingMethodBodies,
            bool foldIdenticalMethodBodies,
            bool deterministic,
            CancellationToken cancellationToken) :
            base(context, messageProvider, pdbWriter, allowMissingMethodBodies, foldIdenticalMethodBodies, deterministic, cancellationToken)
        {
            // EDMAURER make some intelligent guesses for the initial sizes of these things.
            int numMethods = this.module.HintNumberOfMethodDefinitions;
            int numTypeDefsGuess = numMethods / 6;
            int numFieldDefsGuess = numTypeDefsGuess * 4;
            int numPropertyDefsGuess = numMethods / 4;

            this.typeDefs = new DefinitionIndex<ITypeDefinition>(numTypeDefsGuess);
            this.eventDefs = new DefinitionIndex<IEventDefinition>(0);
            this.fieldDefs = new DefinitionIndex<IFieldDefinition>(numFieldDefsGuess);
            this.methodDefs = new DefinitionIndex<IMethodDefinition>(numMethods);
            this.propertyDefs = new DefinitionIndex<IPropertyDefinition>(numPropertyDefsGuess);
            this.parameterDefs = new DefinitionIndex<IParameterDefinition>(numMethods);
            this.genericParameters = new DefinitionIndex<IGenericParameter>(0);

            this.fieldDefIndex = new Dictionary<ITypeDefinition, uint>(numTypeDefsGuess);
            this.methodDefIndex = new Dictionary<ITypeDefinition, uint>(numTypeDefsGuess);
            this.parameterListIndex = new Dictionary<IMethodDefinition, uint>(numMethods);

            this.assemblyRefIndex = new HeapOrReferenceIndex<IAssemblyReference>(this, AssemblyReferenceComparer.Instance);
            this.moduleRefIndex = new HeapOrReferenceIndex<string>(this);
            this.memberRefIndex = new InstanceAndStructuralReferenceIndex<ITypeMemberReference>(this, new MemberRefComparer(this));
            this.methodSpecIndex = new InstanceAndStructuralReferenceIndex<IGenericMethodInstanceReference>(this, new MethodSpecComparer(this));
            this.typeRefIndex = new HeapOrReferenceIndex<ITypeReference>(this);
            this.typeSpecIndex = new InstanceAndStructuralReferenceIndex<ITypeReference>(this, new TypeSpecComparer(this));
            this.standAloneSignatureIndex = new HeapOrReferenceIndex<uint>(this);
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

        protected override bool CompressMetadataStream
        {
            get { return true; }
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
            return this.typeDefs.Rows;
        }

        protected override uint GetEventDefIndex(IEventDefinition def)
        {
            return this.eventDefs[def];
        }

        protected override IReadOnlyList<IEventDefinition> GetEventDefs()
        {
            return this.eventDefs.Rows;
        }

        protected override uint GetFieldDefIndex(IFieldDefinition def)
        {
            return this.fieldDefs[def];
        }

        protected override IReadOnlyList<IFieldDefinition> GetFieldDefs()
        {
            return this.fieldDefs.Rows;
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
            return this.methodDefs.Rows;
        }

        protected override uint GetPropertyDefIndex(IPropertyDefinition def)
        {
            return this.propertyDefs[def];
        }

        protected override IReadOnlyList<IPropertyDefinition> GetPropertyDefs()
        {
            return this.propertyDefs.Rows;
        }

        protected override uint GetParameterDefIndex(IParameterDefinition def)
        {
            return this.parameterDefs[def];
        }

        protected override IReadOnlyList<IParameterDefinition> GetParameterDefs()
        {
            return this.parameterDefs.Rows;
        }

        protected override uint GetGenericParameterIndex(IGenericParameter def)
        {
            return this.genericParameters[def];
        }

        protected override IReadOnlyList<IGenericParameter> GetGenericParameters()
        {
            return this.genericParameters.Rows;
        }

        protected override uint GetFieldDefIndex(INamedTypeDefinition typeDef)
        {
            return this.fieldDefIndex[typeDef];
        }

        protected override uint GetMethodDefIndex(INamedTypeDefinition typeDef)
        {
            return this.methodDefIndex[typeDef];
        }

        protected override uint GetParameterDefIndex(IMethodDefinition methodDef)
        {
            return this.parameterListIndex[methodDef];
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

        protected override uint GetBlobStreamOffset()
        {
            return 0;
        }

        protected override uint GetStringStreamOffset()
        {
            return 0;
        }

        protected override uint GetUserStringStreamOffset()
        {
            return 0;
        }

        protected override ReferenceIndexer CreateReferenceVisitor()
        {
            return new FullReferenceIndexer(this);
        }

        private sealed class FullReferenceIndexer : ReferenceIndexer
        {
            internal FullReferenceIndexer(PeWriter peWriter)
                : base(peWriter)
            {
            }
        }

        protected override void PopulateEncLogTableRows(List<EncLogRow> table, ImmutableArray<int> rowCounts)
        {
        }

        protected override void PopulateEncMapTableRows(List<EncMapRow> table, ImmutableArray<int> rowCounts)
        {
        }

        protected override void PopulateEventMapTableRows(List<EventMapRow> table)
        {
            ITypeDefinition lastParent = null;
            foreach (IEventDefinition eventDef in this.GetEventDefs())
            {
                if (eventDef.ContainingTypeDefinition == lastParent)
                {
                    continue;
                }

                lastParent = eventDef.ContainingTypeDefinition;
                uint eventIndex = this.GetEventDefIndex(eventDef);
                EventMapRow r = new EventMapRow();
                r.Parent = this.GetTypeDefIndex(lastParent);
                r.EventList = eventIndex;
                table.Add(r);
            }
        }

        protected override void PopulatePropertyMapTableRows(List<PropertyMapRow> table)
        {
            ITypeDefinition lastParent = null;
            foreach (IPropertyDefinition propertyDef in this.GetPropertyDefs())
            {
                if (propertyDef.ContainingTypeDefinition == lastParent)
                {
                    continue;
                }

                lastParent = propertyDef.ContainingTypeDefinition;
                uint propertyIndex = this.GetPropertyDefIndex(propertyDef);
                PropertyMapRow r = new PropertyMapRow();
                r.Parent = this.GetTypeDefIndex(lastParent);
                r.PropertyList = propertyIndex;
                table.Add(r);
            }
        }

        protected override IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(IModule module)
        {
            return module.GetTopLevelTypes(this.Context);
        }

        protected override void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef)
        {
            this.typeDefs.Add(typeDef);

            IEnumerable<IGenericTypeParameter> typeParameters = this.GetConsolidatedTypeParameters(typeDef);
            if (typeParameters != null)
            {
                foreach (IGenericTypeParameter genericParameter in typeParameters)
                {
                    this.genericParameters.Add(genericParameter);
                }
            }

            foreach (MethodImplementation methodImplementation in typeDef.GetExplicitImplementationOverrides(Context))
            {
                this.methodImplList.Add(methodImplementation);
            }

            foreach (IEventDefinition eventDef in typeDef.Events)
            {
                this.eventDefs.Add(eventDef);
            }

            this.fieldDefIndex.Add(typeDef, this.fieldDefs.NextRowId);
            foreach (IFieldDefinition fieldDef in typeDef.GetFields(Context))
            {
                this.fieldDefs.Add(fieldDef);
            }

            this.methodDefIndex.Add(typeDef, this.methodDefs.NextRowId);
            foreach (IMethodDefinition methodDef in typeDef.GetMethods(Context))
            {
                this.CreateIndicesFor(methodDef);
                this.methodDefs.Add(methodDef);
            }

            foreach (IPropertyDefinition propertyDef in typeDef.GetProperties(Context))
            {
                this.propertyDefs.Add(propertyDef);
            }
        }

        private void CreateIndicesFor(IMethodDefinition methodDef)
        {
            this.parameterListIndex.Add(methodDef, this.parameterDefs.NextRowId);

            foreach (var paramDef in this.GetParametersToEmit(methodDef))
            {
                this.parameterDefs.Add(paramDef);
            }

            if (methodDef.GenericParameterCount > 0)
            {
                foreach (IGenericMethodParameter genericParameter in methodDef.GenericParameters)
                {
                    this.genericParameters.Add(genericParameter);
                }
            }
        }

        private struct DefinitionIndex<T> where T : IReference
        {
            private readonly Dictionary<T, uint> index;
            private readonly List<T> rows;

            public DefinitionIndex(int capacity)
            {
                this.index = new Dictionary<T, uint>(capacity);
                this.rows = new List<T>(capacity);
            }

            public bool TryGetValue(T item, out uint index)
            {
                return this.index.TryGetValue(item, out index);
            }

            public uint this[T item]
            {
                get { return this.index[item]; }
            }

            public T this[int index]
            {
                get { return this.rows[index]; }
            }

            public IReadOnlyList<T> Rows
            {
                get { return this.rows; }
            }

            public uint NextRowId
            {
                get { return (uint)this.rows.Count + 1; }
            }

            public void Add(T item)
            {
                this.index.Add(item, NextRowId);
                this.rows.Add(item);
            }
        }
    }
}
