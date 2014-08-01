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
    public sealed class MetadataVisualizer
    {
        private readonly TextWriter writer;
        private readonly IReadOnlyList<MetadataReader> readers;
        private readonly MetadataAggregator aggregator;

        // enc map for each delta reader
        private readonly ImmutableArray<ImmutableArray<Handle>> encMaps;

        private MetadataReader reader;
        private readonly List<string[]> pendingRows = new List<string[]>();

        private MetadataVisualizer(TextWriter writer, IReadOnlyList<MetadataReader> readers)
        {
            this.writer = writer;
            this.readers = readers;

            if (readers.Count > 1)
            {
                var deltaReaders = new List<MetadataReader>(readers.Skip(1));
                this.aggregator = new MetadataAggregator(readers[0], deltaReaders);

                this.encMaps = ImmutableArray.CreateRange(deltaReaders.Select(reader => ImmutableArray.CreateRange(reader.GetEditAndContinueMapEntries())));
            }
        }

        public MetadataVisualizer(MetadataReader reader, TextWriter writer)
            : this(writer, new[] { reader })
        {
            this.reader = reader;
        }

        public MetadataVisualizer(IReadOnlyList<MetadataReader> readers, TextWriter writer)
            : this(writer, readers)
        {
        }

        public void VisualizeAllGenerations()
        {
            for (int i = 0; i < readers.Count; i++)
            {
                writer.WriteLine(">>>");
                writer.WriteLine(string.Format(">>> Generation {0}:", i));
                writer.WriteLine(">>>");
                writer.WriteLine();

                Visualize(i);
            }
        }

        public void Visualize(int generation = -1)
        {
            this.reader = (generation >= 0) ? readers[generation] : readers.Last();

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
            WriteForwarders();
            WriteExportedType();            
            WriteManifestResource();        
            WriteGenericParam();            
            WriteMethodSpec();              
            WriteGenericParamConstraint();

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
            if (!TryGetHandleRange(encMap, generationHandle.HandleType, out start, out count))
            {
                throw new BadImageFormatException(string.Format("EncMap is missing record for {0:8X}.", MetadataTokens.GetToken(generationHandle)));
            }

            return encMap[start + MetadataTokens.GetRowNumber(generationHandle) - 1];
        }

        private static bool TryGetHandleRange(ImmutableArray<Handle> handles, HandleType handleType, out int start, out int count)
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
            while (s >= 0 && handles[s].HandleType == handleType)
            {
                s--;
            }

            int e = mapIndex;
            while (e < handles.Length && handles[e].HandleType == handleType)
            {
                e++;
            }

            start = s + 1;
            count = e - start;
            return true;
        }

        private Method GetMethod(MethodHandle handle)
        {
            return Get(handle, (reader, h) => reader.GetMethod((MethodHandle)h));
        }

        private BlobHandle GetLocalSignature(LocalSignatureHandle handle)
        {
            return Get(handle, (reader, h) => reader.GetLocalSignature((LocalSignatureHandle)h));
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
            return Literal(handle, (r, h) => "'" + r.GetString((StringHandle)h) + "'");
        }

        private string Literal(NamespaceHandle handle)
        {
            return Literal(handle, (r, h) => "'" + r.GetString((NamespaceHandle)h) + "'");
        }

        private string Literal(GuidHandle handle)
        {
            return Literal(handle, (r, h) => "{" + r.GetGuid((GuidHandle)h) + "}");
        }

        private string Literal(BlobHandle handle)
        {
            return Literal(handle, (r, h) => BitConverter.ToString(r.GetBytes((BlobHandle)h)));
        }

        private string Literal(Handle handle, Func<MetadataReader, Handle, string> getValue)
        {
            if (handle.IsNil)
            {
                return "nil";
            }

            if (aggregator != null)
            {
                int generation;
                Handle generationHandle = aggregator.GetGenerationHandle(handle, out generation);

                var generationReader = readers[generation];
                string value = getValue(generationReader, generationHandle);
                int offset = generationReader.GetHeapOffset(handle);
                int generationOffset = generationReader.GetHeapOffset(generationHandle);

                if (offset == generationOffset)
                {
                    return string.Format("{0} (#{1:x})", value, offset);
                }
                else
                {
                    return string.Format("{0} (#{1:x}/{2:x})", value, offset, generationOffset);
                }
            }

            if (IsDelta)
            {
                // we can't resolve the literal without aggregate reader
                return string.Format("#{0:x}", reader.GetHeapOffset(handle));
            }

            return string.Format("{1:x} (#{0:x})", reader.GetHeapOffset(handle), getValue(reader, handle));
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
            if (displayTable && MetadataTokens.TryGetTableIndex(handle.HandleType, out table))
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

                uint size, packingSize;
                bool hasLayout = entry.GetTypeLayout(out size, out packingSize);

                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Namespace),
                    Token(entry.GetDeclaringType()),
                    Token(entry.BaseType),
                    TokenList(entry.GetImplementedInterfaces()),
                    TokenRange(entry.GetFields(), h => h),
                    TokenRange(entry.GetMethods(), h => h),
                    EnumValue<int>(entry.Attributes),
                    hasLayout ? size.ToString() : "n/a",
                    hasLayout ? packingSize.ToString() : "n/a"
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
                var entry = reader.GetField(handle);

                int offset = entry.GetOffset();

                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Signature),
                    EnumValue<int>(entry.Attributes),
                    Literal(entry.GetMarshallingDescriptor()),
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
                var entry = reader.GetMethod(handle);
                var import = entry.GetImport();

                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Signature),
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
                    Literal(entry.GetMarshallingDescriptor())
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
                    Literal(entry.Signature)
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
                    EnumValue<byte>(entry.Type),
                    Literal(entry.Value)
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
                    Literal(entry.Value)
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
                    Literal(entry.PermissionSet),
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
                var value = reader.GetLocalSignature(MetadataTokens.LocalSignatureHandle(i));

                AddRow(Literal(value));
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
                var entry = reader.GetEvent(handle);
                var accessors = entry.GetAssociatedMethods();

                AddRow(
                    Literal(entry.Name),
                    Token(accessors.AddOn),
                    Token(accessors.RemoveOn),
                    Token(accessors.Fire),
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
                var entry = reader.GetProperty(handle);
                var accessors = entry.GetAssociatedMethods();

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
                var value = reader.GetModuleReferenceName(MetadataTokens.ModuleReferenceHandle(i));
                AddRow(Literal(value));
            }

            WriteRows("ModuleRef (0x1a):");
        }

        private void WriteTypeSpec()
        {
            AddHeader("Name");

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.TypeSpec); i <= count; i++)
            {
                var value = reader.GetSignature(MetadataTokens.TypeSpecificationHandle(i));
                AddRow(Literal(value));
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
                    Literal(entry.PublicKey),
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
                    Literal(entry.PublicKeyOrToken),
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
                    Literal(entry.HashValue)
                );
            }

            WriteRows("File (0x26):");
        }

        private void WriteForwarders()
        {
            AddHeader(
                "Name",
                "Namespace",
                "Assembly"
            );

            foreach (var handle in reader.TypeForwarders)
            {
                var entry = reader.GetTypeForwarder(handle);
                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Namespace),
                    Token(entry.Implementation));
            }

            WriteRows("ExportedType - forwarders (0x27):");
        }

        private void WriteExportedType()
        {
            // TODO
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
                "TypeConstraints"
            );

            for (int i = 1, count = reader.GetTableRowCount(TableIndex.GenericParam); i <= count; i++)
            {
                var entry = reader.GetGenericParameter(MetadataTokens.GenericParameterHandle(i));

                AddRow(
                    Literal(entry.Name),
                    entry.Index.ToString(),
                    EnumValue<int>(entry.Attributes),
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
                    Literal(entry.Signature)
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
            writer.WriteLine(string.Format("#US (size = {0}):", size));
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

            writer.WriteLine(string.Format("#String (size = {0}):", size));
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

            writer.WriteLine(string.Format("#Blob (size = {0}):", size));
            var handle = MetadataTokens.BlobHandle(0);
            do
            {
                byte[] value = reader.GetBytes(handle);
                writer.WriteLine("  {0:x}: {1}", reader.GetHeapOffset(handle), BitConverter.ToString(value));
                handle = reader.GetNextHandle(handle);
            }
            while (!handle.IsNil);

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

        public void VisualizeMethodBody(MethodBodyBlock body, MethodHandle generationHandle, int generation)
        {
            VisualizeMethodBody(body, (MethodHandle)GetAggregateHandle(generationHandle, generation));
        }

        public void VisualizeMethodBody(MethodBodyBlock body, MethodHandle methodHandle)
        {
            StringBuilder builder = new StringBuilder();

            // TODO: Inspect EncLog to find a containing type and display qualified name.
            var method = GetMethod(methodHandle);
            builder.AppendFormat("Method {0} (0x{1:X8})", Literal(method.Name), MetadataTokens.GetToken(methodHandle));
            builder.AppendLine();

            // TODO: decode signature
            if (!body.LocalSignature.IsNil)
            {
                var localSignature = GetLocalSignature(body.LocalSignature);
                builder.AppendFormat("  Locals: {0}", Literal(localSignature));
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
                return x.HandleType.CompareTo(y.HandleType);
            }
        }
    }
}
