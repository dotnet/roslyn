// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Roslyn.Test.MetadataUtilities
{
    [Flags]
    public enum MetadataVisualizerOptions
    {
        None = 0,
        ShortenBlobs = 1,
    }

    public sealed class MetadataVisualizer
    {
        private enum BlobKind
        {
            None,
            Key,
            FileHash,

            MethodSignature,
            FieldSignature,
            MemberRefSignature,
            StandAloneSignature,

            TypeSpec,
            MethodSpec,

            ConstantValue,
            Marshalling,
            PermissionSet,
            CustomAttribute,

            Count
        }

        private readonly TextWriter writer;
        private readonly IReadOnlyList<MetadataReader> readers;
        private readonly MetadataAggregator aggregator;
        private readonly MetadataVisualizerOptions options;

        // enc map for each delta reader
        private readonly ImmutableArray<ImmutableArray<Handle>> encMaps;

        private MetadataReader reader;
        private readonly List<string[]> pendingRows = new List<string[]>();
        private readonly Dictionary<BlobHandle, BlobKind> BlobKinds = new Dictionary<BlobHandle, BlobKind>(); 

        private MetadataVisualizer(TextWriter writer, IReadOnlyList<MetadataReader> readers, MetadataVisualizerOptions options = MetadataVisualizerOptions.None)
        {
            this.writer = writer;
            this.readers = readers;
            this.options = options;

            if (readers.Count > 1)
            {
                var deltaReaders = new List<MetadataReader>(readers.Skip(1));
                this.aggregator = new MetadataAggregator(readers[0], deltaReaders);

                this.encMaps = ImmutableArray.CreateRange(deltaReaders.Select(reader => ImmutableArray.CreateRange(reader.GetEditAndContinueMapEntries())));
            }
        }

        public MetadataVisualizer(MetadataReader reader, TextWriter writer, MetadataVisualizerOptions options = MetadataVisualizerOptions.None)
            : this(writer, new[] { reader }, options)
        {
            this.reader = reader;
        }

        public MetadataVisualizer(IReadOnlyList<MetadataReader> readers, TextWriter writer, MetadataVisualizerOptions options = MetadataVisualizerOptions.None)
            : this(writer, readers, options)
        {
        }

        public void VisualizeAllGenerations()
        {
            for (int i = 0; i < readers.Count; i++)
            {
                writer.WriteLine(">>>");
                writer.WriteLine($">>> Generation {i}:");
                writer.WriteLine(">>>");
                writer.WriteLine();

                Visualize(i);
            }
        }

        public void Visualize(int generation = -1)
        {
            this.reader = (generation >= 0) ? readers[generation] : readers[readers.Count-1];

            WriteModule();                  
            WriteTypeRef();                 
            WriteTypeDef();                 
            WriteField();                   
            WriteMethod();                  
            WriteParam();                   
            WriteMemberRef();               
            WriteConstant();                
            WriteCustomAttribute();         
            WriteDeclSecurity();            
            WriteStandAloneSig();           
            WriteEvent();                   
            WriteProperty();                
            WriteMethodImpl();              
            WriteModuleRef();               
            WriteTypeSpec();                
            WriteEnCLog();                  
            WriteEnCMap();                  
            WriteAssembly();                
            WriteAssemblyRef();             
            WriteFile();
            WriteExportedType();            
            WriteManifestResource();        
            WriteGenericParam();            
            WriteMethodSpec();              
            WriteGenericParamConstraint();

            // heaps:
            WriteUserStrings();
            WriteStrings();
            WriteBlobs();
            WriteGuids();
        }

        private bool IsDelta
        {
            get
            {
                return reader.GetTableRowCount(TableIndex.EncLog) > 0;
            }
        }

        private void WriteTableName(TableIndex index)
        {
            WriteRows(MakeTableName(index));
        }

        private string MakeTableName(TableIndex index)
        {
            return $"{index} (index: 0x{(byte)index:X2}, size: {reader.GetTableRowCount(index) * reader.GetTableRowSize(index)}): ";
        }

        private void AddHeader(params string[] header)
        {
            Debug.Assert(pendingRows.Count == 0);
            pendingRows.Add(header);
        }

        private void AddRow(params string[] fields)
        {
            Debug.Assert(pendingRows.Count > 0 && pendingRows.Last().Length == fields.Length);
            pendingRows.Add(fields);
        }

        private void WriteRows(string title)
        {
            Debug.Assert(pendingRows.Count > 0);

            if (pendingRows.Count == 1)
            {
                pendingRows.Clear();
                return;
            }

            writer.Write(title);
            writer.WriteLine();

            string columnSeparator = "  ";
            int rowNumberWidth = pendingRows.Count.ToString("x").Length;

            int[] columnWidths = new int[pendingRows.First().Length];
            foreach (var row in pendingRows)
            {
                for (int c = 0; c < row.Length; c++)
                {
                    columnWidths[c] = Math.Max(columnWidths[c], row[c].Length + columnSeparator.Length);
                }
            }

            int tableWidth = columnWidths.Sum() + columnWidths.Length;
            string horizontalSeparator = new string('=', tableWidth);

            for (int r = 0; r < pendingRows.Count; r++)
            {
                var row = pendingRows[r];
               
                // header
                if (r == 0)
                {
                    writer.WriteLine(horizontalSeparator);
                    writer.Write(new string(' ', rowNumberWidth + 2));
                }
                else
                {
                    string rowNumber = r.ToString("x");
                    writer.Write(new string(' ', rowNumberWidth - rowNumber.Length));
                    writer.Write(rowNumber);
                    writer.Write(": ");
                }

                for (int c = 0; c < row.Length; c++)
                {
                    var field = row[c];

                    writer.Write(field);
                    writer.Write(new string(' ', columnWidths[c] - field.Length));
                }

                writer.WriteLine();

                // header
                if (r == 0)
                {
                    writer.WriteLine(horizontalSeparator);
                }
            }

            writer.WriteLine();
            pendingRows.Clear();
        }

        private Handle GetAggregateHandle(Handle generationHandle, int generation)
        {
            var encMap = encMaps[generation - 1];

            int start, count;
            if (!TryGetHandleRange(encMap, generationHandle.Kind, out start, out count))
            {
                throw new BadImageFormatException(string.Format("EncMap is missing record for {0:8X}.", MetadataTokens.GetToken(generationHandle)));
            }

            return encMap[start + MetadataTokens.GetRowNumber(generationHandle) - 1];
        }

        private static bool TryGetHandleRange(ImmutableArray<Handle> handles, HandleKind handleType, out int start, out int count)
        {
            TableIndex tableIndex;
            MetadataTokens.TryGetTableIndex(handleType, out tableIndex);

            int mapIndex = handles.BinarySearch(MetadataTokens.Handle(tableIndex, 0), TokenTypeComparer.Instance);
            if (mapIndex < 0)
            {
                start = 0;
                count = 0;
                return false;
            }

            int s = mapIndex;
            while (s >= 0 && handles[s].Kind == handleType)
            {
                s--;
            }

            int e = mapIndex;
            while (e < handles.Length && handles[e].Kind == handleType)
            {
                e++;
            }

            start = s + 1;
            count = e - start;
            return true;
        }

        private MethodDefinition GetMethod(MethodDefinitionHandle handle)
        {
            return Get(handle, (reader, h) => reader.GetMethodDefinition((MethodDefinitionHandle)h));
        }

        private BlobHandle GetLocalSignature(StandaloneSignatureHandle handle)
        {
            return Get(handle, (reader, h) => reader.GetStandaloneSignature((StandaloneSignatureHandle)h).Signature);
        }

        private TEntity Get<TEntity>(Handle handle, Func<MetadataReader, Handle, TEntity> getter)
        {
            if (aggregator != null)
            {
            int generation;
            var generationHandle = aggregator.GetGenerationHandle(handle, out generation);
            return getter(readers[generation], generationHandle);
        }
            else
            {
                return getter(this.reader, handle);
            }
        }

        private string Literal(StringHandle handle)
        {
            return Literal(handle, BlobKind.None, (r, h) => "'" + r.GetString((StringHandle)h) + "'");
        }

        private string Literal(NamespaceDefinitionHandle handle)
        {
            return Literal(handle, BlobKind.None, (r, h) => "'" + r.GetString((NamespaceDefinitionHandle)h) + "'");
        }

        private string Literal(GuidHandle handle)
        {
            return Literal(handle, BlobKind.None, (r, h) => "{" + r.GetGuid((GuidHandle)h) + "}");
        }

        private string Literal(BlobHandle handle, BlobKind kind)
        {
            return Literal(handle, kind, (r, h) => BitConverter.ToString(r.GetBlobBytes((BlobHandle)h)));
        }

        private string Literal(Handle handle, BlobKind kind, Func<MetadataReader, Handle, string> getValue)
        {
            if (handle.IsNil)
            {
                return "nil";
            }

            if (kind != BlobKind.None)
            {
                BlobKinds[(BlobHandle)handle] = kind;
            }

            if (aggregator != null)
            {
                int generation;
                Handle generationHandle = aggregator.GetGenerationHandle(handle, out generation);

                var generationReader = readers[generation];
                string value = GetValueChecked(getValue, generationReader, generationHandle);
                int offset = generationReader.GetHeapOffset(handle);
                int generationOffset = generationReader.GetHeapOffset(generationHandle);

                if (offset == generationOffset)
                {
                    return $"{value} (#{offset:x})";
                }
                else
                {
                    return $"{value} (#{offset:x}/{generationOffset:x})";
                }
            }

            if (IsDelta)
            {
                // we can't resolve the literal without aggregate reader
                return string.Format("#{0:x}", reader.GetHeapOffset(handle));
            }

            return $"{GetValueChecked(getValue, reader, handle):x} (#{reader.GetHeapOffset(handle):x})";
        }

        private string GetValueChecked(Func<MetadataReader, Handle, string> getValue, MetadataReader reader, Handle handle)
        {
            try
            {
                return getValue(reader, handle);
            }
            catch (BadImageFormatException)
            {
                return "<bad metadata>";
            }
        }

        private string Hex(ushort value)
        {
            return "0x" + value.ToString("X4");
        }

        private string Hex(int value)
        {
            return "0x" + value.ToString("X8");
        }

        public string Token(Handle handle, bool displayTable = true)
        {
            if (handle.IsNil)
            {
                return "nil";
            }

            TableIndex table;
            if (displayTable && MetadataTokens.TryGetTableIndex(handle.Kind, out table))
            {
                return string.Format("0x{0:x8} ({1})", reader.GetToken(handle), table);
            }
            else
            {
                return string.Format("0x{0:x8}", reader.GetToken(handle));
            }
        }

        private static string EnumValue<T>(object value) where T : IEquatable<T>
        {
            T integralValue = (T)value;
            if (integralValue.Equals(default(T)))
            {
                return "0";
            }

            return string.Format("0x{0:x8} ({1})", integralValue, value);
        }

        // TODO (tomat): handle collections should implement IReadOnlyCollection<Handle>
        private string TokenRange<THandle>(IReadOnlyCollection<THandle> handles, Func<THandle, Handle> conversion)
        {
            var genericHandles = handles.Select(conversion);
            return (handles.Count == 0) ? "nil" : Token(genericHandles.First(), displayTable: false) + "-" + Token(genericHandles.Last(), displayTable: false);
        }

        public string TokenList(IReadOnlyCollection<Handle> handles, bool displayTable = false)
        {
            if (handles.Count == 0)
            {
                return "nil";
            }

            return string.Join(", ", handles.Select(h => Token(h, displayTable)));
        }

        private void WriteModule()
        {
            var def = reader.GetModuleDefinition();

            AddHeader(
                "Gen",
                "Name",
                "Mvid",
                "EncId",
                "EncBaseId"
            );

            AddRow(
                def.Generation.ToString(), 
                Literal(def.Name),
                Literal(def.Mvid),
                Literal(def.GenerationId),
                Literal(def.BaseGenerationId));

            WriteRows("Module (0x00):");
        }

        private void WriteTypeRef()
        {
            AddHeader(
                "Scope",
                "Name",
                "Namespace"
            );

            foreach (var handle in reader.TypeReferences)
            {
                var entry = reader.GetTypeReference(handle);

                AddRow(
                    Token(entry.ResolutionScope),
                    Literal(entry.Name),
                    Literal(entry.Namespace)
                );
            }

            WriteRows("TypeRef (0x01):");
        }

        private void WriteTypeDef()
        {
            AddHeader(
                "Name",
                "Namespace",
                "EnclosingType",
                "BaseType",
                "Interfaces",
                "Fields",
                "Methods",
                "Attributes",
                "ClassSize",
                "PackingSize"
            );

            foreach (var handle in reader.TypeDefinitions)
            {
                var entry = reader.GetTypeDefinition(handle);

                var layout = entry.GetLayout();
              
                // TODO: Visualize InterfaceImplementations
                var implementedInterfaces = entry.GetInterfaceImplementations().Select(h => reader.GetInterfaceImplementation(h).Interface).ToArray();

                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Namespace),
                    Token(entry.GetDeclaringType()),
                    Token(entry.BaseType),
                    TokenList(implementedInterfaces),
                    TokenRange(entry.GetFields(), h => h),
                    TokenRange(entry.GetMethods(), h => h),
                    EnumValue<int>(entry.Attributes),
                    !layout.IsDefault ? layout.Size.ToString() : "n/a",
                    !layout.IsDefault ? layout.PackingSize.ToString() : "n/a"
                );
            }

            WriteRows("TypeDef (0x02):");
        }

        private void WriteField()
        {
            AddHeader(
                "Name",
                "Signature",
                "Attributes",
                "Marshalling",
                "Offset",
                "RVA"
            );

            foreach (var handle in reader.FieldDefinitions)
            {
                var entry = reader.GetFieldDefinition(handle);

                int offset = entry.GetOffset();

                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Signature, BlobKind.FieldSignature),
                    EnumValue<int>(entry.Attributes),
                    Literal(entry.GetMarshallingDescriptor(), BlobKind.Marshalling),
                    offset >= 0 ? offset.ToString() : "n/a",
                    entry.GetRelativeVirtualAddress().ToString()
                );
            }

            WriteRows("Field (0x04):");
        }

        private void WriteMethod()
        {
            AddHeader(
                "Name",
                "Signature",
                "RVA",
                "Parameters",
                "GenericParameters",
                "ImplAttributes",
                "Attributes",
                "ImportAttributes",
                "ImportName",
                "ImportModule"
            );

            foreach (var handle in reader.MethodDefinitions)
            {
                var entry = reader.GetMethodDefinition(handle);
                var import = entry.GetImport();

                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Signature, BlobKind.MethodSignature),
                    Hex(entry.RelativeVirtualAddress),
                    TokenRange(entry.GetParameters(), h => h),
                    TokenRange(entry.GetGenericParameters(), h => h),
                    EnumValue<int>(entry.Attributes),    // TODO: we need better visualizer than the default enum
                    EnumValue<int>(entry.ImplAttributes),
                    EnumValue<short>(import.Attributes),
                    Literal(import.Name),
                    Token(import.Module)
                );
            }

            WriteRows("Method (0x06, 0x1C):");
        }

        private void WriteParam()
        {
            AddHeader(
                "Name",
                "Seq#",
                "Attributes",
                "Marshalling"
            );

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.Param); i <= count; i++)
            {
                var entry = reader.GetParameter(MetadataTokens.ParameterHandle(i));

                AddRow(
                    Literal(entry.Name),
                    entry.SequenceNumber.ToString(),
                    EnumValue<int>(entry.Attributes),
                    Literal(entry.GetMarshallingDescriptor(), BlobKind.Marshalling)
                );
            }

            WriteRows("Param (0x08):");
        }

        private void WriteMemberRef()
        {
            AddHeader(
                "Parent",
                "Name",
                "Signature"
            );

            foreach (var handle in reader.MemberReferences)
            {
                var entry = reader.GetMemberReference(handle);

                AddRow(
                    Token(entry.Parent),
                    Literal(entry.Name),
                    Literal(entry.Signature, BlobKind.MemberRefSignature)
                );
            }

            WriteRows("MemberRef (0x0a):");
        }

        private void WriteConstant()
        {
            AddHeader(
                "Parent",
                "Type",
                "Value"
            );

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.Constant); i <= count; i++)
            {
                var entry = reader.GetConstant(MetadataTokens.ConstantHandle(i));

                AddRow(
                    Token(entry.Parent),
                    EnumValue<byte>(entry.TypeCode),
                    Literal(entry.Value, BlobKind.ConstantValue)
                );
            }

            WriteRows("Constant (0x0b):");
        }

        private void WriteCustomAttribute()
        {
            AddHeader(
                "Parent",
                "Constructor",
                "Value"
            );

            foreach (var handle in reader.CustomAttributes)
            {
                var entry = reader.GetCustomAttribute(handle);

                AddRow(
                    Token(entry.Parent),
                    Token(entry.Constructor),
                    Literal(entry.Value, BlobKind.CustomAttribute)
                );
            }

            WriteRows("CustomAttribute (0x0c):");
        }

        private void WriteDeclSecurity()
        {
            AddHeader(
                "Parent",
                "PermissionSet",
                "Action"
            );

            foreach (var handle in reader.DeclarativeSecurityAttributes)
            {
                var entry = reader.GetDeclarativeSecurityAttribute(handle);

                AddRow(
                    Token(entry.Parent),
                    Literal(entry.PermissionSet, BlobKind.PermissionSet),
                    EnumValue<short>(entry.Action)
                );
            }

            WriteRows("DeclSecurity (0x0e):");
        }

        private void WriteStandAloneSig()
        {
            AddHeader("Signature");

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.StandAloneSig); i <= count; i++)
            {
                var value = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(i)).Signature;

                AddRow(Literal(value, BlobKind.StandAloneSignature));
            }

            WriteRows("StandAloneSig (0x11):");
        }

        private void WriteEvent()
        {
            AddHeader(
                "Name",
                "Add",
                "Remove",
                "Fire",
                "Attributes"
            );

            foreach (var handle in reader.EventDefinitions)
            {
                var entry = reader.GetEventDefinition(handle);
                var accessors = entry.GetAccessors();

                AddRow(
                    Literal(entry.Name),
                    Token(accessors.Adder),
                    Token(accessors.Remover),
                    Token(accessors.Raiser),
                    EnumValue<int>(entry.Attributes)
                );
            }

            WriteRows("Event (0x12, 0x14, 0x18):");
        }

        private void WriteProperty()
        {
            AddHeader(
                "Name",
                "Get",
                "Set",
                "Attributes"
            );

            foreach (var handle in reader.PropertyDefinitions)
            {
                var entry = reader.GetPropertyDefinition(handle);
                var accessors = entry.GetAccessors();

                AddRow(
                    Literal(entry.Name),
                    Token(accessors.Getter),
                    Token(accessors.Setter),
                    EnumValue<int>(entry.Attributes)
                );
            }

            WriteRows("Property (0x15, 0x17, 0x18):");
        }

        private void WriteMethodImpl()
        {
            AddHeader(
                "Type",
                "Body",
                "Declaration"
            );

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.MethodImpl); i <= count; i++)
            {
                var entry = reader.GetMethodImplementation(MetadataTokens.MethodImplementationHandle(i));

                AddRow(
                    Token(entry.Type),
                    Token(entry.MethodBody),
                    Token(entry.MethodDeclaration)
                );
            }

            WriteRows("MethodImpl (0x19):");
        }

        private void WriteModuleRef()
        {
            AddHeader("Name");

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.ModuleRef); i <= count; i++)
            {
                var value = reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(i)).Name;
                AddRow(Literal(value));
            }

            WriteRows("ModuleRef (0x1a):");
        }

        private void WriteTypeSpec()
        {
            AddHeader("Name");

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.TypeSpec); i <= count; i++)
            {
                var value = reader.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle(i)).Signature;
                AddRow(Literal(value, BlobKind.TypeSpec));
            }

            WriteRows("TypeSpec (0x1b):");
        }

        private void WriteEnCLog()
        {
            AddHeader(
                "Entity",
                "Operation");

            foreach (var entry in reader.GetEditAndContinueLogEntries())
            {
                AddRow(
                    Token(entry.Handle),
                    EnumValue<int>(entry.Operation));
            }

            WriteRows("EnC Log (0x1e):");
        }

        private void WriteEnCMap()
        {
            if (aggregator != null)
            {
                AddHeader("Entity", "Gen", "Row", "Edit");
            }
            else
            {
                AddHeader("Entity");
            }


            foreach (var entry in reader.GetEditAndContinueMapEntries())
            {
                if (aggregator != null)
                {
                    int generation;
                    Handle primary = aggregator.GetGenerationHandle(entry, out generation);
                    bool isUpdate = readers[generation] != reader;

                    var primaryModule = readers[generation].GetModuleDefinition();

                    AddRow(
                        Token(entry),
                        primaryModule.Generation.ToString(),
                        "0x" + MetadataTokens.GetRowNumber(primary).ToString("x6"), 
                        isUpdate ? "update" : "add");
                }
                else
                {
                    AddRow(Token(entry));
                }
            }

            WriteRows("EnC Map (0x1f):");
        }

        private void WriteAssembly()
        {
            if (reader.IsAssembly)
            {
                AddHeader(
                    "Name",
                    "Version",
                    "Culture",
                    "PublicKey",
                    "Flags",
                    "HashAlgorithm"
                );

                var entry = reader.GetAssemblyDefinition();

                AddRow(
                    Literal(entry.Name),
                    entry.Version.Major + "." + entry.Version.Minor + "." + entry.Version.Revision + "." + entry.Version.Build,
                    Literal(entry.Culture),
                    Literal(entry.PublicKey, BlobKind.Key),
                    EnumValue<int>(entry.Flags),
                    EnumValue<int>(entry.HashAlgorithm)
                );

                WriteRows("Assembly (0x20):");
            }
        }

        private void WriteAssemblyRef()
        {
            AddHeader(
                "Name",
                "Version",
                "Culture",
                "PublicKeyOrToken",
                "Flags"
            );

            foreach (var handle in reader.AssemblyReferences)
            {
                var entry = reader.GetAssemblyReference(handle);

                AddRow(
                    Literal(entry.Name),
                    entry.Version.Major + "." + entry.Version.Minor + "." + entry.Version.Revision + "." + entry.Version.Build,
                    Literal(entry.Culture),
                    Literal(entry.PublicKeyOrToken, BlobKind.Key),
                    EnumValue<int>(entry.Flags)
                );
            }

            WriteRows("AssemblyRef (0x23):");
        }

        private void WriteFile()
        {
            AddHeader(
                "Name",
                "Metadata",
                "HashValue"
            );

            foreach (var handle in reader.AssemblyFiles)
            {
                var entry = reader.GetAssemblyFile(handle);

                AddRow(
                    Literal(entry.Name),
                    entry.ContainsMetadata ? "Yes" : "No",
                    Literal(entry.HashValue, BlobKind.FileHash)
                );
            }

            WriteRows("File (0x26):");
        }
        private void WriteExportedType()
        {
            AddHeader(
                "Name",
                "Namespace",
                "Attributes",
                "Implementation",
                "TypeDefinitionId"
            );

            foreach (var handle in reader.ExportedTypes)
            {
                var entry = reader.GetExportedType(handle);
                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Namespace),
                    entry.Attributes.ToString(),
                    Token(entry.Implementation),
                    Hex(entry.GetTypeDefinitionId())
                );
            }

            WriteRows("ExportedType (0x27):");
        }

        private void WriteManifestResource()
        {
            AddHeader(
                "Name",
                "Attributes",
                "Offset",
                "Implementation"
            );

            foreach (var handle in reader.ManifestResources)
            {
                var entry = reader.GetManifestResource(handle);

                AddRow(
                    Literal(entry.Name),
                    entry.Attributes.ToString(),
                    entry.Offset.ToString(),
                    Token(entry.Implementation)
                );
            }

            WriteRows("ManifestResource (0x28):");
        }

        private void WriteGenericParam()
        {
            AddHeader(
                "Name",
                "Seq#",
                "Attributes",
                "Parent",
                "TypeConstraints"
            );

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.GenericParam); i <= count; i++)
            {
                var entry = reader.GetGenericParameter(MetadataTokens.GenericParameterHandle(i));

                AddRow(
                    Literal(entry.Name),
                    entry.Index.ToString(),
                    EnumValue<int>(entry.Attributes),
                    Token(entry.Parent),
                    TokenRange(entry.GetConstraints(), h => h)
                );
            }

            WriteRows("GenericParam (0x2a):");
        }

        private void WriteMethodSpec()
        {
            AddHeader(
                "Method",
                "Signature"
            );

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.MethodSpec); i <= count; i++)
            {
                var entry = reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(i));

                AddRow(
                    Token(entry.Method),
                    Literal(entry.Signature, BlobKind.MethodSpec)
                );
            }

            WriteRows("MethodSpec (0x2b):");
        }

        private void WriteGenericParamConstraint()
        {
            AddHeader(
                "Parent",
                "Type"
            );

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.GenericParamConstraint); i <= count; i++)
            {
                var entry = reader.GetGenericParameterConstraint(MetadataTokens.GenericParameterConstraintHandle(i));

                AddRow(
                    Token(entry.Parameter),
                    Token(entry.Type)
                );
            }

            WriteRows("GenericParamConstraint (0x2c):");
        }

        private void WriteUserStrings()
        {
            int size = reader.GetHeapSize(HeapIndex.UserString);
            if (size == 0)
            {
                return;
            }

            // TODO: the heap is aligned, don't display the trailing empty strings
            writer.WriteLine($"#US (size = {size}):");
            var handle = MetadataTokens.UserStringHandle(0);
            do
            {
                string value = reader.GetUserString(handle);
                writer.WriteLine("  {0:x}: '{1}'", reader.GetHeapOffset(handle), value);
                handle = reader.GetNextHandle(handle);
            }
            while (!handle.IsNil);

            writer.WriteLine();
        }

        private void WriteStrings()
        {
            int size = reader.GetHeapSize(HeapIndex.String);
            if (size == 0)
            {
                return;
            }

            writer.WriteLine($"#String (size = {size}):");
            var handle = MetadataTokens.StringHandle(0);
            do
            {
                string value = reader.GetString(handle);
                writer.WriteLine("  {0:x}: '{1}'", reader.GetHeapOffset(handle), value);
                handle = reader.GetNextHandle(handle);
            }
            while (!handle.IsNil);

            writer.WriteLine();
        }

        private void WriteBlobs()
        {
            int size = reader.GetHeapSize(HeapIndex.Blob);
            if (size == 0)
            {
                return;
            }

            int[] sizePerKind = new int[(int)BlobKind.Count];

            writer.WriteLine($"#Blob (size = {size}):");
            var handle = MetadataTokens.BlobHandle(0);
            do
            {
                byte[] value = reader.GetBlobBytes(handle);

                BlobKind kind;
                string kindString;
                if (BlobKinds.TryGetValue(handle, out kind))
                {
                    kindString = " (" + kind + ")";

                    // ignoring the compressed blob size:
                    sizePerKind[(int)kind] += value.Length;
                }
                else
                {
                    kindString = "";
                }

                int displayLength = (options & MetadataVisualizerOptions.ShortenBlobs) != 0 ? Math.Min(4, value.Length) : value.Length;
                string valueString = BitConverter.ToString(value, 0, displayLength) + (displayLength < value.Length ? "-..." : null);

                writer.WriteLine($"  {reader.GetHeapOffset(handle):x}{kindString}: {valueString}");
                handle = reader.GetNextHandle(handle);
            }
            while (!handle.IsNil);

            writer.WriteLine();
            writer.WriteLine("Sizes:");

            for (int i = 0; i < sizePerKind.Length; i++)
            {
                if (sizePerKind[i] > 0)
                {
                    writer.WriteLine($"  {(BlobKind)i}: {(decimal)sizePerKind[i]} bytes");
                }
            }

            writer.WriteLine();
        }

        private void WriteGuids()
        {
            int size = reader.GetHeapSize(HeapIndex.Guid);
            if (size == 0)
            {
                return;
            }

            writer.WriteLine(string.Format("#Guid (size = {0}):", size));
            int i = 1;
            while (i <= size / 16)
            {
                string value = reader.GetGuid(MetadataTokens.GuidHandle(i)).ToString();
                writer.WriteLine("  {0:x}: {{{1}}}", i, value);
                i++;
            }

            writer.WriteLine();
        }

        public void VisualizeMethodBody(MethodBodyBlock body, MethodDefinitionHandle generationHandle, int generation)
        {
            VisualizeMethodBody(body, (MethodDefinitionHandle)GetAggregateHandle(generationHandle, generation));
        }

        public void VisualizeMethodBody(MethodBodyBlock body, MethodDefinitionHandle methodHandle, bool emitHeader = true)
        {
            StringBuilder builder = new StringBuilder();

            // TODO: Inspect EncLog to find a containing type and display qualified name.
            var method = GetMethod(methodHandle);
            if (emitHeader)
            {
                builder.AppendFormat("Method {0} (0x{1:X8})", Literal(method.Name), MetadataTokens.GetToken(methodHandle));
                builder.AppendLine();
            }

            // TODO: decode signature
            if (!body.LocalSignature.IsNil)
            {
                var localSignature = GetLocalSignature(body.LocalSignature);
                builder.AppendFormat("  Locals: {0}", Literal(localSignature, BlobKind.StandAloneSignature));
                builder.AppendLine();
            }

            ILVisualizerAsTokens.Instance.DumpMethod(
                builder,
                body.MaxStack,
                body.GetILBytes(),
                ImmutableArray.Create<ILVisualizer.LocalInfo>(),     // TODO
                ImmutableArray.Create<ILVisualizer.HandlerSpan>());  // TOOD: ILVisualizer.GetHandlerSpans(body.ExceptionRegions)

            builder.AppendLine();

            writer.Write(builder.ToString());
        }

        private sealed class TokenTypeComparer : IComparer<Handle>
        {
            public static readonly TokenTypeComparer Instance = new TokenTypeComparer();

            public int Compare(Handle x, Handle y)
            {
                return x.Kind.CompareTo(y.Kind);
            }
        }
    }
}
