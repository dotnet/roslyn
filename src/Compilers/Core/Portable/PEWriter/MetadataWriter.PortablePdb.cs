// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace Microsoft.Cci
{
    partial class MetadataWriter
    {
        private readonly Dictionary<DebugSourceDocument, int> _documentIndex = new Dictionary<DebugSourceDocument, int>();
        private readonly Dictionary<IImportScope, int> _scopeIndex = new Dictionary<IImportScope, int>();

        private void SerializeMethodDebugInfo(IMethodBody bodyOpt, int methodRid, int localSignatureRowId, ref int lastLocalVariableRid, ref int lastLocalConstantRid)
        {
            if (bodyOpt == null)
            {
                _debugBuilderOpt.AddMethodDebugInformation(0, new BlobIdx(0));
                return;
            }

            bool isIterator = bodyOpt.StateMachineTypeName != null;
            bool emitDebugInfo = isIterator || bodyOpt.HasAnySequencePoints;

            if (!emitDebugInfo)
            {
                _debugBuilderOpt.AddMethodDebugInformation(0, new BlobIdx(0));
                return;
            }

            var bodyImportScope = bodyOpt.ImportScope;
            int importScopeRid = (bodyImportScope != null) ? GetImportScopeIndex(bodyImportScope, _scopeIndex) : 0;

            // documents & sequence points:
            int singleDocumentRowId;
            BlobIdx sequencePointsBlob = SerializeSequencePoints(localSignatureRowId, bodyOpt.GetSequencePoints(), _documentIndex, out singleDocumentRowId);
            _debugBuilderOpt.AddMethodDebugInformation(documentRowId: singleDocumentRowId, sequencePoints: sequencePointsBlob);

            // Unlike native PDB we don't emit an empty root scope.
            // scopes are already ordered by StartOffset ascending then by EndOffset descending (the longest scope first).

            if (bodyOpt.LocalScopes.Length == 0)
            {
                // TODO: the compiler should produce a scope for each debuggable method 
                _debugBuilderOpt.AddLocalScope(
                    methodRowId: methodRid,
                    importScopeRowId: importScopeRid,
                    variableList: lastLocalVariableRid + 1,
                    constantList: lastLocalConstantRid + 1,
                    startOffset: 0,
                    length: bodyOpt.IL.Length);
            }
            else
            {
                foreach (LocalScope scope in bodyOpt.LocalScopes)
                {
                    _debugBuilderOpt.AddLocalScope(
                        methodRowId: methodRid,
                        importScopeRowId: importScopeRid,
                        variableList: lastLocalVariableRid + 1,
                        constantList: lastLocalConstantRid + 1,
                        startOffset: scope.StartOffset,
                        length: scope.Length);

                    foreach (ILocalDefinition local in scope.Variables)
                    {
                        Debug.Assert(local.SlotIndex >= 0);

                        lastLocalVariableRid = _debugBuilderOpt.AddLocalVariable(
                            attributes: (ushort)local.PdbAttributes,
                            index: local.SlotIndex,
                            name: _debugBuilderOpt.GetStringIndex(local.Name));

                        SerializeDynamicLocalInfo(local, rowId: lastLocalVariableRid, isConstant: false);
                    }

                    foreach (ILocalDefinition constant in scope.Constants)
                    {
                        var mdConstant = constant.CompileTimeValue;
                        Debug.Assert(mdConstant != null);

                        lastLocalConstantRid = _debugBuilderOpt.AddLocalConstant(
                            name: _debugBuilderOpt.GetStringIndex(constant.Name),
                            signature: SerializeLocalConstantSignature(constant));

                        SerializeDynamicLocalInfo(constant, rowId: lastLocalConstantRid, isConstant: true);
                    }
                }
            }

            var asyncDebugInfo = bodyOpt.AsyncDebugInfo;
            if (asyncDebugInfo != null)
            {
                _debugBuilderOpt.AddStateMachineMethod(
                    moveNextMethodRowId: methodRid,
                    kickoffMethodRowId: GetMethodDefIndex(asyncDebugInfo.KickoffMethod));

                SerializeAsyncMethodSteppingInfo(asyncDebugInfo, methodRid);
            }

            SerializeStateMachineLocalScopes(bodyOpt, methodRid);

            // delta doesn't need this information - we use information recorded by previous generation emit
            if (Context.ModuleBuilder.CommonCompilation.Options.EnableEditAndContinue && !IsFullMetadata)
            {
                SerializeEncMethodDebugInformation(bodyOpt, methodRid);
            }
        }

        private BlobIdx SerializeLocalConstantSignature(ILocalDefinition localConstant)
        {
            var writer = new BlobBuilder();

            // CustomMod*
            SerializeCustomModifiers(localConstant.CustomModifiers, writer);

            var type = localConstant.Type;
            var typeCode = type.TypeCode(Context);

            object value = localConstant.CompileTimeValue.Value;

            // PrimitiveConstant or EnumConstant
            if (value is decimal)
            {
                writer.WriteByte(0x11);
                writer.WriteCompressedInteger(GetTypeDefOrRefCodedIndex(type, treatRefAsPotentialTypeSpec: true));

                writer.WriteDecimal((decimal)value);
            }
            else if (value is DateTime)
            {
                writer.WriteByte(0x11);
                writer.WriteCompressedInteger(GetTypeDefOrRefCodedIndex(type, treatRefAsPotentialTypeSpec: true));

                writer.WriteDateTime((DateTime)value);
            }
            else if (typeCode == PrimitiveTypeCode.String)
            {
                writer.WriteByte((byte)ConstantTypeCode.String);
                if (value == null)
                {
                    writer.WriteByte(0xff);
                }
                else
                {
                    writer.WriteUTF16((string)value);
                }
            }
            else if (value != null)
            {
                // TypeCode
                writer.WriteByte((byte)MetadataWriterUtilities.GetConstantTypeCode(value));

                // Value
                writer.WriteConstant(value);

                // EnumType
                if (type.IsEnum)
                {
                    writer.WriteCompressedInteger(GetTypeDefOrRefCodedIndex(type, treatRefAsPotentialTypeSpec: true));
                }
            }
            else if (this.module.IsPlatformType(type, PlatformType.SystemObject))
            {
                writer.WriteByte(0x1c);
            }
            else
            {
                writer.WriteByte((byte)(type.IsValueType ? 0x11 : 0x12));
                writer.WriteCompressedInteger(GetTypeDefOrRefCodedIndex(type, treatRefAsPotentialTypeSpec: true));
            }

            return _debugBuilderOpt.GetBlobIndex(writer);
        }

        private static uint HasCustomDebugInformation(HasCustomDebugInformationTag tag, int rowId)
        {
            return (uint)(rowId << 5) | (uint)tag;
        }

        private enum HasCustomDebugInformationTag
        {
            MethodDef = 0,
            Module = 7,
            Assembly = 14,
            LocalVariable = 24,
            LocalConstant = 25,
        }

        #region ImportScope

        private const int ModuleImportScopeRid = 1;

        private void SerializeImport(BlobBuilder writer, AssemblyReferenceAlias alias)
        {
            // <import> ::= AliasAssemblyReference <alias> <target-assembly>
            writer.WriteByte((byte)ImportDefinitionKind.AliasAssemblyReference);
            writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(alias.Name)));
            writer.WriteCompressedInteger((uint)GetOrAddAssemblyRefIndex(alias.Assembly));
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
                writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(import.AliasOpt)));
                writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(import.TargetXmlNamespaceOpt)));
            }
            else if (import.TargetTypeOpt != null)
            {
                Debug.Assert(import.TargetNamespaceOpt == null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                if (import.AliasOpt != null)
                {
                    // <import> ::= AliasType <alias> <target-type>
                    writer.WriteByte((byte)ImportDefinitionKind.AliasType);
                    writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(import.AliasOpt)));
                }
                else
                {
                    // <import> ::= ImportType <target-type>
                    writer.WriteByte((byte)ImportDefinitionKind.ImportType);
                }

                writer.WriteCompressedInteger(GetTypeDefOrRefCodedIndex(import.TargetTypeOpt, treatRefAsPotentialTypeSpec: true)); // TODO: index in release build         
            }
            else if (import.TargetNamespaceOpt != null)
            {
                if (import.TargetAssemblyOpt != null)
                {
                    if (import.AliasOpt != null)
                    {
                        // <import> ::= AliasAssemblyNamespace <alias> <target-assembly> <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.AliasAssemblyNamespace);
                        writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(import.AliasOpt)));
                    }
                    else
                    {
                        // <import> ::= ImportAssemblyNamespace <target-assembly> <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.ImportAssemblyNamespace);
                    }

                    writer.WriteCompressedInteger((uint)GetAssemblyRefIndex(import.TargetAssemblyOpt));
                }
                else
                {
                    if (import.AliasOpt != null)
                    {
                        // <import> ::= AliasNamespace <alias> <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.AliasNamespace);
                        writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(import.AliasOpt)));
                    }
                    else
                    {
                        // <import> ::= ImportNamespace <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.ImportNamespace);
                    }
                }

                // TODO: cache?
                string namespaceName = TypeNameSerializer.BuildQualifiedNamespaceName(import.TargetNamespaceOpt);
                writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(namespaceName)));
            } 
            else
            {
                // <import> ::= ImportReferenceAlias <alias>
                Debug.Assert(import.AliasOpt != null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                writer.WriteByte((byte)ImportDefinitionKind.ImportAssemblyReferenceAlias);
                writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(_debugBuilderOpt.GetBlobIndexUtf8(import.AliasOpt)));
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

            int rid = _debugBuilderOpt.AddImportScope(
                parentScopeRowId: 0,
                imports: _debugBuilderOpt.GetBlobIndex(writer));

            Debug.Assert(rid == ModuleImportScopeRid);
        }

        private int GetImportScopeIndex(IImportScope scope, Dictionary<IImportScope, int> scopeIndex)
        {
            int scopeRid;
            if (scopeIndex.TryGetValue(scope, out scopeRid))
            {
                // scope is already indexed:
                return scopeRid;
            }

            var parent = scope.Parent;
            int parentScopeRid = (parent != null) ? GetImportScopeIndex(scope.Parent, scopeIndex) : ModuleImportScopeRid;

            int rid = _debugBuilderOpt.AddImportScope(
                parentScopeRowId: parentScopeRid,
                imports: SerializeImportsBlob(scope));

            scopeIndex.Add(scope, rid);
            return rid;
        }

        private BlobIdx SerializeImportsBlob(IImportScope scope)
        {
            var writer = new BlobBuilder();

            foreach (UsedNamespaceOrType import in scope.GetUsedNamespaces())
            {
                SerializeImport(writer, import);
            }

            return _debugBuilderOpt.GetBlobIndex(writer);
        }

        private void SerializeModuleDefaultNamespace()
        {
            // C#: DefaultNamespace is null.
            // VB: DefaultNamespace is non-null.
            if (module.DefaultNamespace == null)
            {
                return;
            }

            _debugBuilderOpt.AddCustomDebugInformation(
                parent: HasCustomDebugInformation(HasCustomDebugInformationTag.Module, 1),
                kind: _debugBuilderOpt.GetGuidIndex(PortableCustomDebugInfoKinds.DefaultNamespace),
                value: _debugBuilderOpt.GetBlobIndexUtf8(module.DefaultNamespace));
        }

        #endregion

        #region Locals

        private void SerializeDynamicLocalInfo(ILocalDefinition local, int rowId, bool isConstant)
        {
            var dynamicFlags = local.DynamicTransformFlags;
            if (dynamicFlags.IsDefault)
            {
                return;
            }

            var value = SerializeBitVector(dynamicFlags);

            var tag = isConstant ? HasCustomDebugInformationTag.LocalConstant : HasCustomDebugInformationTag.LocalVariable;

            _debugBuilderOpt.AddCustomDebugInformation(
                parent: HasCustomDebugInformation(tag, rowId),
                kind: _debugBuilderOpt.GetGuidIndex(PortableCustomDebugInfoKinds.DynamicLocalVariables),
                value: _debugBuilderOpt.GetBlobIndex(value));
        }

        private static ImmutableArray<byte> SerializeBitVector(ImmutableArray<TypedConstant> vector)
        {
            var builder = ArrayBuilder<byte>.GetInstance();

            int b = 0;
            int shift = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                if ((bool)vector[i].Value)
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

        #endregion
        
        #region State Machines

        private void SerializeAsyncMethodSteppingInfo(AsyncMethodBodyDebugInfo asyncInfo, int moveNextMethodRid)
        {
            Debug.Assert(asyncInfo.ResumeOffsets.Length == asyncInfo.YieldOffsets.Length);
            Debug.Assert(asyncInfo.CatchHandlerOffset >= -1);

            var writer = new BlobBuilder();

            writer.WriteUInt32((uint)((long)asyncInfo.CatchHandlerOffset + 1));

            for (int i = 0; i < asyncInfo.ResumeOffsets.Length; i++)
            {
                writer.WriteUInt32((uint)asyncInfo.YieldOffsets[i]);
                writer.WriteUInt32((uint)asyncInfo.ResumeOffsets[i]);
                writer.WriteCompressedInteger((uint)moveNextMethodRid);
            }

            _debugBuilderOpt.AddCustomDebugInformation(
                parent: HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, moveNextMethodRid),
                kind: _debugBuilderOpt.GetGuidIndex(PortableCustomDebugInfoKinds.AsyncMethodSteppingInformationBlob),
                value: _debugBuilderOpt.GetBlobIndex(writer));
        }

        private void SerializeStateMachineLocalScopes(IMethodBody methodBody, int methodRowId)
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

            _debugBuilderOpt.AddCustomDebugInformation(
                parent: HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, methodRowId),
                kind: _debugBuilderOpt.GetGuidIndex(PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes),
                value: _debugBuilderOpt.GetBlobIndex(writer));
        }

        #endregion

        #region Sequence Points

        private BlobIdx SerializeSequencePoints(
            int localSignatureRowId,
            ImmutableArray<SequencePoint> sequencePoints,
            Dictionary<DebugSourceDocument, int> documentIndex,
            out int singleDocumentRowId)
        {
            if (sequencePoints.Length == 0)
            {
                singleDocumentRowId = 0;
                return default(BlobIdx);
            }

            var writer = new BlobBuilder();

            int previousNonHiddenStartLine = -1;
            int previousNonHiddenStartColumn = -1;

            // header:
            writer.WriteCompressedInteger((uint)localSignatureRowId);

            var previousDocument = TryGetSingleDocument(sequencePoints);
            singleDocumentRowId = (previousDocument != null) ? GetOrAddDocument(previousDocument, documentIndex) : 0;

            for (int i = 0; i < sequencePoints.Length; i++)
            {
                var currentDocument = sequencePoints[i].Document;
                if (previousDocument != currentDocument)
                {
                    int documentRowId = GetOrAddDocument(currentDocument, documentIndex);

                    // optional document in header or document record:
                    if (previousDocument != null)
                    {
                        writer.WriteCompressedInteger(0);
                    }

                    writer.WriteCompressedInteger((uint)documentRowId);
                    previousDocument = currentDocument;
                }

                // delta IL offset:
                if (i > 0)
                {
                    writer.WriteCompressedInteger((uint)(sequencePoints[i].Offset - sequencePoints[i - 1].Offset));
                }
                else
                {
                    writer.WriteCompressedInteger((uint)sequencePoints[i].Offset);
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
                    writer.WriteCompressedInteger((uint)sequencePoints[i].StartLine);
                    writer.WriteCompressedInteger((uint)sequencePoints[i].StartColumn);
                }
                else
                {
                    writer.WriteCompressedSignedInteger(sequencePoints[i].StartLine - previousNonHiddenStartLine);
                    writer.WriteCompressedSignedInteger(sequencePoints[i].StartColumn - previousNonHiddenStartColumn);
                }

                previousNonHiddenStartLine = sequencePoints[i].StartLine;
                previousNonHiddenStartColumn = sequencePoints[i].StartColumn;
            }

            return _debugBuilderOpt.GetBlobIndex(writer);
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

            writer.WriteCompressedInteger((uint)deltaLines);

            if (deltaLines == 0)
            {
                writer.WriteCompressedInteger((uint)deltaColumns);
            }
            else
            {
                writer.WriteCompressedSignedInteger(deltaColumns);
            }
        }

        #endregion

        #region Documents

        private int GetOrAddDocument(DebugSourceDocument document, Dictionary<DebugSourceDocument, int> index)
        {
            int documentRowId;
            if (!index.TryGetValue(document, out documentRowId))
            {
                var checksumAndAlgorithm = document.ChecksumAndAlgorithm;

                documentRowId = _debugBuilderOpt.AddDocument(
                    name: SerializeDocumentName(document.Location),
                    hashAlgorithm: checksumAndAlgorithm.Item1.IsDefault ? new GuidIdx(0) : _debugBuilderOpt.GetGuidIndex(checksumAndAlgorithm.Item2),
                    hash: (checksumAndAlgorithm.Item1.IsDefault) ? new BlobIdx(0) : _debugBuilderOpt.GetBlobIndex(checksumAndAlgorithm.Item1),
                    language: _debugBuilderOpt.GetGuidIndex(document.Language));

                index.Add(document, documentRowId);
            }

            return documentRowId;
        }

        private static readonly char[] Separator1 = { '/' };
        private static readonly char[] Separator2 = { '\\' };

        private BlobIdx SerializeDocumentName(string name)
        {
            Debug.Assert(name != null);

            var writer = new BlobBuilder();

            int c1 = Count(name, Separator1[0]);
            int c2 = Count(name, Separator2[0]);
            char[] separator = (c1 >= c2) ? Separator1 : Separator2;

            writer.WriteByte((byte)separator[0]);

            // TODO: avoid allocations
            foreach (var part in name.Split(separator))
            {
                BlobIdx partIndex = _debugBuilderOpt.GetBlobIndex(ImmutableArray.Create(s_utf8Encoding.GetBytes(part)));
                writer.WriteCompressedInteger((uint)_debugBuilderOpt.ResolveBlobIndex(partIndex));
            }

            return _debugBuilderOpt.GetBlobIndex(writer);
        }

        private static int Count(string str, char c)
        {
            int count = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == c)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion

        #region Edit and Continue

        private void SerializeEncMethodDebugInformation(IMethodBody methodBody, int methodRowId)
        {
            var encInfo = GetEncMethodDebugInfo(methodBody);

            if (!encInfo.LocalSlots.IsDefaultOrEmpty)
            {
                var writer = new BlobBuilder();

                encInfo.SerializeLocalSlots(writer);

                _debugBuilderOpt.AddCustomDebugInformation(
                    parent: HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, methodRowId),
                    kind: _debugBuilderOpt.GetGuidIndex(PortableCustomDebugInfoKinds.EncLocalSlotMap),
                    value: _debugBuilderOpt.GetBlobIndex(writer));
            }

            if (!encInfo.Lambdas.IsDefaultOrEmpty)
            {
                var writer = new BlobBuilder();

                encInfo.SerializeLambdaMap(writer);

                _debugBuilderOpt.AddCustomDebugInformation(
                    parent: HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, methodRowId),
                    kind: _debugBuilderOpt.GetGuidIndex(PortableCustomDebugInfoKinds.EncLambdaAndClosureMap),
                    value: _debugBuilderOpt.GetBlobIndex(writer));
            }
        }

        #endregion
    }
}
