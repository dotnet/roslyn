using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata.Ecma335.Tokens;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class InitialPeGeneration : PeGeneration
    {
        private readonly Guid moduleVersionId;
        private readonly PEFileReader reader;
        private readonly Dictionary<string, TypeFromMetadata> typeDefs; // TODO: Index by namespace + name. By MetadataTypeName?

        public InitialPeGeneration(Guid moduleVersionId, PEFileReader reader)
        {
            this.moduleVersionId = moduleVersionId;
            this.reader = reader;
            this.typeDefs = GetTypeDefinitions(reader);
        }

        internal override PeGeneration PreviousGeneration
        {
            get { return null; }
        }

        internal override ushort Ordinal
        {
            get { return 0; }
        }

        internal override Guid ModuleVersionId
        {
            get { return this.moduleVersionId; }
        }

        internal override Guid EncId
        {
            get { return default(Guid); }
        }

        internal override int BlobStreamLength
        {
            get { return this.reader.BlobStreamLength; }
        }

        internal override int StringStreamLength
        {
            get { return this.reader.StringStreamLength; }
        }

        internal override int UserStringStreamLength
        {
            get { return this.reader.UserStringStreamLength; }
        }

        internal override bool TryGetType(ITypeDefinition def, out uint index)
        {
            TypeFromMetadata type;
            if (!this.TryGetTypeDefinition(def, out type))
            {
                index = 0;
                return false;
            }
            index = type.RowId;
            return true;
        }

        internal override bool TryGetEvent(IEventDefinition def, out uint index)
        {
            TypeFromMetadata type;
            if (this.TryGetTypeDefinition(def.ContainingTypeDefinition, out type))
            {
                EventHandle handle;
                if (type.TryGetEvent(def.Name, out handle))
                {
                    index = (uint)handle.GetRowNumber();
                    return true;
                }
            }
            index = 0;
            return false;
        }

        internal override bool TryGetField(IFieldDefinition def, out uint index)
        {
            TypeFromMetadata type;
            if (this.TryGetTypeDefinition(def.ContainingTypeDefinition, out type))
            {
                FieldHandle handle;
                if (type.TryGetField(def.Name, out handle))
                {
                    index = (uint)handle.GetRowNumber();
                    return true;
                }
            }
            index = 0;
            return false;
        }

        internal override bool TryGetMethod(IMethodDefinition def, out uint index)
        {
            TypeFromMetadata type;
            if (this.TryGetTypeDefinition(def.ContainingTypeDefinition, out type))
            {
                MethodHandle handle;
                if (type.TryGetMethod(def.Name, out handle))
                {
                    index = (uint)handle.GetRowNumber();
                    return true;
                }
            }
            index = 0;
            return false;
        }

        internal override bool TryGetProperty(IPropertyDefinition def, out uint index)
        {
            TypeFromMetadata type;
            if (this.TryGetTypeDefinition(def.ContainingTypeDefinition, out type))
            {
                PropertyHandle handle;
                if (type.TryGetProperty(def.Name, out handle))
                {
                    index = (uint)handle.GetRowNumber();
                    return true;
                }
            }
            index = 0;
            return false;
        }

        internal override int TypeDefinitionCount
        {
            get { return this.reader.TypeDefinitions.Count; }
        }

        internal override int EventDefinitionCount
        {
            get { return this.reader.EventDefinitions.Count; }
        }

        internal override int FieldDefinitionCount
        {
            get { return this.reader.FieldDefinitions.Count; }
        }

        internal override int MethodDefinitionCount
        {
            get { return this.reader.MethodDefinitions.Count; }
        }

        internal override int PropertyDefinitionCount
        {
            get { return this.reader.PropertyDefinitions.Count; }
        }

        internal override int ParameterDefinitionCount
        {
            get { return (int)this.reader.ParameterDefinitionCount; }
        }

        internal override int AssemblyReferenceCount
        {
            get { return this.reader.AssemblyReferences.Count; }
        }

        internal override int MemberReferenceCount
        {
            get { return this.reader.MemberReferences.Count; }
        }

        internal override int MethodSpecificationCount
        {
            get { return (int)this.reader.MethodSpecificationCount; }
        }

        internal override int TypeReferenceCount
        {
            get { return this.reader.TypeReferences.Count; }
        }

        internal override int TypeSpecificationCount
        {
            get { return (int)this.reader.TypeSpecificationCount; }
        }

        internal override int StandAloneSignatureCount
        {
            get { return (int)this.reader.StandAloneSignatureCount; }
        }

        internal override int ConstantCount
        {
            get { return (int)this.reader.ConstantCount; }
        }

        internal override int CustomAttributeCount
        {
            get { return (int)this.reader.CustomAttributeCount; }
        }

        internal override int DeclSecurityCount
        {
            get { return (int)this.reader.DeclSecurityCount; }
        }

        internal override int GetLocalCount(IMethodDefinition def)
        {
            TypeFromMetadata type;
            var ok = this.TryGetTypeDefinition(def.ContainingTypeDefinition, out type);
            Debug.Assert(ok);

            MethodHandle handle;
            ok = type.TryGetMethod(def.Name, out handle);
            Debug.Assert(ok);

            var methodIL = this.reader.GetMethodIL(handle);

            var localsHandle = methodIL.LocalSignature;
            if (localsHandle.IsNil)
            {
                return 0;
            }
            else
            {
                var signatureReader = this.reader.GetReader(localsHandle);
                var callingConvention = signatureReader.ReadByte();
                return (int)signatureReader.ReadCompressedUInt32();
            }
        }

        internal override PeGeneration CreateNextGeneration(
            Guid encId,
            ReadOnlyArray<KeyValuePair<ITypeDefinition, uint>> typesAdded,
            ReadOnlyArray<KeyValuePair<IEventDefinition, uint>> eventsAdded,
            ReadOnlyArray<KeyValuePair<IFieldDefinition, uint>> fieldsAdded,
            ReadOnlyArray<KeyValuePair<IMethodDefinition, uint>> methodsAdded,
            ReadOnlyArray<KeyValuePair<IPropertyDefinition, uint>> propertiesAdded,
            int parameterDefsAdded,
            int assemblyRefsAdded,
            int memberRefsAdded,
            int methodSpecsAdded,
            int typeRefsAdded,
            int typeSpecsAdded,
            int standAloneSigsAdded,
            int constantsAdded,
            int customAttributesAdded,
            int declSecurityAdded,
            int blobStreamLengthAdded,
            int stringStreamLengthAdded,
            int userStringStreamLengthAdded,
            IReadOnlyDictionary<IMethodDefinition, uint> methodLocalCounts)
        {
            return new DeltaPeGeneration(
                this,
                1,
                encId,
                typesAdded,
                eventsAdded,
                fieldsAdded,
                methodsAdded,
                propertiesAdded,
                parameterDefCount: this.ParameterDefinitionCount + parameterDefsAdded,
                assemblyRefCount: this.AssemblyReferenceCount + assemblyRefsAdded,
                memberRefCount: this.MemberReferenceCount + memberRefsAdded,
                methodSpecCount: this.MethodSpecificationCount + methodSpecsAdded,
                typeRefCount: this.TypeReferenceCount + typeRefsAdded,
                typeSpecCount: this.TypeSpecificationCount + typeSpecsAdded,
                standAloneSigCount: this.StandAloneSignatureCount + standAloneSigsAdded,
                constantCount: this.ConstantCount + constantsAdded,
                customAttributeCount: this.CustomAttributeCount + customAttributesAdded,
                declSecurityCount: this.DeclSecurityCount + declSecurityAdded,
                blobStreamLength: this.BlobStreamLength + blobStreamLengthAdded,
                stringStreamLength: this.StringStreamLength + stringStreamLengthAdded,
                userStringStreamLength: this.UserStringStreamLength + userStringStreamLengthAdded,
                methodLocalCounts: methodLocalCounts);
        }

        private bool TryGetTypeDefinition(ITypeDefinition def, out TypeFromMetadata type)
        {
            var name = ((INamedTypeDefinition)def).Name; // TODO: Index by namespace + name. By MetadataTypeName?
            return this.typeDefs.TryGetValue(name, out type);
        }

        private static Dictionary<string, TypeFromMetadata> GetTypeDefinitions(PEFileReader reader)
        {
            var types = new Dictionary<string, TypeFromMetadata>();
            foreach (var typeHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeHandle);
                var name = reader.GetString(typeDef.Name); // TODO: Index by namespace + name. By MetadataTypeName?
                types.Add(name, new TypeFromMetadata(reader, typeHandle));
            }
            return types;
        }

        private static Dictionary<string, EventHandle> GetEventDefs(PEFileReader reader, TypeHandle typeHandle)
        {
            var events = new Dictionary<string, EventHandle>();
            foreach (var eventHandle in reader.GetTypeDefinition(typeHandle).GetEvents())
            {
                var eventDef = reader.GetEvent(eventHandle);
                var name = reader.GetString(eventDef.Name);
                events.Add(name, eventHandle);
            }
            return events;
        }

        private static Dictionary<string, FieldHandle> GetFieldDefs(PEFileReader reader, TypeHandle typeHandle)
        {
            var fields = new Dictionary<string, FieldHandle>();
            foreach (var fieldHandle in reader.GetTypeDefinition(typeHandle).GetFields())
            {
                var fieldDef = reader.GetField(fieldHandle);
                var name = reader.GetString(fieldDef.Name);
                fields.Add(name, fieldHandle);
            }
            return fields;
        }

        // TODO: Index by signature.
        private static Dictionary<string, MethodHandle> GetMethodDefs(PEFileReader reader, TypeHandle typeHandle)
        {
            var methods = new Dictionary<string, MethodHandle>();
            foreach (var methodHandle in reader.GetTypeDefinition(typeHandle).GetMethods())
            {
                var methodDef = reader.GetMethod(methodHandle);
                var name = reader.GetString(methodDef.Name);
                methods.Add(name, methodHandle);
            }
            return methods;
        }

        private static Dictionary<string, PropertyHandle> GetPropertyDefs(PEFileReader reader, TypeHandle typeHandle)
        {
            var properties = new Dictionary<string, PropertyHandle>();
            foreach (var propertyHandle in reader.GetTypeDefinition(typeHandle).GetProperties())
            {
                var propertyDef = reader.GetProperty(propertyHandle);
                var name = reader.GetString(propertyDef.Name);
                properties.Add(name, propertyHandle);
            }
            return properties;
        }

        private sealed class TypeFromMetadata
        {
            private readonly PEFileReader reader;
            private readonly TypeHandle typeHandle;
            private Dictionary<string, EventHandle> eventDefs;
            private Dictionary<string, FieldHandle> fieldDefs;
            private Dictionary<string, MethodHandle> methodDefs; // TODO: Index by signature
            private Dictionary<string, PropertyHandle> propertyDefs;

            public TypeFromMetadata(PEFileReader reader, TypeHandle typeHandle)
            {
                this.reader = reader;
                this.typeHandle = typeHandle;
            }

            public uint RowId
            {
                get { return (uint)this.typeHandle.GetRowNumber(); }
            }

            public bool TryGetEvent(string name, out EventHandle handle)
            {
                if (this.eventDefs == null)
                {
                    this.eventDefs = GetEventDefs(this.reader, this.typeHandle);
                }
                return this.eventDefs.TryGetValue(name, out handle);
            }

            public bool TryGetField(string name, out FieldHandle handle)
            {
                if (this.fieldDefs == null)
                {
                    this.fieldDefs = GetFieldDefs(this.reader, this.typeHandle);
                }
                return this.fieldDefs.TryGetValue(name, out handle);
            }

            // TODO: Index by signature.
            public bool TryGetMethod(string name, out MethodHandle handle)
            {
                if (this.methodDefs == null)
                {
                    this.methodDefs = GetMethodDefs(this.reader, this.typeHandle);
                }
                return this.methodDefs.TryGetValue(name, out handle);
            }

            public bool TryGetProperty(string name, out PropertyHandle handle)
            {
                if (this.propertyDefs == null)
                {
                    this.propertyDefs = GetPropertyDefs(this.reader, this.typeHandle);
                }
                return this.propertyDefs.TryGetValue(name, out handle);
            }
        }
    }
}
