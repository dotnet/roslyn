// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class MetadataTablesBuilder
    {
        private const byte MetadataFormatMajorVersion = 2;
        private const byte MetadataFormatMinorVersion = 0;

        private readonly MetadataHeapsBuilder _heaps;
        private readonly MetadataHeapsBuilder _debugHeapsOpt;

        public MetadataTablesBuilder(MetadataHeapsBuilder heaps, MetadataHeapsBuilder debugHeaps)
        {
            if (heaps == null)
            {
                throw new ArgumentNullException(nameof(heaps));
            }

            _heaps = heaps;
            _debugHeapsOpt = debugHeaps;
        }

        // type system table rows:
        private struct AssemblyRefTableRow { public Version Version; public BlobIdx PublicKeyToken; public StringIdx Name; public StringIdx Culture; public uint Flags; public BlobIdx HashValue; }
        private struct ModuleRow { public ushort Generation; public StringIdx Name; public int ModuleVersionId; public int EncId; public int EncBaseId; }
        private struct AssemblyRow { public uint HashAlgorithm; public Version Version; public ushort Flags; public BlobIdx AssemblyKey; public StringIdx AssemblyName; public StringIdx AssemblyCulture; }
        private struct ClassLayoutRow { public ushort PackingSize; public uint ClassSize; public uint Parent; }
        private struct ConstantRow { public byte Type; public uint Parent; public BlobIdx Value; }
        private struct CustomAttributeRow { public uint Parent; public uint Type; public BlobIdx Value; }
        private struct DeclSecurityRow { public ushort Action; public uint Parent; public BlobIdx PermissionSet; }
        private struct EncLogRow { public uint Token; public byte FuncCode; }
        private struct EncMapRow { public uint Token; }
        private struct EventRow { public ushort EventFlags; public StringIdx Name; public uint EventType; }
        private struct EventMapRow { public uint Parent; public uint EventList; }
        private struct ExportedTypeRow { public uint Flags; public uint TypeDefId; public StringIdx TypeName; public StringIdx TypeNamespace; public uint Implementation; }
        private struct FieldLayoutRow { public uint Offset; public uint Field; }
        private struct FieldMarshalRow { public uint Parent; public BlobIdx NativeType; }
        private struct FieldRvaRow { public uint Offset; public uint Field; }
        private struct FieldDefRow { public ushort Flags; public StringIdx Name; public BlobIdx Signature; }
        private struct FileTableRow { public uint Flags; public StringIdx FileName; public BlobIdx HashValue; }
        private struct GenericParamConstraintRow { public uint Owner; public uint Constraint; }
        private struct GenericParamRow { public ushort Number; public ushort Flags; public uint Owner; public StringIdx Name; }
        private struct ImplMapRow { public ushort MappingFlags; public uint MemberForwarded; public StringIdx ImportName; public uint ImportScope; }
        private struct InterfaceImplRow { public uint Class; public uint Interface; }
        private struct ManifestResourceRow { public uint Offset; public uint Flags; public StringIdx Name; public uint Implementation; }
        private struct MemberRefRow { public uint Class; public StringIdx Name; public BlobIdx Signature; }
        private struct MethodImplRow { public uint Class; public uint MethodBody; public uint MethodDecl; }
        private struct MethodSemanticsRow { public ushort Semantic; public uint Method; public uint Association; }
        private struct MethodSpecRow { public uint Method; public BlobIdx Instantiation; }
        private struct MethodRow { public int Rva; public ushort ImplFlags; public ushort Flags; public StringIdx Name; public BlobIdx Signature; public uint ParamList; }
        private struct ModuleRefRow { public StringIdx Name; }
        private struct NestedClassRow { public uint NestedClass; public uint EnclosingClass; }
        private struct ParamRow { public ushort Flags; public ushort Sequence; public StringIdx Name; }
        private struct PropertyMapRow { public uint Parent; public uint PropertyList; }
        private struct PropertyRow { public ushort PropFlags; public StringIdx Name; public BlobIdx Type; }
        private struct TypeDefRow { public uint Flags; public StringIdx Name; public StringIdx Namespace; public uint Extends; public uint FieldList; public uint MethodList; }
        private struct TypeRefRow { public uint ResolutionScope; public StringIdx Name; public StringIdx Namespace; }
        private struct TypeSpecRow { public BlobIdx Signature; }
        private struct StandaloneSigRow { public BlobIdx Signature; }
       
        // debug table rows:
        private struct DocumentRow { public BlobIdx Name; public uint HashAlgorithm; public BlobIdx Hash; public uint Language; }
        private struct MethodDebugInformationRow { public uint Document; public BlobIdx SequencePoints; }
        private struct LocalScopeRow { public uint Method; public uint ImportScope; public uint VariableList; public uint ConstantList; public uint StartOffset; public uint Length; }
        private struct LocalVariableRow { public ushort Attributes; public ushort Index; public StringIdx Name; } 
        private struct LocalConstantRow { public StringIdx Name; public BlobIdx Signature; }
        private struct ImportScopeRow { public uint Parent; public BlobIdx Imports; }
        private struct StateMachineMethodRow { public uint MoveNextMethod; public uint KickoffMethod; }
        private struct CustomDebugInformationRow { public uint Parent; public uint Kind; public BlobIdx Value; }

        // type system tables:
        private readonly List<ModuleRow> _moduleTable = new List<ModuleRow>(1);
        private readonly List<AssemblyRow> _assemblyTable = new List<AssemblyRow>(1);
        private readonly List<ClassLayoutRow> _classLayoutTable = new List<ClassLayoutRow>();

        private readonly List<ConstantRow> _constantTable = new List<ConstantRow>();
        private uint _constantTableLastParent;
        private bool _constantTableNeedsSorting;

        private readonly List<CustomAttributeRow> _customAttributeTable = new List<CustomAttributeRow>();
        private uint _customAttributeTableLastParent;
        private bool _customAttributeTableNeedsSorting;

        private readonly List<DeclSecurityRow> _declSecurityTable = new List<DeclSecurityRow>();
        private uint _declSecurityTableLastParent;
        private bool _declSecurityTableNeedsSorting;

        private readonly List<EncLogRow> _encLogTable = new List<EncLogRow>();
        private readonly List<EncMapRow> _encMapTable = new List<EncMapRow>();
        private readonly List<EventRow> _eventTable = new List<EventRow>();
        private readonly List<EventMapRow> _eventMapTable = new List<EventMapRow>();        
        private readonly List<ExportedTypeRow> _exportedTypeTable = new List<ExportedTypeRow>();
        private readonly List<FieldLayoutRow> _fieldLayoutTable = new List<FieldLayoutRow>();

        private readonly List<FieldMarshalRow> _fieldMarshalTable = new List<FieldMarshalRow>();
        private uint _fieldMarshalTableLastParent;
        private bool _fieldMarshalTableNeedsSorting;

        private readonly List<FieldRvaRow> _fieldRvaTable = new List<FieldRvaRow>();
        private readonly List<FieldDefRow> _fieldTable = new List<FieldDefRow>();
        private readonly List<FileTableRow> _fileTable = new List<FileTableRow>();
        private readonly List<GenericParamConstraintRow> _genericParamConstraintTable = new List<GenericParamConstraintRow>();
        private readonly List<GenericParamRow> _genericParamTable = new List<GenericParamRow>();
        private readonly List<ImplMapRow> _implMapTable = new List<ImplMapRow>();
        private readonly List<InterfaceImplRow> _interfaceImplTable = new List<InterfaceImplRow>();
        private readonly List<ManifestResourceRow> _manifestResourceTable = new List<ManifestResourceRow>();
        private readonly List<MemberRefRow> _memberRefTable = new List<MemberRefRow>();
        private readonly List<MethodImplRow> _methodImplTable = new List<MethodImplRow>();

        private readonly List<MethodSemanticsRow> _methodSemanticsTable = new List<MethodSemanticsRow>();
        private uint _methodSemanticsTableLastAssociation;
        private bool _methodSemanticsTableNeedsSorting;

        private readonly List<MethodSpecRow> _methodSpecTable = new List<MethodSpecRow>();
        private readonly List<MethodRow> _methodDefTable = new List<MethodRow>();
        private readonly List<ModuleRefRow> _moduleRefTable = new List<ModuleRefRow>();
        private readonly List<NestedClassRow> _nestedClassTable = new List<NestedClassRow>();
        private readonly List<ParamRow> _paramTable = new List<ParamRow>();
        private readonly List<PropertyMapRow> _propertyMapTable = new List<PropertyMapRow>();
        private readonly List<PropertyRow> _propertyTable = new List<PropertyRow>();
        private readonly List<TypeDefRow> _typeDefTable = new List<TypeDefRow>();
        private readonly List<TypeRefRow> _typeRefTable = new List<TypeRefRow>();
        private readonly List<TypeSpecRow> _typeSpecTable = new List<TypeSpecRow>();
        private readonly List<AssemblyRefTableRow> _assemblyRefTable = new List<AssemblyRefTableRow>();
        private readonly List<StandaloneSigRow> _standAloneSigTable = new List<StandaloneSigRow>();

        // debug tables:
        private readonly List<DocumentRow> _documentTable = new List<DocumentRow>();
        private readonly List<MethodDebugInformationRow> _methodDebugInformationTable = new List<MethodDebugInformationRow>();
        private readonly List<LocalScopeRow> _localScopeTable = new List<LocalScopeRow>();
        private readonly List<LocalVariableRow> _localVariableTable = new List<LocalVariableRow>();
        private readonly List<LocalConstantRow> _localConstantTable = new List<LocalConstantRow>();
        private readonly List<ImportScopeRow> _importScopeTable = new List<ImportScopeRow>();
        private readonly List<StateMachineMethodRow> _stateMachineMethodTable = new List<StateMachineMethodRow>();
        private readonly List<CustomDebugInformationRow> _customDebugInformationTable = new List<CustomDebugInformationRow>();
        
        public void SetCapacity(TableIndex table, int capacity)
        {
            switch (table)
            {
                case TableIndex.Module:                 _moduleTable.Capacity = capacity; break;
                case TableIndex.TypeRef:                _typeRefTable.Capacity = capacity; break;
                case TableIndex.TypeDef:                _typeDefTable.Capacity = capacity; break;
                case TableIndex.Field:                  _fieldTable.Capacity = capacity; break;
                case TableIndex.MethodDef:              _methodDefTable.Capacity = capacity; break;
                case TableIndex.Param:                  _paramTable.Capacity = capacity; break;
                case TableIndex.InterfaceImpl:          _interfaceImplTable.Capacity = capacity; break;
                case TableIndex.MemberRef:              _memberRefTable.Capacity = capacity; break;
                case TableIndex.Constant:               _constantTable.Capacity = capacity; break;
                case TableIndex.CustomAttribute:        _customAttributeTable.Capacity = capacity; break;
                case TableIndex.FieldMarshal:           _fieldMarshalTable.Capacity = capacity; break;
                case TableIndex.DeclSecurity:           _declSecurityTable.Capacity = capacity; break;
                case TableIndex.ClassLayout:            _classLayoutTable.Capacity = capacity; break;
                case TableIndex.FieldLayout:            _fieldLayoutTable.Capacity = capacity; break;
                case TableIndex.StandAloneSig:          _standAloneSigTable.Capacity = capacity; break;
                case TableIndex.EventMap:               _eventMapTable.Capacity = capacity; break;
                case TableIndex.Event:                  _eventTable.Capacity = capacity; break;
                case TableIndex.PropertyMap:            _propertyMapTable.Capacity = capacity; break;
                case TableIndex.Property:               _propertyTable.Capacity = capacity; break;
                case TableIndex.MethodSemantics:        _methodSemanticsTable.Capacity = capacity; break;
                case TableIndex.MethodImpl:             _methodImplTable.Capacity = capacity; break;
                case TableIndex.ModuleRef:              _moduleRefTable.Capacity = capacity; break;
                case TableIndex.TypeSpec:               _typeSpecTable.Capacity = capacity; break;
                case TableIndex.ImplMap:                _implMapTable.Capacity = capacity; break;
                case TableIndex.FieldRva:               _fieldRvaTable.Capacity = capacity; break;
                case TableIndex.EncLog:                 _encLogTable.Capacity = capacity; break;
                case TableIndex.EncMap:                 _encMapTable.Capacity = capacity; break;
                case TableIndex.Assembly:               _assemblyTable.Capacity = capacity; break;
                case TableIndex.AssemblyRef:            _assemblyRefTable.Capacity = capacity; break;
                case TableIndex.File:                   _fileTable.Capacity = capacity; break;
                case TableIndex.ExportedType:           _exportedTypeTable.Capacity = capacity; break;
                case TableIndex.ManifestResource:       _manifestResourceTable.Capacity = capacity; break;
                case TableIndex.NestedClass:            _nestedClassTable.Capacity = capacity; break;
                case TableIndex.GenericParam:           _genericParamTable.Capacity = capacity; break;
                case TableIndex.MethodSpec:             _methodSpecTable.Capacity = capacity; break;
                case TableIndex.GenericParamConstraint: _genericParamConstraintTable.Capacity = capacity; break;
                case TableIndex.Document:               _documentTable.Capacity = capacity; break;
                case TableIndex.MethodDebugInformation: _methodDebugInformationTable.Capacity = capacity; break;
                case TableIndex.LocalScope:             _localScopeTable.Capacity = capacity; break;
                case TableIndex.LocalVariable:          _localVariableTable.Capacity = capacity; break;
                case TableIndex.LocalConstant:          _localConstantTable.Capacity = capacity; break;
                case TableIndex.ImportScope:            _importScopeTable.Capacity = capacity; break;
                case TableIndex.StateMachineMethod:     _stateMachineMethodTable.Capacity = capacity; break;
                case TableIndex.CustomDebugInformation: _customDebugInformationTable.Capacity = capacity; break;

                case TableIndex.AssemblyOS:
                case TableIndex.AssemblyProcessor:
                case TableIndex.AssemblyRefOS:
                case TableIndex.AssemblyRefProcessor:
                case TableIndex.EventPtr:
                case TableIndex.FieldPtr:
                case TableIndex.MethodPtr:
                case TableIndex.ParamPtr:
                case TableIndex.PropertyPtr:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(table));
            }
        }

        #region Building

        public void AddModule(
            int generation,
            StringIdx moduleName,
            Guid mvid, 
            Guid encId, 
            Guid encBaseId)
        {
            _moduleTable.Add(new ModuleRow
            {
                Generation = (ushort)generation,
                Name = moduleName,
                ModuleVersionId = _heaps.AllocateGuid(mvid),
                EncId = _heaps.GetGuidIndex(encId),
                EncBaseId = _heaps.GetGuidIndex(encBaseId),
            });
        }

        public void AddAssembly(
            StringIdx name, 
            Version version,
            StringIdx culture,
            BlobIdx publicKey,
            AssemblyFlags flags,
            AssemblyHashAlgorithm hashAlgorithm)
        {
            _assemblyTable.Add(new AssemblyRow
            {
                Flags = (ushort)flags,
                HashAlgorithm = (uint)hashAlgorithm,
                Version = version,
                AssemblyKey = publicKey,
                AssemblyName = name,
                AssemblyCulture = culture
            });
        }

        public void AddAssemblyReference(
            StringIdx name,
            Version version,
            StringIdx culture,
            BlobIdx publicKeyOrToken,
            AssemblyFlags flags,
            BlobIdx hashValue)
        {
            _assemblyRefTable.Add(new AssemblyRefTableRow
            {
                Name = name,
                Version = version,
                Culture = culture,
                PublicKeyToken = publicKeyOrToken,
                Flags = (uint)flags,
                HashValue = hashValue
            });
        }

        public void AddTypeDefinition(
            TypeAttributes attributes, 
            StringIdx @namespace,
            StringIdx name,
            uint baseTypeCodedIndex,
            int fieldList,
            int methodList)
        {
            Debug.Assert(@namespace != null);
            Debug.Assert(name != null);

            _typeDefTable.Add(new TypeDefRow
            {
                Flags = (uint)attributes,
                Name = name,
                Namespace = @namespace,
                Extends = baseTypeCodedIndex,
                FieldList = (uint)fieldList,
                MethodList = (uint)methodList
            });
        }

        public void AddTypeLayout(
            int typeDefinitionRowId,
            ushort packingSize,
            uint size)
        {
            _classLayoutTable.Add(new ClassLayoutRow
            {
                Parent = (uint)typeDefinitionRowId,
                PackingSize = packingSize,
                ClassSize = size
            });
        }

        public void AddInterfaceImplementation(
            int typeDefinitionRowId,
            uint interfaceCodedIndex)
        {
            _interfaceImplTable.Add(new InterfaceImplRow
            {
                Class = (uint)typeDefinitionRowId,
                Interface = interfaceCodedIndex
            });
        }

        public void AddNestedType(
            int typeDefinitionRowId,
            int enclosingTypeDefinitionRowId)
        {
            _nestedClassTable.Add(new NestedClassRow
            {
                NestedClass = (uint)typeDefinitionRowId,
                EnclosingClass = (uint)enclosingTypeDefinitionRowId
            });
        }

        public int AddTypeReference(uint resolutionScope, StringIdx @namespace, StringIdx name)
        {
            Debug.Assert(@namespace != null);
            Debug.Assert(name != null);

            _typeRefTable.Add(new TypeRefRow
            {
                ResolutionScope = resolutionScope,
                Name = name,
                Namespace = @namespace
            });

            // row id
            return _typeRefTable.Count;
        }

        public void SetTypeSpecificationTableCapacity(int capacity)
        {
            _typeSpecTable.Capacity = capacity;
        }

        public void AddTypeSpecification(BlobIdx signature)
        {
            _typeSpecTable.Add(new TypeSpecRow
            {
                Signature = signature
            });
        }

        public void SetStandaloneSigTableCapacity(int capacity)
        {
            _standAloneSigTable.Capacity = capacity;
        }

        public void AddStandaloneSignature(BlobIdx signature)
        {
            _standAloneSigTable.Add(new StandaloneSigRow
            {
                Signature = signature
            });
        }

        public void AddProperty(PropertyAttributes attributes, StringIdx name, BlobIdx signature)
        {
            _propertyTable.Add(new PropertyRow
            {
                PropFlags = (ushort)attributes,
                Name = name,
                Type = signature
            });
        }

        public void AddPropertyMap(int typeDefinitionRowId, int propertyList)
        {
            _propertyMapTable.Add(new PropertyMapRow
            {
                Parent = (uint)typeDefinitionRowId,
                PropertyList = (uint)propertyList
            });
        }

        public void AddEvent(EventAttributes attributes, StringIdx name, uint type)
        {
            _eventTable.Add(new EventRow
            {
                EventFlags = (ushort)attributes,
                Name = name,
                EventType = type
            });
        }

        public void AddEventMap(int typeDefinitionRowId, int eventList)
        {
            _eventMapTable.Add(new EventMapRow
            {
                Parent = (uint)typeDefinitionRowId,
                EventList = (uint)eventList
            });
        }

        public void AddConstant(uint parent, object value)
        {
            // the table is required to be sorted by Parent:
            _constantTableNeedsSorting |= parent < _constantTableLastParent;
            _constantTableLastParent = parent;

            _constantTable.Add(new ConstantRow
            {
                Type = (byte)MetadataWriterUtilities.GetConstantTypeCode(value),
                Parent = parent,
                Value = _heaps.GetConstantBlobIndex(value)
            });
        }

        public void AddMethodSemantics(uint association, ushort semantics, int methodDefinitionRowId)
        {
            // the table is required to be sorted by Association:
            _methodSemanticsTableNeedsSorting |= association < _methodSemanticsTableLastAssociation;
            _methodSemanticsTableLastAssociation = association;

            _methodSemanticsTable.Add(new MethodSemanticsRow
            {
                Association = association,
                Method = (uint)methodDefinitionRowId,
                Semantic = semantics
            });
        }

        public void AddCustomAttribute(uint parent, uint constructor, BlobIdx value)
        {
            // the table is required to be sorted by Parent:
            _customAttributeTableNeedsSorting |= parent < _customAttributeTableLastParent;
            _customAttributeTableLastParent = parent;

            _customAttributeTable.Add(new CustomAttributeRow
            {
                Parent = parent,
                Type = constructor,
                Value = value
            });
        }

        public void AddMethodSpecification(uint method, BlobIdx instantiation)
        {
            _methodSpecTable.Add(new MethodSpecRow
            {
                Method = method,
                Instantiation = instantiation
            });
        }

        public void AddModuleReference(StringIdx moduleName)
        {
            _moduleRefTable.Add(new ModuleRefRow
            {
                Name = moduleName
            });
        }

        public void AddParameter(ParameterAttributes attributes, StringIdx name, int sequenceNumber)
        {
            _paramTable.Add(new ParamRow
            {
                Flags = (ushort)attributes,
                Name = name,
                Sequence = (ushort)sequenceNumber
            });
        }

        public int AddGenericParameter(
            uint parent,
            GenericParameterAttributes attributes,
            StringIdx name,
            int index)
        {
            _genericParamTable.Add(new GenericParamRow
            {
                Flags = (ushort)attributes,
                Name = name,
                Number = (ushort)index,
                Owner = parent
            });

            // row id
            return _genericParamTable.Count;
        }

        public void AddGenericParameterConstraint(
            int genericParameterRowId,
            uint constraint)
        {
            _genericParamConstraintTable.Add(new GenericParamConstraintRow
            {
                Owner = (uint)genericParameterRowId,
                Constraint = constraint,
            });
        }

        public void AddFieldDefinition(
            FieldAttributes attributes,
            StringIdx name,
            BlobIdx signature)
        {
            _fieldTable.Add(new FieldDefRow
            {
                Flags = (ushort)attributes,
                Name = name,
                Signature = signature
            });
        }

        public void AddFieldLayout(
            int fieldDefinitionRowId,
            int offset)
        {
            _fieldLayoutTable.Add(new FieldLayoutRow
            {
                Field = (uint)fieldDefinitionRowId,
                Offset = (uint)offset
            });
        }
        public void AddMarshallingDescriptor(
            uint parent,
            BlobIdx descriptor)
        {
            // the table is required to be sorted by Parent:
            _fieldMarshalTableNeedsSorting |= parent < _fieldMarshalTableLastParent;
            _fieldMarshalTableLastParent = parent;

            _fieldMarshalTable.Add(new FieldMarshalRow
            {
                Parent = parent,
                NativeType = descriptor
            });
        }

        public void AddFieldRelativeVirtualAddress(
            int fieldDefinitionRowId,
            int relativeVirtualAddress)
        {
            _fieldRvaTable.Add(new FieldRvaRow
            {
                Field = (uint)fieldDefinitionRowId,
                Offset = (uint)relativeVirtualAddress
            });
        }

        public void AddMethodDefinition(
            MethodAttributes attributes, 
            MethodImplAttributes implAttributes,
            StringIdx name,
            BlobIdx signature,
            int relativeVirtualAddress,
            int paramList)
        {
            _methodDefTable.Add(new MethodRow
            {
                Flags = (ushort)attributes,
                ImplFlags = (ushort)implAttributes,
                Name = name,
                Signature = signature,
                Rva = relativeVirtualAddress,
                ParamList = (uint)paramList
            });
        }

        public void AddMethodImport(
            uint member,
            MethodImportAttributes attributes, 
            StringIdx name, 
            int moduleReferenceRowId)
        {
            _implMapTable.Add(new ImplMapRow
            {
                MemberForwarded = member,
                ImportName = name,
                ImportScope = (uint)moduleReferenceRowId,
                MappingFlags = (ushort)attributes,
            });
        }

        public void AddMethodImplementation(
            int typeDefinitionRowId,
            uint methodBody,
            uint methodDeclaration)
        {
            _methodImplTable.Add(new MethodImplRow
            {
                Class = (uint)typeDefinitionRowId,
                MethodBody = methodBody,
                MethodDecl = methodDeclaration
            });
        }

        public void AddMemberReference(
            uint type,
            StringIdx name,
            BlobIdx signature)
        {
            _memberRefTable.Add(new MemberRefRow
            {
                Class = type,
                Name = name,
                Signature = signature
            });
        }

        public void AddManifestResource(
            ManifestResourceAttributes attributes,
            StringIdx name,
            uint implementation,
            long offset)
        {
            _manifestResourceTable.Add(new ManifestResourceRow
            {
                Flags = (uint)attributes,
                Name = name,
                Implementation = implementation,
                Offset = (uint)offset
            });
        }

        public void AddAssemblyFile(
            StringIdx name,
            BlobIdx hashValue,
            bool containsMetadata)
        {
            _fileTable.Add(new FileTableRow
            {
                FileName = name,
                Flags = containsMetadata ? 0u : 1u,
                HashValue = hashValue
            });
        }

        public void AddExportedType(
            TypeAttributes attributes,
            StringIdx @namespace,
            StringIdx name,
            uint implementation,
            int typeDefinitionId)
        {
            _exportedTypeTable.Add(new ExportedTypeRow
            {
                Flags = (uint)attributes,
                Implementation = implementation,
                TypeNamespace = @namespace,
                TypeName = name,
                TypeDefId = (uint)typeDefinitionId
            });
        }

        // TODO: remove
        public uint GetExportedTypeFlags(int rowId)
        {
            return _exportedTypeTable[rowId].Flags;
        }

        public void AddDeclarativeSecurityAttribute(
            uint parent,
            DeclarativeSecurityAction action,
            BlobIdx permissionSet)
        {
            // the table is required to be sorted by Parent:
            _declSecurityTableNeedsSorting |= parent < _declSecurityTableLastParent;
            _declSecurityTableLastParent = parent;

            _declSecurityTable.Add(new DeclSecurityRow
            {
                Parent = parent,
                Action = (ushort)action,
                PermissionSet = permissionSet
            });
        }

        public void AddEncLogEntry(int token, EncFuncCode code)
        {
            _encLogTable.Add(new EncLogRow
            {
                Token = (uint)token,
                FuncCode = (byte)code
            });
        }

        public void AddEncMapEntry(int token)
        {
            _encMapTable.Add(new EncMapRow
            {
                Token = (uint)token
            });
        }

        #endregion

        public ImmutableArray<int> GetRowCounts()
        {
            var rowCounts = new int[MetadataTokens.TableCount];

            rowCounts[(int)TableIndex.Assembly] = _assemblyTable.Count;
            rowCounts[(int)TableIndex.AssemblyRef] = _assemblyRefTable.Count;
            rowCounts[(int)TableIndex.ClassLayout] = _classLayoutTable.Count;
            rowCounts[(int)TableIndex.Constant] = _constantTable.Count;
            rowCounts[(int)TableIndex.CustomAttribute] = _customAttributeTable.Count;
            rowCounts[(int)TableIndex.DeclSecurity] = _declSecurityTable.Count;
            rowCounts[(int)TableIndex.EncLog] = _encLogTable.Count;
            rowCounts[(int)TableIndex.EncMap] = _encMapTable.Count;
            rowCounts[(int)TableIndex.EventMap] = _eventMapTable.Count;
            rowCounts[(int)TableIndex.Event] = _eventTable.Count;
            rowCounts[(int)TableIndex.ExportedType] = _exportedTypeTable.Count;
            rowCounts[(int)TableIndex.FieldLayout] = _fieldLayoutTable.Count;
            rowCounts[(int)TableIndex.FieldMarshal] = _fieldMarshalTable.Count;
            rowCounts[(int)TableIndex.FieldRva] = _fieldRvaTable.Count;
            rowCounts[(int)TableIndex.Field] = _fieldTable.Count;
            rowCounts[(int)TableIndex.File] = _fileTable.Count;
            rowCounts[(int)TableIndex.GenericParamConstraint] = _genericParamConstraintTable.Count;
            rowCounts[(int)TableIndex.GenericParam] = _genericParamTable.Count;
            rowCounts[(int)TableIndex.ImplMap] = _implMapTable.Count;
            rowCounts[(int)TableIndex.InterfaceImpl] = _interfaceImplTable.Count;
            rowCounts[(int)TableIndex.ManifestResource] = _manifestResourceTable.Count;
            rowCounts[(int)TableIndex.MemberRef] = _memberRefTable.Count;
            rowCounts[(int)TableIndex.MethodImpl] = _methodImplTable.Count;
            rowCounts[(int)TableIndex.MethodSemantics] = _methodSemanticsTable.Count;
            rowCounts[(int)TableIndex.MethodSpec] = _methodSpecTable.Count;
            rowCounts[(int)TableIndex.MethodDef] = _methodDefTable.Count;
            rowCounts[(int)TableIndex.ModuleRef] = _moduleRefTable.Count;
            rowCounts[(int)TableIndex.Module] = 1;
            rowCounts[(int)TableIndex.NestedClass] = _nestedClassTable.Count;
            rowCounts[(int)TableIndex.Param] = _paramTable.Count;
            rowCounts[(int)TableIndex.PropertyMap] = _propertyMapTable.Count;
            rowCounts[(int)TableIndex.Property] = _propertyTable.Count;
            rowCounts[(int)TableIndex.StandAloneSig] = _standAloneSigTable.Count;
            rowCounts[(int)TableIndex.TypeDef] = _typeDefTable.Count;
            rowCounts[(int)TableIndex.TypeRef] = _typeRefTable.Count;
            rowCounts[(int)TableIndex.TypeSpec] = _typeSpecTable.Count;

            rowCounts[(int)TableIndex.Document] = _documentTable.Count;
            rowCounts[(int)TableIndex.MethodDebugInformation] = _methodDebugInformationTable.Count;
            rowCounts[(int)TableIndex.LocalScope] = _localScopeTable.Count;
            rowCounts[(int)TableIndex.LocalVariable] = _localVariableTable.Count;
            rowCounts[(int)TableIndex.LocalConstant] = _localConstantTable.Count;
            rowCounts[(int)TableIndex.StateMachineMethod] = _stateMachineMethodTable.Count;
            rowCounts[(int)TableIndex.ImportScope] = _importScopeTable.Count;
            rowCounts[(int)TableIndex.CustomDebugInformation] = _customDebugInformationTable.Count;

            return ImmutableArray.CreateRange(rowCounts);
        }

        public int GetModuleVersionGuidOffsetInMetadataStream(int guidHeapOffsetInMetadataStream)
        {
            // index of module version ID in the guidWriter stream
            int moduleVersionIdIndex = _moduleTable[0].ModuleVersionId;

            // offset into the guidWriter stream of the module version ID
            int moduleVersionOffsetInGuidTable = (moduleVersionIdIndex - 1) << 4;

            return guidHeapOffsetInMetadataStream + moduleVersionOffsetInGuidTable;
        }

        #region Serialization

        private void SerializeMetadataTables(
            BlobBuilder writer,
            MetadataSizes metadataSizes,
            int methodBodyStreamRva,
            int mappedFieldDataStreamRva)
        {
            int startPosition = writer.Position;

            this.SerializeTablesHeader(writer, metadataSizes);

            if (metadataSizes.IsPresent(TableIndex.Module))
            {
                SerializeModuleTable(writer, metadataSizes, _heaps);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeRef))
            {
                this.SerializeTypeRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeDef))
            {
                this.SerializeTypeDefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Field))
            {
                this.SerializeFieldTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodDef))
            {
                this.SerializeMethodDefTable(writer, metadataSizes, methodBodyStreamRva);
            }

            if (metadataSizes.IsPresent(TableIndex.Param))
            {
                this.SerializeParamTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.InterfaceImpl))
            {
                this.SerializeInterfaceImplTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MemberRef))
            {
                this.SerializeMemberRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Constant))
            {
                this.SerializeConstantTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.CustomAttribute))
            {
                this.SerializeCustomAttributeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldMarshal))
            {
                this.SerializeFieldMarshalTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.DeclSecurity))
            {
                this.SerializeDeclSecurityTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ClassLayout))
            {
                this.SerializeClassLayoutTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldLayout))
            {
                this.SerializeFieldLayoutTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.StandAloneSig))
            {
                this.SerializeStandAloneSigTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.EventMap))
            {
                this.SerializeEventMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Event))
            {
                this.SerializeEventTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.PropertyMap))
            {
                this.SerializePropertyMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Property))
            {
                this.SerializePropertyTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodSemantics))
            {
                this.SerializeMethodSemanticsTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodImpl))
            {
                this.SerializeMethodImplTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ModuleRef))
            {
                this.SerializeModuleRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeSpec))
            {
                this.SerializeTypeSpecTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ImplMap))
            {
                this.SerializeImplMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldRva))
            {
                this.SerializeFieldRvaTable(writer, metadataSizes, mappedFieldDataStreamRva);
            }

            if (metadataSizes.IsPresent(TableIndex.EncLog))
            {
                this.SerializeEncLogTable(writer);
            }

            if (metadataSizes.IsPresent(TableIndex.EncMap))
            {
                this.SerializeEncMapTable(writer);
            }

            if (metadataSizes.IsPresent(TableIndex.Assembly))
            {
                this.SerializeAssemblyTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.AssemblyRef))
            {
                this.SerializeAssemblyRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.File))
            {
                this.SerializeFileTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ExportedType))
            {
                this.SerializeExportedTypeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ManifestResource))
            {
                this.SerializeManifestResourceTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.NestedClass))
            {
                this.SerializeNestedClassTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.GenericParam))
            {
                this.SerializeGenericParamTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodSpec))
            {
                this.SerializeMethodSpecTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.GenericParamConstraint))
            {
                this.SerializeGenericParamConstraintTable(writer, metadataSizes);
            }

            // debug tables
            if (metadataSizes.IsPresent(TableIndex.Document))
            {
                this.SerializeDocumentTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodDebugInformation))
            {
                this.SerializeMethodDebugInformationTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalScope))
            {
                this.SerializeLocalScopeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalVariable))
            {
                this.SerializeLocalVariableTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalConstant))
            {
                this.SerializeLocalConstantTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ImportScope))
            {
                this.SerializeImportScopeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.StateMachineMethod))
            {
                this.SerializeStateMachineMethodTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.CustomDebugInformation))
            {
                this.SerializeCustomDebugInformationTable(writer, metadataSizes);
            }

            writer.WriteByte(0);
            writer.Align(4);

            int endPosition = writer.Position;
            Debug.Assert(metadataSizes.MetadataTableStreamSize == endPosition - startPosition);
        }

        private void SerializeTablesHeader(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            int startPosition = writer.Position;

            HeapSizeFlag heapSizes = 0;
            if (metadataSizes.StringIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.StringHeapLarge;
            }

            if (metadataSizes.GuidIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.GuidHeapLarge;
            }

            if (metadataSizes.BlobIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.BlobHeapLarge;
            }

            if (metadataSizes.IsMinimalDelta)
            {
                heapSizes |= (HeapSizeFlag.EnCDeltas | HeapSizeFlag.DeletedMarks);
            }

            ulong sortedDebugTables = metadataSizes.PresentTablesMask & MetadataSizes.SortedDebugTables;

            // Consider filtering out type system tables that are not present:
            ulong sortedTables = sortedDebugTables | (metadataSizes.IsStandaloneDebugMetadata ? 0UL : 0x16003301fa00);

            writer.WriteUInt32(0); // reserved
            writer.WriteByte(MetadataFormatMajorVersion);
            writer.WriteByte(MetadataFormatMinorVersion);
            writer.WriteByte((byte)heapSizes);
            writer.WriteByte(1); // reserved
            writer.WriteUInt64(metadataSizes.PresentTablesMask);
            writer.WriteUInt64(sortedTables);
            SerializeRowCounts(writer, metadataSizes.RowCounts, metadataSizes.PresentTablesMask);

            int endPosition = writer.Position;
            Debug.Assert(metadataSizes.CalculateTableStreamHeaderSize() == endPosition - startPosition);
        }

        private static void SerializeRowCounts(BlobBuilder writer, ImmutableArray<int> rowCounts, ulong includeTables)
        {
            for (int i = 0; i < rowCounts.Length; i++)
            {
                if (((1UL << i) & includeTables) != 0)
                {
                    int rowCount = rowCounts[i];
                    if (rowCount > 0)
                    {
                        writer.WriteInt32(rowCount);
                    }
                }
            }
        }

        private void SerializeModuleTable(BlobBuilder writer, MetadataSizes metadataSizes, MetadataHeapsBuilder heaps)
        {
            foreach (var moduleRow in _moduleTable)
            {
                writer.WriteUInt16(moduleRow.Generation);
                writer.WriteReference((uint)heaps.ResolveStringIndex(moduleRow.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)moduleRow.ModuleVersionId, metadataSizes.GuidIndexSize);
                writer.WriteReference((uint)moduleRow.EncId, metadataSizes.GuidIndexSize);
                writer.WriteReference((uint)moduleRow.EncBaseId, metadataSizes.GuidIndexSize);
            }
        }

        private void SerializeEncLogTable(BlobBuilder writer)
        {
            foreach (EncLogRow encLog in _encLogTable)
            {
                writer.WriteUInt32(encLog.Token);
                writer.WriteUInt32(encLog.FuncCode);
            }
        }

        private void SerializeEncMapTable(BlobBuilder writer)
        {
            foreach (EncMapRow encMap in _encMapTable)
            {
                writer.WriteUInt32(encMap.Token);
            }
        }

        private void SerializeTypeRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeRefRow typeRef in _typeRefTable)
            {
                writer.WriteReference(typeRef.ResolutionScope, metadataSizes.ResolutionScopeCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(typeRef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(typeRef.Namespace), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeTypeDefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeDefRow typeDef in _typeDefTable)
            {
                writer.WriteUInt32(typeDef.Flags);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(typeDef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(typeDef.Namespace), metadataSizes.StringIndexSize);
                writer.WriteReference(typeDef.Extends, metadataSizes.TypeDefOrRefCodedIndexSize);
                writer.WriteReference(typeDef.FieldList, metadataSizes.FieldDefIndexSize);
                writer.WriteReference(typeDef.MethodList, metadataSizes.MethodDefIndexSize);
            }
        }

        private void SerializeFieldTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FieldDefRow fieldDef in _fieldTable)
            {
                writer.WriteUInt16(fieldDef.Flags);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(fieldDef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(fieldDef.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeMethodDefTable(BlobBuilder writer, MetadataSizes metadataSizes, int methodBodyStreamRva)
        {
            foreach (MethodRow method in _methodDefTable)
            {
                if (method.Rva == -1)
                {
                    writer.WriteUInt32(0);
                }
                else
                {
                    writer.WriteUInt32((uint)(methodBodyStreamRva + method.Rva));
                }

                writer.WriteUInt16(method.ImplFlags);
                writer.WriteUInt16(method.Flags);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(method.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(method.Signature), metadataSizes.BlobIndexSize);
                writer.WriteReference(method.ParamList, metadataSizes.ParameterIndexSize);
            }
        }

        private void SerializeParamTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ParamRow param in _paramTable)
            {
                writer.WriteUInt16(param.Flags);
                writer.WriteUInt16(param.Sequence);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(param.Name), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeInterfaceImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // TODO (bug https://github.com/dotnet/roslyn/issues/3905):
            // We should sort the table by Class and then by Interface.
            foreach (InterfaceImplRow interfaceImpl in _interfaceImplTable)
            {
                writer.WriteReference(interfaceImpl.Class, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(interfaceImpl.Interface, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private void SerializeMemberRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MemberRefRow memberRef in _memberRefTable)
            {
                writer.WriteReference(memberRef.Class, metadataSizes.MemberRefParentCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(memberRef.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(memberRef.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeConstantTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            var ordered = _constantTableNeedsSorting ? _constantTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _constantTable;

            foreach (ConstantRow constant in ordered)
            {
                writer.WriteByte(constant.Type);
                writer.WriteByte(0);
                writer.WriteReference(constant.Parent, metadataSizes.HasConstantCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(constant.Value), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeCustomAttributeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent will be sorted in the order they were added to the table.
            var ordered = _customAttributeTableNeedsSorting ? _customAttributeTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _customAttributeTable;

            foreach (CustomAttributeRow customAttribute in ordered)
            {
                writer.WriteReference(customAttribute.Parent, metadataSizes.HasCustomAttributeCodedIndexSize);
                writer.WriteReference(customAttribute.Type, metadataSizes.CustomAttributeTypeCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(customAttribute.Value), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeFieldMarshalTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            var ordered = _fieldMarshalTableNeedsSorting ? _fieldMarshalTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _fieldMarshalTable;
            
            foreach (FieldMarshalRow fieldMarshal in ordered)
            {
                writer.WriteReference(fieldMarshal.Parent, metadataSizes.HasFieldMarshalCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(fieldMarshal.NativeType), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeDeclSecurityTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent will be sorted in the order they were added to the table.
            var ordered = _declSecurityTableNeedsSorting ? _declSecurityTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _declSecurityTable;
            
            foreach (DeclSecurityRow declSecurity in ordered)
            {
                writer.WriteUInt16(declSecurity.Action);
                writer.WriteReference(declSecurity.Parent, metadataSizes.DeclSecurityCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(declSecurity.PermissionSet), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeClassLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _classLayoutTable.Count; i++)
            {
                Debug.Assert(_classLayoutTable[i - 1].Parent < _classLayoutTable[i].Parent);
            }
#endif
            foreach (ClassLayoutRow classLayout in _classLayoutTable)
            {
                writer.WriteUInt16(classLayout.PackingSize);
                writer.WriteUInt32(classLayout.ClassSize);
                writer.WriteReference(classLayout.Parent, metadataSizes.TypeDefIndexSize);
            }
        }

        private void SerializeFieldLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _fieldLayoutTable.Count; i++)
            {
                Debug.Assert(_fieldLayoutTable[i - 1].Field < _fieldLayoutTable[i].Field);
            }
#endif
            foreach (FieldLayoutRow fieldLayout in _fieldLayoutTable)
            {
                writer.WriteUInt32(fieldLayout.Offset);
                writer.WriteReference(fieldLayout.Field, metadataSizes.FieldDefIndexSize);
            }
        }

        private void SerializeStandAloneSigTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (StandaloneSigRow row in _standAloneSigTable)
            {
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(row.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeEventMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (EventMapRow eventMap in _eventMapTable)
            {
                writer.WriteReference(eventMap.Parent, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(eventMap.EventList, metadataSizes.EventDefIndexSize);
            }
        }

        private void SerializeEventTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (EventRow eventRow in _eventTable)
            {
                writer.WriteUInt16(eventRow.EventFlags);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(eventRow.Name), metadataSizes.StringIndexSize);
                writer.WriteReference(eventRow.EventType, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private void SerializePropertyMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyMapRow propertyMap in _propertyMapTable)
            {
                writer.WriteReference(propertyMap.Parent, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(propertyMap.PropertyList, metadataSizes.PropertyDefIndexSize);
            }
        }

        private void SerializePropertyTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyRow property in _propertyTable)
            {
                writer.WriteUInt16(property.PropFlags);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(property.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(property.Type), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeMethodSemanticsTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent will be sorted in the order they were added to the table.
            var ordered = _methodSemanticsTableNeedsSorting ? _methodSemanticsTable.OrderBy((x, y) => (int)x.Association - (int)y.Association) : _methodSemanticsTable;
            
            foreach (MethodSemanticsRow methodSemantic in ordered)
            {
                writer.WriteUInt16(methodSemantic.Semantic);
                writer.WriteReference(methodSemantic.Method, metadataSizes.MethodDefIndexSize);
                writer.WriteReference(methodSemantic.Association, metadataSizes.HasSemanticsCodedIndexSize);
            }
        }

        private void SerializeMethodImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _methodImplTable.Count; i++)
            {
                Debug.Assert(_methodImplTable[i - 1].Class <= _methodImplTable[i].Class);
            }
#endif
            foreach (MethodImplRow methodImpl in _methodImplTable)
            {
                writer.WriteReference(methodImpl.Class, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(methodImpl.MethodBody, metadataSizes.MethodDefOrRefCodedIndexSize);
                writer.WriteReference(methodImpl.MethodDecl, metadataSizes.MethodDefOrRefCodedIndexSize);
            }
        }

        private void SerializeModuleRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ModuleRefRow moduleRef in _moduleRefTable)
            {
                writer.WriteReference((uint)_heaps.ResolveStringIndex(moduleRef.Name), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeTypeSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeSpecRow typeSpec in _typeSpecTable)
            {
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(typeSpec.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeImplMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _implMapTable.Count; i++)
            {
                Debug.Assert(_implMapTable[i - 1].MemberForwarded < _implMapTable[i].MemberForwarded);
            }
#endif
            foreach (ImplMapRow implMap in _implMapTable)
            {
                writer.WriteUInt16(implMap.MappingFlags);
                writer.WriteReference(implMap.MemberForwarded, metadataSizes.MemberForwardedCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(implMap.ImportName), metadataSizes.StringIndexSize);
                writer.WriteReference(implMap.ImportScope, metadataSizes.ModuleRefIndexSize);
            }
        }

        private void SerializeFieldRvaTable(BlobBuilder writer, MetadataSizes metadataSizes, int mappedFieldDataStreamRva)
        {
#if DEBUG
            for (int i = 1; i < _fieldRvaTable.Count; i++)
            {
                Debug.Assert(_fieldRvaTable[i - 1].Field < _fieldRvaTable[i].Field);
            }
#endif
            foreach (FieldRvaRow fieldRva in _fieldRvaTable)
            {
                writer.WriteUInt32((uint)mappedFieldDataStreamRva + fieldRva.Offset);
                writer.WriteReference(fieldRva.Field, metadataSizes.FieldDefIndexSize);
            }
        }

        private void SerializeAssemblyTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (AssemblyRow row in _assemblyTable)
            {
                writer.WriteUInt32(row.HashAlgorithm);
                writer.WriteUInt16((ushort)row.Version.Major);
                writer.WriteUInt16((ushort)row.Version.Minor);
                writer.WriteUInt16((ushort)row.Version.Build);
                writer.WriteUInt16((ushort)row.Version.Revision);
                writer.WriteUInt32(row.Flags);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(row.AssemblyKey), metadataSizes.BlobIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(row.AssemblyName), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(row.AssemblyCulture), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeAssemblyRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (AssemblyRefTableRow row in _assemblyRefTable)
            {
                writer.WriteUInt16((ushort)row.Version.Major);
                writer.WriteUInt16((ushort)row.Version.Minor);
                writer.WriteUInt16((ushort)row.Version.Build);
                writer.WriteUInt16((ushort)row.Version.Revision);
                writer.WriteUInt32(row.Flags);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(row.PublicKeyToken), metadataSizes.BlobIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(row.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(row.Culture), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(row.HashValue), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeFileTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FileTableRow fileReference in _fileTable)
            {
                writer.WriteUInt32(fileReference.Flags);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(fileReference.FileName), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(fileReference.HashValue), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeExportedTypeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ExportedTypeRow exportedType in _exportedTypeTable)
            {
                writer.WriteUInt32((uint)exportedType.Flags);
                writer.WriteUInt32(exportedType.TypeDefId);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(exportedType.TypeName), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(exportedType.TypeNamespace), metadataSizes.StringIndexSize);
                writer.WriteReference(exportedType.Implementation, metadataSizes.ImplementationCodedIndexSize);
            }
        }

        private void SerializeManifestResourceTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ManifestResourceRow manifestResource in _manifestResourceTable)
            {
                writer.WriteUInt32(manifestResource.Offset);
                writer.WriteUInt32(manifestResource.Flags);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(manifestResource.Name), metadataSizes.StringIndexSize);
                writer.WriteReference(manifestResource.Implementation, metadataSizes.ImplementationCodedIndexSize);
            }
        }

        private void SerializeNestedClassTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _nestedClassTable.Count; i++)
            {
                Debug.Assert(_nestedClassTable[i - 1].NestedClass <= _nestedClassTable[i].NestedClass);
            }
#endif
            foreach (NestedClassRow nestedClass in _nestedClassTable)
            {
                writer.WriteReference(nestedClass.NestedClass, metadataSizes.TypeDefIndexSize);
                writer.WriteReference(nestedClass.EnclosingClass, metadataSizes.TypeDefIndexSize);
            }
        }

        private void SerializeGenericParamTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _genericParamTable.Count; i++)
            {
                Debug.Assert(
                    _genericParamTable[i - 1].Owner < _genericParamTable[i].Owner ||
                    _genericParamTable[i - 1].Owner == _genericParamTable[i].Owner && _genericParamTable[i - 1].Number < _genericParamTable[i].Number);
            }
#endif            
            foreach (GenericParamRow genericParam in _genericParamTable)
            {
                writer.WriteUInt16(genericParam.Number);
                writer.WriteUInt16(genericParam.Flags);
                writer.WriteReference(genericParam.Owner, metadataSizes.TypeOrMethodDefCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveStringIndex(genericParam.Name), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeGenericParamConstraintTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _genericParamConstraintTable.Count; i++)
            {
                Debug.Assert(_genericParamConstraintTable[i - 1].Owner <= _genericParamConstraintTable[i].Owner);
            }
#endif
            foreach (GenericParamConstraintRow genericParamConstraint in _genericParamConstraintTable)
            {
                writer.WriteReference(genericParamConstraint.Owner, metadataSizes.GenericParamIndexSize);
                writer.WriteReference(genericParamConstraint.Constraint, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private void SerializeMethodSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MethodSpecRow methodSpec in _methodSpecTable)
            {
                writer.WriteReference(methodSpec.Method, metadataSizes.MethodDefOrRefCodedIndexSize);
                writer.WriteReference((uint)_heaps.ResolveBlobIndex(methodSpec.Instantiation), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeDocumentTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _documentTable)
            {
                writer.WriteReference((uint)_debugHeapsOpt.ResolveBlobIndex(row.Name), metadataSizes.BlobIndexSize);
                writer.WriteReference(row.HashAlgorithm, metadataSizes.GuidIndexSize);
                writer.WriteReference((uint)_debugHeapsOpt.ResolveBlobIndex(row.Hash), metadataSizes.BlobIndexSize);
                writer.WriteReference(row.Language, metadataSizes.GuidIndexSize);
            }
        }

        private void SerializeMethodDebugInformationTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _methodDebugInformationTable)
            {
                writer.WriteReference(row.Document, metadataSizes.DocumentIndexSize);
                writer.WriteReference((uint)_debugHeapsOpt.ResolveBlobIndex(row.SequencePoints), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeLocalScopeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            // Spec: The table is required to be sorted first by Method in ascending order, then by StartOffset in ascending order, then by Length in descending order.
            for (int i = 1; i < _localScopeTable.Count; i++)
            {
                Debug.Assert(_localScopeTable[i - 1].Method <= _localScopeTable[i].Method);
                if (_localScopeTable[i - 1].Method == _localScopeTable[i].Method)
                {
                    Debug.Assert(_localScopeTable[i - 1].StartOffset <= _localScopeTable[i].StartOffset);
                    if (_localScopeTable[i - 1].StartOffset == _localScopeTable[i].StartOffset)
                    {
                        Debug.Assert(_localScopeTable[i - 1].Length >= _localScopeTable[i].Length);
                    }
                }
            }
#endif
            foreach (var row in _localScopeTable)
            {
                writer.WriteReference(row.Method, metadataSizes.MethodDefIndexSize);
                writer.WriteReference(row.ImportScope, metadataSizes.ImportScopeIndexSize);
                writer.WriteReference(row.VariableList, metadataSizes.LocalVariableIndexSize);
                writer.WriteReference(row.ConstantList, metadataSizes.LocalConstantIndexSize);
                writer.WriteUInt32(row.StartOffset);
                writer.WriteUInt32(row.Length);
            }
        }

        private void SerializeLocalVariableTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _localVariableTable)
            {
                writer.WriteUInt16(row.Attributes);
                writer.WriteUInt16(row.Index);
                writer.WriteReference((uint)_debugHeapsOpt.ResolveStringIndex(row.Name), metadataSizes.StringIndexSize);
            }
        }

        private void SerializeLocalConstantTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _localConstantTable)
            {
                writer.WriteReference((uint)_debugHeapsOpt.ResolveStringIndex(row.Name), metadataSizes.StringIndexSize);
                writer.WriteReference((uint)_debugHeapsOpt.ResolveBlobIndex(row.Signature), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeImportScopeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _importScopeTable)
            {
                writer.WriteReference(row.Parent, metadataSizes.ImportScopeIndexSize);
                writer.WriteReference((uint)_debugHeapsOpt.ResolveBlobIndex(row.Imports), metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeStateMachineMethodTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _stateMachineMethodTable.Count; i++)
            {
                Debug.Assert(_stateMachineMethodTable[i - 1].MoveNextMethod < _stateMachineMethodTable[i].MoveNextMethod);
            }
#endif
            foreach (var row in _stateMachineMethodTable)
            {
                writer.WriteReference(row.MoveNextMethod, metadataSizes.MethodDefIndexSize);
                writer.WriteReference(row.KickoffMethod, metadataSizes.MethodDefIndexSize);
            }
        }

        private void SerializeCustomDebugInformationTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent and kind will be sorted in the order they were added to the table.
            foreach (CustomDebugInformationRow row in _customDebugInformationTable.OrderBy((x, y) =>
            {
                int result = (int)x.Parent - (int)y.Parent;
                return (result != 0) ? result : (int)x.Kind - (int)y.Kind;
            }))
            {
                writer.WriteReference(row.Parent, metadataSizes.HasCustomDebugInformationSize);
                writer.WriteReference(row.Kind, metadataSizes.GuidIndexSize);
                writer.WriteReference((uint)_debugHeapsOpt.ResolveBlobIndex(row.Value), metadataSizes.BlobIndexSize);
            }
        }

        #endregion
    }
}
