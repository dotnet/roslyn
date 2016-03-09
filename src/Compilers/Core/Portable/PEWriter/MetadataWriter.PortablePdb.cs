// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace Microsoft.Cci
{
    internal partial class MetadataWriter
    {
        private struct DocumentRow
        {
            public BlobIdx Name;
            public uint HashAlgorithm; // Guid
            public BlobIdx Hash;
            public uint Language;      // Guid
        }

        private struct MethodDebugInformationRow
        {
            public uint Document;       // DocumentRid
            public BlobIdx SequencePoints;
        }

        private struct LocalScopeRow
        {
            public uint Method;         // MethodRid
            public uint ImportScope;    // ImportScopeRid
            public uint VariableList;   // LocalVariableRid
            public uint ConstantList;   // LocalConstantRid
            public uint StartOffset;
            public uint Length;
        }

        private struct LocalVariableRow
        {
            public ushort Attributes;
            public ushort Index;
            public StringIdx Name;
        }

        private struct LocalConstantRow
        {
            public StringIdx Name;
            public BlobIdx Signature;
        }

        private struct ImportScopeRow
        {
            public uint Parent;        // ImportScopeRid
            public BlobIdx Imports;
        }

        private struct StateMachineMethodRow
        {
            public uint MoveNextMethod;      // MethodRid
            public uint KickoffMethod;       // MethodRid
        }

        private struct CustomDebugInformationRow
        {
            public uint Parent;       // HasCustomDebugInformation coded index
            public uint Kind;         // Guid
            public BlobIdx Value;
        }

        private readonly List<DocumentRow> _documentTable = new List<DocumentRow>();
        private readonly List<MethodDebugInformationRow> _methodDebugInformationTable = new List<MethodDebugInformationRow>();
        private readonly List<LocalScopeRow> _localScopeTable = new List<LocalScopeRow>();
        private readonly List<LocalVariableRow> _localVariableTable = new List<LocalVariableRow>();
        private readonly List<LocalConstantRow> _localConstantTable = new List<LocalConstantRow>();
        private readonly List<ImportScopeRow> _importScopeTable = new List<ImportScopeRow>();
        private readonly List<StateMachineMethodRow> _stateMachineMethodTable = new List<StateMachineMethodRow>();
        private readonly List<CustomDebugInformationRow> _customDebugInformationTable = new List<CustomDebugInformationRow>();

        private readonly Dictionary<DebugSourceDocument, int> _documentIndex = new Dictionary<DebugSourceDocument, int>();
        private readonly Dictionary<IImportScope, int> _scopeIndex = new Dictionary<IImportScope, int>();

        private void SerializeMethodDebugInfo(IMethodBody bodyOpt, int methodRid, int localSignatureRowId)
        {
            if (bodyOpt == null)
            {
                _methodDebugInformationTable.Add(default(MethodDebugInformationRow));
                return;
            }

            bool isIterator = bodyOpt.StateMachineTypeName != null;
            bool emitDebugInfo = isIterator || bodyOpt.HasAnySequencePoints;

            if (!emitDebugInfo)
            {
                _methodDebugInformationTable.Add(default(MethodDebugInformationRow));
                return;
            }

            var bodyImportScope = bodyOpt.ImportScope;
            int importScopeRid = (bodyImportScope != null) ? GetImportScopeIndex(bodyImportScope, _scopeIndex) : 0;

            // documents & sequence points:
            int singleDocumentRowId;
            ArrayBuilder<Cci.SequencePoint> sequencePoints = ArrayBuilder<Cci.SequencePoint>.GetInstance();
            bodyOpt.GetSequencePoints(sequencePoints);
            BlobIdx sequencePointsBlob = SerializeSequencePoints(localSignatureRowId, sequencePoints.ToImmutableAndFree(), _documentIndex, out singleDocumentRowId);
            _methodDebugInformationTable.Add(new MethodDebugInformationRow { Document = (uint)singleDocumentRowId, SequencePoints = sequencePointsBlob });

            // Unlike native PDB we don't emit an empty root scope.
            // scopes are already ordered by StartOffset ascending then by EndOffset descending (the longest scope first).

            if (bodyOpt.LocalScopes.Length == 0)
            {
                // TODO: the compiler should produce a scope for each debuggable method 
                _localScopeTable.Add(new LocalScopeRow
                {
                    Method = (uint)methodRid,
                    ImportScope = (uint)importScopeRid,
                    VariableList = (uint)_localVariableTable.Count + 1,
                    ConstantList = (uint)_localConstantTable.Count + 1,
                    StartOffset = 0,
                    Length = (uint)bodyOpt.IL.Length
                });
            }
            else
            {
                foreach (LocalScope scope in bodyOpt.LocalScopes)
                {
                    _localScopeTable.Add(new LocalScopeRow
                    {
                        Method = (uint)methodRid,
                        ImportScope = (uint)importScopeRid,
                        VariableList = (uint)_localVariableTable.Count + 1,
                        ConstantList = (uint)_localConstantTable.Count + 1,
                        StartOffset = (uint)scope.StartOffset,
                        Length = (uint)scope.Length
                    });

                    foreach (ILocalDefinition local in scope.Variables)
                    {
                        Debug.Assert(local.SlotIndex >= 0);

                        _localVariableTable.Add(new LocalVariableRow
                        {
                            Attributes = (ushort)local.PdbAttributes,
                            Index = (ushort)local.SlotIndex,
                            Name = _debugHeapsOpt.GetStringIndex(local.Name)
                        });

                        SerializeDynamicLocalInfo(local, rowId: _localVariableTable.Count, isConstant: false);
                    }

                    foreach (ILocalDefinition constant in scope.Constants)
                    {
                        var mdConstant = constant.CompileTimeValue;
                        Debug.Assert(mdConstant != null);

                        _localConstantTable.Add(new LocalConstantRow
                        {
                            Name = _debugHeapsOpt.GetStringIndex(constant.Name),
                            Signature = SerializeLocalConstantSignature(constant)
                        });

                        SerializeDynamicLocalInfo(constant, rowId: _localConstantTable.Count, isConstant: true);
                    }
                }
            }

            var asyncDebugInfo = bodyOpt.AsyncDebugInfo;
            if (asyncDebugInfo != null)
            {
                _stateMachineMethodTable.Add(new StateMachineMethodRow
                {
                    MoveNextMethod = (uint)methodRid,
                    KickoffMethod = (uint)GetMethodDefIndex(asyncDebugInfo.KickoffMethod)
                });

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
                writer.WriteByte((byte)GetConstantTypeCode(value));

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

            return _debugHeapsOpt.GetBlobIndex(writer);
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
            writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(alias.Name)));
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
                writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(import.AliasOpt)));
                writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(import.TargetXmlNamespaceOpt)));
            }
            else if (import.TargetTypeOpt != null)
            {
                Debug.Assert(import.TargetNamespaceOpt == null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                if (import.AliasOpt != null)
                {
                    // <import> ::= AliasType <alias> <target-type>
                    writer.WriteByte((byte)ImportDefinitionKind.AliasType);
                    writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(import.AliasOpt)));
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
                        writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(import.AliasOpt)));
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
                        writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(import.AliasOpt)));
                    }
                    else
                    {
                        // <import> ::= ImportNamespace <target-namespace>
                        writer.WriteByte((byte)ImportDefinitionKind.ImportNamespace);
                    }
                }

                // TODO: cache?
                string namespaceName = TypeNameSerializer.BuildQualifiedNamespaceName(import.TargetNamespaceOpt);
                writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(namespaceName)));
            }
            else
            {
                // <import> ::= ImportReferenceAlias <alias>
                Debug.Assert(import.AliasOpt != null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                writer.WriteByte((byte)ImportDefinitionKind.ImportAssemblyReferenceAlias);
                writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(_debugHeapsOpt.GetBlobIndexUtf8(import.AliasOpt)));
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

            _importScopeTable.Add(new ImportScopeRow
            {
                Parent = 0,
                Imports = _debugHeapsOpt.GetBlobIndex(writer)
            });

            Debug.Assert(_importScopeTable.Count == ModuleImportScopeRid);
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

            _importScopeTable.Add(new ImportScopeRow
            {
                Parent = (uint)parentScopeRid,
                Imports = SerializeImportsBlob(scope)
            });

            var rid = _importScopeTable.Count;
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

            return _debugHeapsOpt.GetBlobIndex(writer);
        }

        private void SerializeModuleDefaultNamespace()
        {
            // C#: DefaultNamespace is null.
            // VB: DefaultNamespace is non-null.
            if (module.DefaultNamespace == null)
            {
                return;
            }

            _customDebugInformationTable.Add(new CustomDebugInformationRow
            {
                Parent = HasCustomDebugInformation(HasCustomDebugInformationTag.Module, 1),
                Kind = (uint)_debugHeapsOpt.GetGuidIndex(PortableCustomDebugInfoKinds.DefaultNamespace),
                Value = _debugHeapsOpt.GetBlobIndexUtf8(module.DefaultNamespace)
            });
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

            _customDebugInformationTable.Add(new CustomDebugInformationRow
            {
                Parent = HasCustomDebugInformation(tag, rowId),
                Kind = (uint)_debugHeapsOpt.GetGuidIndex(PortableCustomDebugInfoKinds.DynamicLocalVariables),
                Value = _debugHeapsOpt.GetBlobIndex(value),
            });
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

            _customDebugInformationTable.Add(new CustomDebugInformationRow
            {
                Parent = HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, moveNextMethodRid),
                Kind = (uint)_debugHeapsOpt.GetGuidIndex(PortableCustomDebugInfoKinds.AsyncMethodSteppingInformationBlob),
                Value = _debugHeapsOpt.GetBlobIndex(writer),
            });
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

            _customDebugInformationTable.Add(new CustomDebugInformationRow
            {
                Parent = HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, methodRowId),
                Kind = (uint)_debugHeapsOpt.GetGuidIndex(PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes),
                Value = _debugHeapsOpt.GetBlobIndex(writer),
            });
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

            return _debugHeapsOpt.GetBlobIndex(writer);
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
                documentRowId = _documentTable.Count + 1;
                index.Add(document, documentRowId);

                var checksumAndAlgorithm = document.ChecksumAndAlgorithm;
                _documentTable.Add(new DocumentRow
                {
                    Name = SerializeDocumentName(document.Location),
                    HashAlgorithm = (uint)(checksumAndAlgorithm.Item1.IsDefault ? 0 : _debugHeapsOpt.GetGuidIndex(checksumAndAlgorithm.Item2)),
                    Hash = (checksumAndAlgorithm.Item1.IsDefault) ? new BlobIdx(0) : _debugHeapsOpt.GetBlobIndex(checksumAndAlgorithm.Item1),
                    Language = (uint)_debugHeapsOpt.GetGuidIndex(document.Language),
                });
            }

            return documentRowId;
        }

        private static readonly char[] s_separator1 = { '/' };
        private static readonly char[] s_separator2 = { '\\' };

        private BlobIdx SerializeDocumentName(string name)
        {
            Debug.Assert(name != null);

            var writer = new BlobBuilder();

            int c1 = Count(name, s_separator1[0]);
            int c2 = Count(name, s_separator2[0]);
            char[] separator = (c1 >= c2) ? s_separator1 : s_separator2;

            writer.WriteByte((byte)separator[0]);

            // TODO: avoid allocations
            foreach (var part in name.Split(separator))
            {
                BlobIdx partIndex = _debugHeapsOpt.GetBlobIndex(ImmutableArray.Create(s_utf8Encoding.GetBytes(part)));
                writer.WriteCompressedInteger((uint)_debugHeapsOpt.ResolveBlobIndex(partIndex));
            }

            return _debugHeapsOpt.GetBlobIndex(writer);
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

                _customDebugInformationTable.Add(new CustomDebugInformationRow
                {
                    Parent = HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, methodRowId),
                    Kind = (uint)_debugHeapsOpt.GetGuidIndex(PortableCustomDebugInfoKinds.EncLocalSlotMap),
                    Value = _debugHeapsOpt.GetBlobIndex(writer),
                });
            }

            if (!encInfo.Lambdas.IsDefaultOrEmpty)
            {
                var writer = new BlobBuilder();

                encInfo.SerializeLambdaMap(writer);

                _customDebugInformationTable.Add(new CustomDebugInformationRow
                {
                    Parent = HasCustomDebugInformation(HasCustomDebugInformationTag.MethodDef, methodRowId),
                    Kind = (uint)_debugHeapsOpt.GetGuidIndex(PortableCustomDebugInfoKinds.EncLambdaAndClosureMap),
                    Value = _debugHeapsOpt.GetBlobIndex(writer),
                });
            }
        }

        #endregion

        #region Table Serialization

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
            foreach (var row in _stateMachineMethodTable)
            {
                writer.WriteReference(row.MoveNextMethod, metadataSizes.MethodDefIndexSize);
                writer.WriteReference(row.KickoffMethod, metadataSizes.MethodDefIndexSize);
            }
        }

        private void SerializeCustomDebugInformationTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // sort by Parent, Kind
            _customDebugInformationTable.Sort(CustomDebugInformationRowComparer.Instance);

            foreach (var row in _customDebugInformationTable)
            {
                writer.WriteReference(row.Parent, metadataSizes.HasCustomDebugInformationSize);
                writer.WriteReference(row.Kind, metadataSizes.GuidIndexSize);
                writer.WriteReference((uint)_debugHeapsOpt.ResolveBlobIndex(row.Value), metadataSizes.BlobIndexSize);
            }
        }

        private class CustomDebugInformationRowComparer : Comparer<CustomDebugInformationRow>
        {
            public static readonly CustomDebugInformationRowComparer Instance = new CustomDebugInformationRowComparer();

            public override int Compare(CustomDebugInformationRow x, CustomDebugInformationRow y)
            {
                int result = (int)x.Parent - (int)y.Parent;
                return (result != 0) ? result : (int)x.Kind - (int)y.Kind;
            }
        }

        #endregion
    }
}
