// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal partial class MetadataWriter
    {
        /// <summary>
        /// Import scopes are associated with binders (in C#) and thus multiple instances might be created for a single set of imports.
        /// We consider scopes with the same parent and the same imports the same.
        /// Internal for testing.
        /// </summary>
        internal sealed class ImportScopeEqualityComparer : IEqualityComparer<IImportScope>
        {
            private readonly EmitContext _context;

            public ImportScopeEqualityComparer(EmitContext context)
            {
                _context = context;
            }

            public bool Equals(IImportScope x, IImportScope y)
            {
                return (object)x == y ||
                       x != null && y != null && Equals(x.Parent, y.Parent) && x.GetUsedNamespaces(_context).SequenceEqual(y.GetUsedNamespaces(_context));
            }

            public int GetHashCode(IImportScope obj)
            {
                return Hash.Combine(Hash.CombineValues(obj.GetUsedNamespaces(_context)), obj.Parent != null ? GetHashCode(obj.Parent) : 0);
            }
        }

        private readonly Dictionary<DebugSourceDocument, DocumentHandle> _documentIndex = new Dictionary<DebugSourceDocument, DocumentHandle>();
        private readonly Dictionary<IImportScope, ImportScopeHandle> _scopeIndex;

        private void SerializeMethodDebugInfo(IMethodBody bodyOpt, int methodRid, int aggregateMethodRid, StandaloneSignatureHandle localSignatureHandleOpt, ref LocalVariableHandle lastLocalVariableHandle, ref LocalConstantHandle lastLocalConstantHandle)
        {
            if (bodyOpt == null)
            {
                _debugMetadataOpt.AddMethodDebugInformation(document: default, sequencePoints: default);
                return;
            }

            bool isKickoffMethod = bodyOpt.StateMachineTypeName != null;
            bool emitAllDebugInfo = isKickoffMethod || !bodyOpt.SequencePoints.IsEmpty;

            var methodHandle = MetadataTokens.MethodDefinitionHandle(methodRid);

            // documents & sequence points:
            BlobHandle sequencePointsBlob = SerializeSequencePoints(localSignatureHandleOpt, bodyOpt.SequencePoints, _documentIndex, out var singleDocumentHandle);
            _debugMetadataOpt.AddMethodDebugInformation(document: singleDocumentHandle, sequencePoints: sequencePointsBlob);

            if (emitAllDebugInfo)
            {
                var bodyImportScope = bodyOpt.ImportScope;
                var importScopeHandle = (bodyImportScope != null) ? GetImportScopeIndex(bodyImportScope, _scopeIndex) : default;

                // Unlike native PDB we don't emit an empty root scope.
                // scopes are already ordered by StartOffset ascending then by EndOffset descending (the longest scope first).

                if (bodyOpt.LocalScopes.Length == 0)
                {
                    // TODO: the compiler should produce a scope for each debuggable method rather then adding one here
                    _debugMetadataOpt.AddLocalScope(
                        method: methodHandle,
                        importScope: importScopeHandle,
                        variableList: NextHandle(lastLocalVariableHandle),
                        constantList: NextHandle(lastLocalConstantHandle),
                        startOffset: 0,
                        length: bodyOpt.IL.Length);
                }
                else
                {
                    foreach (LocalScope scope in bodyOpt.LocalScopes)
                    {
                        _debugMetadataOpt.AddLocalScope(
                            method: methodHandle,
                            importScope: importScopeHandle,
                            variableList: NextHandle(lastLocalVariableHandle),
                            constantList: NextHandle(lastLocalConstantHandle),
                            startOffset: scope.StartOffset,
                            length: scope.Length);

                        foreach (ILocalDefinition local in scope.Variables)
                        {
                            Debug.Assert(local.SlotIndex >= 0);

                            lastLocalVariableHandle = _debugMetadataOpt.AddLocalVariable(
                                attributes: local.PdbAttributes,
                                index: local.SlotIndex,
                                name: _debugMetadataOpt.GetOrAddString(local.Name));

                            SerializeLocalInfo(local, lastLocalVariableHandle);
                        }

                        foreach (ILocalDefinition constant in scope.Constants)
                        {
                            var mdConstant = constant.CompileTimeValue;
                            Debug.Assert(mdConstant != null);

                            lastLocalConstantHandle = _debugMetadataOpt.AddLocalConstant(
                                name: _debugMetadataOpt.GetOrAddString(constant.Name),
                                signature: SerializeLocalConstantSignature(constant));

                            SerializeLocalInfo(constant, lastLocalConstantHandle);
                        }
                    }
                }

                var moveNextBodyInfo = bodyOpt.MoveNextBodyInfo;
                if (moveNextBodyInfo != null)
                {
                    _debugMetadataOpt.AddStateMachineMethod(
                        moveNextMethod: methodHandle,
                        kickoffMethod: GetMethodDefinitionHandle(moveNextBodyInfo.KickoffMethod));

                    if (moveNextBodyInfo is AsyncMoveNextBodyDebugInfo asyncInfo)
                    {
                        SerializeAsyncMethodSteppingInfo(asyncInfo, methodHandle, aggregateMethodRid);
                    }
                }

                if (bodyOpt.IsPrimaryConstructor)
                {
                    _debugMetadataOpt.AddCustomDebugInformation(
                        parent: methodHandle,
                        kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.PrimaryConstructorInformationBlob),
                        value: default(BlobHandle));
                }

                SerializeStateMachineLocalScopes(bodyOpt, methodHandle);
            }

            // Emit EnC info for all methods even if they do not have sequence points.
            // The information facilitates reusing lambdas and closures. The reuse is important for runtimes that can't add new members (e.g. Mono).
            // EnC delta doesn't need this information - we use information recorded by previous generation emit.
            if (Context.Module.CommonCompilation.Options.EnableEditAndContinue && IsFullMetadata)
            {
                SerializeEncMethodDebugInformation(bodyOpt, methodHandle);
            }
        }

        private static LocalVariableHandle NextHandle(LocalVariableHandle handle) =>
            MetadataTokens.LocalVariableHandle(MetadataTokens.GetRowNumber(handle) + 1);

        private static LocalConstantHandle NextHandle(LocalConstantHandle handle) =>
            MetadataTokens.LocalConstantHandle(MetadataTokens.GetRowNumber(handle) + 1);

        private BlobHandle SerializeLocalConstantSignature(ILocalDefinition localConstant)
        {
            var builder = new BlobBuilder();

            // TODO: BlobEncoder.LocalConstantSignature

            // CustomMod*
            var encoder = new CustomModifiersEncoder(builder);
            SerializeCustomModifiers(encoder, localConstant.CustomModifiers);

            var type = localConstant.Type;
            var typeCode = type.TypeCode;

            object value = localConstant.CompileTimeValue.Value;

            // PrimitiveConstant or EnumConstant
            if (value is decimal)
            {
                builder.WriteByte((byte)SignatureTypeKind.ValueType);
                builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(GetTypeHandle(type)));

                builder.WriteDecimal((decimal)value);
            }
            else if (value is DateTime)
            {
                builder.WriteByte((byte)SignatureTypeKind.ValueType);
                builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(GetTypeHandle(type)));

                builder.WriteDateTime((DateTime)value);
            }
            else if (typeCode == PrimitiveTypeCode.String)
            {
                builder.WriteByte((byte)ConstantTypeCode.String);
                if (value == null)
                {
                    builder.WriteByte(0xff);
                }
                else
                {
                    builder.WriteUTF16((string)value);
                }
            }
            else if (value != null)
            {
                // TypeCode
                builder.WriteByte((byte)GetConstantTypeCode(value));

                // Value
                builder.WriteConstant(value);

                // EnumType
                if (type.IsEnum)
                {
                    builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(GetTypeHandle(type)));
                }
            }
            else if (this.module.IsPlatformType(type, PlatformType.SystemObject))
            {
                builder.WriteByte((byte)SignatureTypeCode.Object);
            }
            else
            {
                builder.WriteByte((byte)(type.IsValueType ? SignatureTypeKind.ValueType : SignatureTypeKind.Class));
                builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(GetTypeHandle(type)));
            }

            return _debugMetadataOpt.GetOrAddBlob(builder);
        }

        private static SignatureTypeCode GetConstantTypeCode(object value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a zero.
                return (SignatureTypeCode)SignatureTypeKind.Class;
            }

            Debug.Assert(!value.GetType().GetTypeInfo().IsEnum);

            // Perf: Note that JIT optimizes each expression val.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            if (value.GetType() == typeof(int))
            {
                return SignatureTypeCode.Int32;
            }

            if (value.GetType() == typeof(string))
            {
                return SignatureTypeCode.String;
            }

            if (value.GetType() == typeof(bool))
            {
                return SignatureTypeCode.Boolean;
            }

            if (value.GetType() == typeof(char))
            {
                return SignatureTypeCode.Char;
            }

            if (value.GetType() == typeof(byte))
            {
                return SignatureTypeCode.Byte;
            }

            if (value.GetType() == typeof(long))
            {
                return SignatureTypeCode.Int64;
            }

            if (value.GetType() == typeof(double))
            {
                return SignatureTypeCode.Double;
            }

            if (value.GetType() == typeof(short))
            {
                return SignatureTypeCode.Int16;
            }

            if (value.GetType() == typeof(ushort))
            {
                return SignatureTypeCode.UInt16;
            }

            if (value.GetType() == typeof(uint))
            {
                return SignatureTypeCode.UInt32;
            }

            if (value.GetType() == typeof(sbyte))
            {
                return SignatureTypeCode.SByte;
            }

            if (value.GetType() == typeof(ulong))
            {
                return SignatureTypeCode.UInt64;
            }

            if (value.GetType() == typeof(float))
            {
                return SignatureTypeCode.Single;
            }

            throw ExceptionUtilities.Unreachable();
        }

        #region ImportScope

        private static readonly ImportScopeHandle ModuleImportScopeHandle = MetadataTokens.ImportScopeHandle(1);

        private void SerializeImport(BlobBuilder writer, AssemblyReferenceAlias alias)
        {
            // <import> ::= AliasAssemblyReference <alias> <target-assembly>
            writer.WriteByte((byte)ImportDefinitionKind.AliasAssemblyReference);
            writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(alias.Name)));
            writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(GetOrAddAssemblyReferenceHandle(alias.Assembly)));
        }

        private void SerializeImport(BlobBuilder writer, UsedNamespaceOrType import)
        {
            if (import.TargetXmlNamespaceOpt != null)
            {
                Debug.Assert(import.TargetNamespaceOpt == null);
                Debug.Assert(import.TargetAssemblyOpt == null);
                Debug.Assert(import.TargetTypeOpt == null);

                // <import> ::= ImportXmlNamespace <alias> <target-namespace>
                writer.WriteByte((byte)ImportDefinitionKind.ImportXmlNamespace);
                writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(import.AliasOpt)));
                writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(import.TargetXmlNamespaceOpt)));
            }
            else if (import.TargetTypeOpt != null)
            {
                Debug.Assert(import.TargetNamespaceOpt == null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                if (import.AliasOpt != null)
                {
                    // <import> ::= AliasType <alias> <target-type>
                    writer.WriteByte((byte)ImportDefinitionKind.AliasType);
                    writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(import.AliasOpt)));
                }
                else
                {
                    // <import> ::= ImportType <target-type>
                    writer.WriteByte((byte)ImportDefinitionKind.ImportType);
                }

                writer.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(GetTypeHandle(import.TargetTypeOpt))); // TODO: index in release build
            }
            else if (import.TargetNamespaceOpt != null)
            {
                if (import.TargetAssemblyOpt != null)
                {
                    if (import.AliasOpt != null)
                    {
                        // <import> ::= AliasAssemblyNamespace <alias> <target-assembly> <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.AliasAssemblyNamespace);
                        writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(import.AliasOpt)));
                    }
                    else
                    {
                        // <import> ::= ImportAssemblyNamespace <target-assembly> <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.ImportAssemblyNamespace);
                    }

                    writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(GetAssemblyReferenceHandle(import.TargetAssemblyOpt)));
                }
                else
                {
                    if (import.AliasOpt != null)
                    {
                        // <import> ::= AliasNamespace <alias> <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.AliasNamespace);
                        writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(import.AliasOpt)));
                    }
                    else
                    {
                        // <import> ::= ImportNamespace <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.ImportNamespace);
                    }
                }

                // TODO: cache?
                string namespaceName = TypeNameSerializer.BuildQualifiedNamespaceName(import.TargetNamespaceOpt);
                writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(namespaceName)));
            }
            else
            {
                // <import> ::= ImportReferenceAlias <alias>
                Debug.Assert(import.AliasOpt != null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                writer.WriteByte((byte)ImportDefinitionKind.ImportAssemblyReferenceAlias);
                writer.WriteCompressedInteger(MetadataTokens.GetHeapOffset(_debugMetadataOpt.GetOrAddBlobUTF8(import.AliasOpt)));
            }
        }

        private void DefineModuleImportScope()
        {
            // module-level import scope:
            var writer = new BlobBuilder();

            SerializeModuleDefaultNamespace();

            foreach (AssemblyReferenceAlias alias in module.GetAssemblyReferenceAliases(Context))
            {
                SerializeImport(writer, alias);
            }

            foreach (UsedNamespaceOrType import in module.GetImports())
            {
                SerializeImport(writer, import);
            }

            var rid = _debugMetadataOpt.AddImportScope(
                parentScope: default(ImportScopeHandle),
                imports: _debugMetadataOpt.GetOrAddBlob(writer));

            Debug.Assert(rid == ModuleImportScopeHandle);
        }

        private ImportScopeHandle GetImportScopeIndex(IImportScope scope, Dictionary<IImportScope, ImportScopeHandle> scopeIndex)
        {
            ImportScopeHandle scopeHandle;
            if (scopeIndex.TryGetValue(scope, out scopeHandle))
            {
                // scope is already indexed:
                return scopeHandle;
            }

            var parent = scope.Parent;
            var parentScopeHandle = (parent != null) ? GetImportScopeIndex(scope.Parent, scopeIndex) : ModuleImportScopeHandle;

            var result = _debugMetadataOpt.AddImportScope(
                parentScope: parentScopeHandle,
                imports: SerializeImportsBlob(scope));

            scopeIndex.Add(scope, result);
            return result;
        }

        private BlobHandle SerializeImportsBlob(IImportScope scope)
        {
            var writer = new BlobBuilder();

            foreach (UsedNamespaceOrType import in scope.GetUsedNamespaces(Context))
            {
                SerializeImport(writer, import);
            }

            return _debugMetadataOpt.GetOrAddBlob(writer);
        }

        private void SerializeModuleDefaultNamespace()
        {
            // C#: DefaultNamespace is null.
            // VB: DefaultNamespace is non-null.
            if (module.DefaultNamespace == null)
            {
                return;
            }

            _debugMetadataOpt.AddCustomDebugInformation(
                parent: EntityHandle.ModuleDefinition,
                kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.DefaultNamespace),
                value: _debugMetadataOpt.GetOrAddBlobUTF8(module.DefaultNamespace));
        }

        #endregion

        #region Locals

        private void SerializeLocalInfo(ILocalDefinition local, EntityHandle parent)
        {
            var dynamicFlags = local.DynamicTransformFlags;
            if (!dynamicFlags.IsEmpty)
            {
                var value = SerializeBitVector(dynamicFlags);

                _debugMetadataOpt.AddCustomDebugInformation(
                    parent: parent,
                    kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.DynamicLocalVariables),
                    value: _debugMetadataOpt.GetOrAddBlob(value));
            }

            var tupleElementNames = local.TupleElementNames;
            if (!tupleElementNames.IsEmpty)
            {
                var builder = new BlobBuilder();
                SerializeTupleElementNames(builder, tupleElementNames);

                _debugMetadataOpt.AddCustomDebugInformation(
                    parent: parent,
                    kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.TupleElementNames),
                    value: _debugMetadataOpt.GetOrAddBlob(builder));
            }
        }

        private static ImmutableArray<byte> SerializeBitVector(ImmutableArray<bool> vector)
        {
            var builder = ArrayBuilder<byte>.GetInstance();

            int b = 0;
            int shift = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                if (vector[i])
                {
                    b |= 1 << shift;
                }

                if (shift == 7)
                {
                    builder.Add((byte)b);
                    b = 0;
                    shift = 0;
                }
                else
                {
                    shift++;
                }
            }

            if (b != 0)
            {
                builder.Add((byte)b);
            }
            else
            {
                // trim trailing zeros:
                int lastNonZero = builder.Count - 1;
                while (builder[lastNonZero] == 0)
                {
                    lastNonZero--;
                }

                builder.Clip(lastNonZero + 1);
            }

            return builder.ToImmutableAndFree();
        }

        private static void SerializeTupleElementNames(BlobBuilder builder, ImmutableArray<string> names)
        {
            foreach (var name in names)
            {
                WriteUtf8String(builder, name ?? string.Empty);
            }
        }

        /// <summary>
        /// Write string as UTF-8 with null terminator.
        /// </summary>
        private static void WriteUtf8String(BlobBuilder builder, string str)
        {
            builder.WriteUTF8(str);
            builder.WriteByte(0);
        }

        #endregion

        #region State Machines

        private void SerializeAsyncMethodSteppingInfo(AsyncMoveNextBodyDebugInfo asyncInfo, MethodDefinitionHandle moveNextMethod, int aggregateMethodDefRid)
        {
            Debug.Assert(asyncInfo.ResumeOffsets.Length == asyncInfo.YieldOffsets.Length);
            Debug.Assert(asyncInfo.CatchHandlerOffset >= -1);

            var writer = new BlobBuilder();

            writer.WriteUInt32((uint)((long)asyncInfo.CatchHandlerOffset + 1));

            for (int i = 0; i < asyncInfo.ResumeOffsets.Length; i++)
            {
                writer.WriteUInt32((uint)asyncInfo.YieldOffsets[i]);
                writer.WriteUInt32((uint)asyncInfo.ResumeOffsets[i]);
                writer.WriteCompressedInteger(aggregateMethodDefRid);
            }

            _debugMetadataOpt.AddCustomDebugInformation(
                parent: moveNextMethod,
                kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.AsyncMethodSteppingInformationBlob),
                value: _debugMetadataOpt.GetOrAddBlob(writer));
        }

        private void SerializeStateMachineLocalScopes(IMethodBody methodBody, MethodDefinitionHandle method)
        {
            var scopes = methodBody.StateMachineHoistedLocalScopes;
            if (scopes.IsDefaultOrEmpty)
            {
                return;
            }

            var writer = new BlobBuilder();

            foreach (var scope in scopes)
            {
                writer.WriteUInt32((uint)scope.StartOffset);
                writer.WriteUInt32((uint)scope.Length);
            }

            _debugMetadataOpt.AddCustomDebugInformation(
                parent: method,
                kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes),
                value: _debugMetadataOpt.GetOrAddBlob(writer));
        }

        #endregion

        #region Sequence Points

        private BlobHandle SerializeSequencePoints(
            StandaloneSignatureHandle localSignatureHandleOpt,
            ImmutableArray<SequencePoint> sequencePoints,
            Dictionary<DebugSourceDocument, DocumentHandle> documentIndex,
            out DocumentHandle singleDocumentHandle)
        {
            if (sequencePoints.Length == 0)
            {
                singleDocumentHandle = default(DocumentHandle);
                return default(BlobHandle);
            }

            var writer = PooledBlobBuilder.GetInstance();

            int previousNonHiddenStartLine = -1;
            int previousNonHiddenStartColumn = -1;

            // header:
            writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(localSignatureHandleOpt));

            var previousDocument = TryGetSingleDocument(sequencePoints);
            singleDocumentHandle = (previousDocument != null) ? GetOrAddDocument(previousDocument, documentIndex) : default(DocumentHandle);

            for (int i = 0; i < sequencePoints.Length; i++)
            {
                var currentDocument = sequencePoints[i].Document;
                if (previousDocument != currentDocument)
                {
                    var documentHandle = GetOrAddDocument(currentDocument, documentIndex);

                    // optional document in header or document record:
                    if (previousDocument != null)
                    {
                        writer.WriteCompressedInteger(0);
                    }

                    writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(documentHandle));
                    previousDocument = currentDocument;
                }

                // delta IL offset:
                if (i > 0)
                {
                    writer.WriteCompressedInteger(sequencePoints[i].Offset - sequencePoints[i - 1].Offset);
                }
                else
                {
                    writer.WriteCompressedInteger(sequencePoints[i].Offset);
                }

                if (sequencePoints[i].IsHidden)
                {
                    writer.WriteInt16(0);
                    continue;
                }

                // Delta Lines & Columns:
                SerializeDeltaLinesAndColumns(writer, sequencePoints[i]);

                // delta Start Lines & Columns:
                if (previousNonHiddenStartLine < 0)
                {
                    Debug.Assert(previousNonHiddenStartColumn < 0);
                    writer.WriteCompressedInteger(sequencePoints[i].StartLine);
                    writer.WriteCompressedInteger(sequencePoints[i].StartColumn);
                }
                else
                {
                    writer.WriteCompressedSignedInteger(sequencePoints[i].StartLine - previousNonHiddenStartLine);
                    writer.WriteCompressedSignedInteger(sequencePoints[i].StartColumn - previousNonHiddenStartColumn);
                }

                previousNonHiddenStartLine = sequencePoints[i].StartLine;
                previousNonHiddenStartColumn = sequencePoints[i].StartColumn;
            }

            return _debugMetadataOpt.GetOrAddBlobAndFree(writer);
        }

        private static DebugSourceDocument TryGetSingleDocument(ImmutableArray<SequencePoint> sequencePoints)
        {
            DebugSourceDocument singleDocument = sequencePoints[0].Document;
            for (int i = 1; i < sequencePoints.Length; i++)
            {
                if (sequencePoints[i].Document != singleDocument)
                {
                    return null;
                }
            }

            return singleDocument;
        }

        private void SerializeDeltaLinesAndColumns(BlobBuilder writer, SequencePoint sequencePoint)
        {
            int deltaLines = sequencePoint.EndLine - sequencePoint.StartLine;
            int deltaColumns = sequencePoint.EndColumn - sequencePoint.StartColumn;

            // only hidden sequence points have zero width
            Debug.Assert(deltaLines != 0 || deltaColumns != 0 || sequencePoint.IsHidden);

            writer.WriteCompressedInteger(deltaLines);

            if (deltaLines == 0)
            {
                writer.WriteCompressedInteger(deltaColumns);
            }
            else
            {
                writer.WriteCompressedSignedInteger(deltaColumns);
            }
        }

        #endregion

        #region Documents

        private DocumentHandle GetOrAddDocument(DebugSourceDocument document, Dictionary<DebugSourceDocument, DocumentHandle> index)
        {
            if (index.TryGetValue(document, out var documentHandle))
            {
                return documentHandle;
            }

            return AddDocument(document, index);
        }

        private DocumentHandle AddDocument(DebugSourceDocument document, Dictionary<DebugSourceDocument, DocumentHandle> index)
        {
            DocumentHandle documentHandle;
            DebugSourceInfo info = document.GetSourceInfo();

            var name = document.Location;
            if (_usingNonSourceDocumentNameEnumerator)
            {
                var result = _nonSourceDocumentNameEnumerator.MoveNext();
                Debug.Assert(result);
                name = _nonSourceDocumentNameEnumerator.Current;
            }

            documentHandle = _debugMetadataOpt.AddDocument(
                name: _debugMetadataOpt.GetOrAddDocumentName(name),
                hashAlgorithm: info.Checksum.IsDefault ? default(GuidHandle) : _debugMetadataOpt.GetOrAddGuid(info.ChecksumAlgorithmId),
                hash: info.Checksum.IsDefault ? default(BlobHandle) : _debugMetadataOpt.GetOrAddBlob(info.Checksum),
                language: _debugMetadataOpt.GetOrAddGuid(document.Language));

            index.Add(document, documentHandle);

            if (info.EmbeddedTextBlob != null)
            {
                _debugMetadataOpt.AddCustomDebugInformation(
                    parent: documentHandle,
                    kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.EmbeddedSource),
                    value: _debugMetadataOpt.GetOrAddBlob(info.EmbeddedTextBlob));
            }

            return documentHandle;
        }

        /// <summary>
        /// Add document entries for all debug documents that do not yet have an entry.
        /// </summary>
        /// <remarks>
        /// This is done after serializing method debug info to ensure that we embed all requested
        /// text even if there are no corresponding sequence points.
        /// </remarks>
        public void AddRemainingDebugDocuments(IReadOnlyDictionary<string, DebugSourceDocument> documents)
        {
            foreach (var kvp in documents
                .Where(kvp => !_documentIndex.ContainsKey(kvp.Value))
                .OrderBy(kvp => kvp.Key))
            {
                AddDocument(kvp.Value, _documentIndex);
            }
        }

        #endregion

        #region Edit and Continue

        private void SerializeEncMethodDebugInformation(IMethodBody methodBody, MethodDefinitionHandle method)
        {
            var encInfo = GetEncMethodDebugInfo(methodBody);

            if (!encInfo.LocalSlots.IsDefaultOrEmpty)
            {
                var writer = PooledBlobBuilder.GetInstance();

                encInfo.SerializeLocalSlots(writer);

                _debugMetadataOpt.AddCustomDebugInformation(
                    parent: method,
                    kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.EncLocalSlotMap),
                    value: _debugMetadataOpt.GetOrAddBlobAndFree(writer));
            }

            if (!encInfo.Lambdas.IsDefaultOrEmpty)
            {
                var writer = PooledBlobBuilder.GetInstance();

                encInfo.SerializeLambdaMap(writer);

                _debugMetadataOpt.AddCustomDebugInformation(
                    parent: method,
                    kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.EncLambdaAndClosureMap),
                    value: _debugMetadataOpt.GetOrAddBlobAndFree(writer));
            }

            if (!encInfo.StateMachineStates.IsDefaultOrEmpty)
            {
                var writer = PooledBlobBuilder.GetInstance();

                encInfo.SerializeStateMachineStates(writer);

                _debugMetadataOpt.AddCustomDebugInformation(
                    parent: method,
                    kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.EncStateMachineStateMap),
                    value: _debugMetadataOpt.GetOrAddBlobAndFree(writer));
            }
        }

        #endregion

        private void EmbedSourceLink(Stream stream)
        {
            byte[] bytes;

            try
            {
                bytes = stream.ReadAllBytes();
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                throw new SymUnmanagedWriterException(e.Message, e);
            }

            _debugMetadataOpt.AddCustomDebugInformation(
                parent: EntityHandle.ModuleDefinition,
                kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.SourceLink),
                value: _debugMetadataOpt.GetOrAddBlob(bytes));
        }

        ///<summary>The version of the compilation options schema to be written to the PDB.</summary>
        private const int CompilationOptionsSchemaVersion = 2;

        /// <summary>
        /// Capture the set of compilation options to allow a compilation 
        /// to be reconstructed from the pdb
        /// </summary>
        private void EmbedCompilationOptions(CommonPEModuleBuilder module)
        {
            var builder = new BlobBuilder();

            if (this.Context.RebuildData is { } rebuildData)
            {
                var reader = rebuildData.OptionsBlobReader;
                builder.WriteBytes(reader.ReadBytes(reader.RemainingBytes));
            }
            else
            {
                var compilerVersion = typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                WriteValue(CompilationOptionNames.CompilationOptionsVersion, CompilationOptionsSchemaVersion.ToString());
                WriteValue(CompilationOptionNames.CompilerVersion, compilerVersion);

                WriteValue(CompilationOptionNames.Language, module.CommonCompilation.Options.Language);
                WriteValue(CompilationOptionNames.SourceFileCount, module.CommonCompilation.SyntaxTrees.Count().ToString());
                WriteValue(CompilationOptionNames.OutputKind, module.OutputKind.ToString());

                if (module.EmitOptions.FallbackSourceFileEncoding != null)
                {
                    WriteValue(CompilationOptionNames.FallbackEncoding, module.EmitOptions.FallbackSourceFileEncoding.WebName);
                }

                if (module.EmitOptions.DefaultSourceFileEncoding != null)
                {
                    WriteValue(CompilationOptionNames.DefaultEncoding, module.EmitOptions.DefaultSourceFileEncoding.WebName);
                }

                int portabilityPolicy = 0;
                if (module.CommonCompilation.Options.AssemblyIdentityComparer is DesktopAssemblyIdentityComparer identityComparer)
                {
                    portabilityPolicy |= identityComparer.PortabilityPolicy.SuppressSilverlightLibraryAssembliesPortability ? 0b1 : 0;
                    portabilityPolicy |= identityComparer.PortabilityPolicy.SuppressSilverlightPlatformAssembliesPortability ? 0b10 : 0;
                }

                if (portabilityPolicy != 0)
                {
                    WriteValue(CompilationOptionNames.PortabilityPolicy, portabilityPolicy.ToString());
                }

                var optimizationLevel = module.CommonCompilation.Options.OptimizationLevel;
                var debugPlusMode = module.CommonCompilation.Options.DebugPlusMode;
                if ((optimizationLevel, debugPlusMode) != OptimizationLevelFacts.DefaultValues)
                {
                    WriteValue(CompilationOptionNames.Optimization, optimizationLevel.ToPdbSerializedString(debugPlusMode));
                }

                var platform = module.CommonCompilation.Options.Platform;
                WriteValue(CompilationOptionNames.Platform, platform.ToString());

                var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                WriteValue(CompilationOptionNames.RuntimeVersion, runtimeVersion);

                module.CommonCompilation.SerializePdbEmbeddedCompilationOptions(builder);
            }

            _debugMetadataOpt.AddCustomDebugInformation(
                parent: EntityHandle.ModuleDefinition,
                kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.CompilationOptions),
                value: _debugMetadataOpt.GetOrAddBlob(builder));

            void WriteValue(string key, string value)
            {
                builder.WriteUTF8(key);
                builder.WriteByte(0);
                builder.WriteUTF8(value);
                builder.WriteByte(0);
            }
        }

        /// <summary>
        /// Writes information about metadata references to the pdb so the same
        /// reference can be found on sourcelink to create the compilation again
        /// </summary>
        private void EmbedMetadataReferenceInformation(CommonPEModuleBuilder module)
        {
            var builder = new BlobBuilder();

            // Order of information
            // File name (null terminated string): A.exe
            // Extern Alias (null terminated string): a1,a2,a3
            // MetadataImageKind (byte)
            // EmbedInteropTypes (boolean)
            // COFF header Timestamp field (4 byte int)
            // COFF header SizeOfImage field (4 byte int)
            // MVID (Guid, 24 bytes)
            var referenceManager = module.CommonCompilation.GetBoundReferenceManager();
            foreach (var pair in referenceManager.GetReferencedAssemblyAliases())
            {
                if (referenceManager.GetMetadataReference(pair.AssemblySymbol) is PortableExecutableReference { FilePath: { } } portableReference)
                {
                    var fileName = PathUtilities.GetFileName(portableReference.FilePath);
                    var peReader = pair.AssemblySymbol.GetISymbol() is IAssemblySymbol assemblySymbol
                        ? assemblySymbol.GetMetadata().GetAssembly().ManifestModule.PEReaderOpt
                        : null;

                    // Don't write before checking that we can get a peReader for the metadata reference
                    if (peReader is null)
                        continue;

                    // Write file name first
                    builder.WriteUTF8(fileName);

                    // Make sure to add null terminator
                    builder.WriteByte(0);

                    // Extern alias
                    if (pair.Aliases.Length > 0)
                        builder.WriteUTF8(string.Join(",", pair.Aliases.OrderBy(StringComparer.Ordinal)));

                    // Always null terminate the extern alias list
                    builder.WriteByte(0);

                    byte kindAndEmbedInteropTypes = (byte)(portableReference.Properties.EmbedInteropTypes
                        ? 0b10
                        : 0b0);

                    kindAndEmbedInteropTypes |= portableReference.Properties.Kind switch
                    {
                        MetadataImageKind.Assembly => 1,
                        MetadataImageKind.Module => 0,
                        _ => throw ExceptionUtilities.UnexpectedValue(portableReference.Properties.Kind)
                    };

                    builder.WriteByte(kindAndEmbedInteropTypes);
                    builder.WriteInt32(peReader.PEHeaders.CoffHeader.TimeDateStamp);
                    builder.WriteInt32(peReader.PEHeaders.PEHeader.SizeOfImage);

                    var metadataReader = peReader.GetMetadataReader();
                    var moduleDefinition = metadataReader.GetModuleDefinition();
                    builder.WriteGuid(metadataReader.GetGuid(moduleDefinition.Mvid));
                }
            }

            _debugMetadataOpt.AddCustomDebugInformation(
                parent: EntityHandle.ModuleDefinition,
                kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.CompilationMetadataReferences),
                value: _debugMetadataOpt.GetOrAddBlob(builder));
        }

        private void EmbedTypeDefinitionDocumentInformation(CommonPEModuleBuilder module)
        {
            var builder = new BlobBuilder();

            foreach (var (definition, documents) in module.GetTypeToDebugDocumentMap(Context))
            {
                foreach (var document in documents)
                {
                    var handle = GetOrAddDocument(document, _documentIndex);
                    builder.WriteCompressedInteger(MetadataTokens.GetRowNumber(handle));
                }
                _debugMetadataOpt.AddCustomDebugInformation(
                    parent: GetTypeDefinitionHandle(definition),
                    kind: _debugMetadataOpt.GetOrAddGuid(PortableCustomDebugInfoKinds.TypeDefinitionDocuments),
                    value: _debugMetadataOpt.GetOrAddBlob(builder));
                builder.Clear();
            }
        }

    }
}
