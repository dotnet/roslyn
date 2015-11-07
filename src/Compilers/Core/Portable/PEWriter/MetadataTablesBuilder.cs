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
    partial class MetadataWriter
    {
        private const byte MetadataFormatMajorVersion = 2;
        private const byte MetadataFormatMinorVersion = 0;

        // type system table rows:
        private struct AssemblyRefTableRow { public Version Version; public BlobIdx PublicKeyToken; public StringIdx Name; public StringIdx Culture; public AssemblyContentType ContentType; public bool IsRetargetable; }
        private struct ModuleRow { public ushort Generation; public StringIdx Name; public int ModuleVersionId; public int EncId; public int EncBaseId; }
        private struct ClassLayoutRow { public ushort PackingSize; public uint ClassSize; public uint Parent; }
        private struct ConstantRow { public byte Type; public uint Parent; public BlobIdx Value; }
        private struct CustomAttributeRow { public uint Parent; public uint Type; public BlobIdx Value; public int OriginalPosition; }
        private struct DeclSecurityRow { public ushort Action; public uint Parent; public BlobIdx PermissionSet; public int OriginalIndex; }
        protected struct EncLogRow { public uint Token; public EncFuncCode FuncCode; }
        protected struct EncMapRow { public uint Token; }
        private struct EventRow { public ushort EventFlags; public StringIdx Name; public uint EventType; }
        protected struct EventMapRow { public uint Parent; public uint EventList; }
        private struct ExportedTypeRow { public TypeFlags Flags; public uint TypeDefId; public StringIdx TypeName; public StringIdx TypeNamespace; public uint Implementation; }
        private struct FieldLayoutRow { public uint Offset; public uint Field; }
        private struct FieldMarshalRow { public uint Parent; public BlobIdx NativeType; }
        private struct FieldRvaRow { public uint Offset; public uint Field; }
        private struct FieldDefRow { public ushort Flags; public StringIdx Name; public BlobIdx Signature; }
        private struct FileTableRow { public uint Flags; public StringIdx FileName; public BlobIdx HashValue; }
        private struct GenericParamConstraintRow { public uint Owner; public uint Constraint; }
        private struct GenericParamRow { public ushort Number; public ushort Flags; public uint Owner; public StringIdx Name; public IGenericParameter GenericParameter; }
        private struct ImplMapRow { public ushort MappingFlags; public uint MemberForwarded; public StringIdx ImportName; public uint ImportScope; }
        private struct InterfaceImplRow { public uint Class; public uint Interface; }
        private struct ManifestResourceRow { public uint Offset; public uint Flags; public StringIdx Name; public uint Implementation; }
        private struct MemberRefRow { public uint Class; public StringIdx Name; public BlobIdx Signature; }
        private struct MethodImplRow { public uint Class; public uint MethodBody; public uint MethodDecl; }
        private struct MethodSemanticsRow { public ushort Semantic; public uint Method; public uint Association; public uint OriginalIndex; }
        private struct MethodSpecRow { public uint Method; public BlobIdx Instantiation; }
        private struct MethodRow { public int Rva; public ushort ImplFlags; public ushort Flags; public StringIdx Name; public BlobIdx Signature; public uint ParamList; }
        private struct ModuleRefRow { public StringIdx Name; }
        private struct NestedClassRow { public uint NestedClass; public uint EnclosingClass; }
        private struct ParamRow { public ushort Flags; public ushort Sequence; public StringIdx Name; }
        protected struct PropertyMapRow { public uint Parent; public uint PropertyList; }
        private struct PropertyRow { public ushort PropFlags; public StringIdx Name; public BlobIdx Type; }
        private struct TypeDefRow { public uint Flags; public StringIdx Name; public StringIdx Namespace; public uint Extends; public uint FieldList; public uint MethodList; }
        private struct TypeRefRow { public uint ResolutionScope; public StringIdx Name; public StringIdx Namespace; }
        private struct TypeSpecRow { public BlobIdx Signature; }
       
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
        private BlobIdx _assemblyKey;
        private StringIdx _assemblyName;
        private StringIdx _assemblyCulture;

        private readonly List<AssemblyRefTableRow> _assemblyRefTable = new List<AssemblyRefTableRow>();
        private readonly List<ClassLayoutRow> _classLayoutTable = new List<ClassLayoutRow>();
        private readonly List<ConstantRow> _constantTable = new List<ConstantRow>();
        private readonly List<CustomAttributeRow> _customAttributeTable = new List<CustomAttributeRow>();
        private readonly List<DeclSecurityRow> _declSecurityTable = new List<DeclSecurityRow>();
        private readonly List<EncLogRow> _encLogTable = new List<EncLogRow>();
        private readonly List<EncMapRow> _encMapTable = new List<EncMapRow>();
        private readonly List<EventMapRow> _eventMapTable = new List<EventMapRow>();
        private readonly List<EventRow> _eventTable = new List<EventRow>();
        private readonly List<ExportedTypeRow> _exportedTypeTable = new List<ExportedTypeRow>();
        private readonly List<FieldLayoutRow> _fieldLayoutTable = new List<FieldLayoutRow>();
        private readonly List<FieldMarshalRow> _fieldMarshalTable = new List<FieldMarshalRow>();
        private readonly List<FieldRvaRow> _fieldRvaTable = new List<FieldRvaRow>();
        private readonly List<FieldDefRow> _fieldDefTable = new List<FieldDefRow>();
        private readonly List<FileTableRow> _fileTable = new List<FileTableRow>();
        private readonly List<GenericParamConstraintRow> _genericParamConstraintTable = new List<GenericParamConstraintRow>();
        private readonly List<GenericParamRow> _genericParamTable = new List<GenericParamRow>();
        private readonly List<ImplMapRow> _implMapTable = new List<ImplMapRow>();
        private readonly List<InterfaceImplRow> _interfaceImplTable = new List<InterfaceImplRow>();
        private readonly List<ManifestResourceRow> _manifestResourceTable = new List<ManifestResourceRow>();
        private readonly List<MemberRefRow> _memberRefTable = new List<MemberRefRow>();
        private readonly List<MethodImplRow> _methodImplTable = new List<MethodImplRow>();
        private readonly List<MethodSemanticsRow> _methodSemanticsTable = new List<MethodSemanticsRow>();
        private readonly List<MethodSpecRow> _methodSpecTable = new List<MethodSpecRow>();
        private readonly List<ModuleRefRow> _moduleRefTable = new List<ModuleRefRow>();
        private readonly List<NestedClassRow> _nestedClassTable = new List<NestedClassRow>();
        private readonly List<ParamRow> _paramTable = new List<ParamRow>();
        private readonly List<PropertyMapRow> _propertyMapTable = new List<PropertyMapRow>();
        private readonly List<PropertyRow> _propertyTable = new List<PropertyRow>();
        private readonly List<TypeDefRow> _typeDefTable = new List<TypeDefRow>();
        private readonly List<TypeRefRow> _typeRefTable = new List<TypeRefRow>();
        private readonly List<TypeSpecRow> _typeSpecTable = new List<TypeSpecRow>();

        // debug tables:
        private readonly List<DocumentRow> _documentTable = new List<DocumentRow>();
        private readonly List<MethodDebugInformationRow> _methodDebugInformationTable = new List<MethodDebugInformationRow>();
        private readonly List<LocalScopeRow> _localScopeTable = new List<LocalScopeRow>();
        private readonly List<LocalVariableRow> _localVariableTable = new List<LocalVariableRow>();
        private readonly List<LocalConstantRow> _localConstantTable = new List<LocalConstantRow>();
        private readonly List<ImportScopeRow> _importScopeTable = new List<ImportScopeRow>();
        private readonly List<StateMachineMethodRow> _stateMachineMethodTable = new List<StateMachineMethodRow>();
        private readonly List<CustomDebugInformationRow> _customDebugInformationTable = new List<CustomDebugInformationRow>();
    }
}
