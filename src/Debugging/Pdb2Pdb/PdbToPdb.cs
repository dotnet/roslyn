// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata.Ecma335.Blobs;
using System.Reflection.PortableExecutable;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System;
using Roslyn.Test.PdbUtilities;

namespace Microsoft.DiaSymReader.Tools
{
    public sealed class PdbToPdb
    {
        public static void Convert(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream)
        {
            var metadata = new MetadataBuilder();
            ImmutableArray<int> externalRowCounts;
            int debugEntryPointToken = 0;
            Guid pdbGuid = Guid.Empty;
            uint pdbStamp = 0;

            using (var peReader = new PEReader(peStream))
            {
                foreach (var entry in peReader.ReadDebugDirectory())
                {
                    if (entry.Type == DebugDirectoryEntryType.CodeView)
                    {
                        pdbGuid = peReader.ReadCodeViewDebugDirectoryData(entry).Guid;
                        pdbStamp = entry.Stamp;
                        break;
                    }
                }

                var metadataReader = peReader.GetMetadataReader();
                var symReader = SymReaderFactory.CreateReader(sourcePdbStream, metadataReader);

                externalRowCounts = GetRowCounts(metadataReader);
                debugEntryPointToken = symReader.GetUserEntryPoint();

                // documents:
                var documentIndex = new Dictionary<string, DocumentHandle>();
                var documents = symReader.GetDocuments();
                metadata.SetCapacity(TableIndex.Document, documents.Length);

                foreach (var document in documents)
                {
                    string name = document.GetName();

                    var handle = metadata.AddDocument(
                        name: metadata.AddDocumentName(name),
                        hashAlgorithm: metadata.GetGuid(document.GetHashAlgorithm()),
                        hash: metadata.GetBlob(document.GetChecksum()),
                        language: metadata.GetGuid(document.GetLanguage()));

                    documentIndex.Add(name, handle);
                }

                var lastLocalVariableHandle = default(LocalVariableHandle);
                var lastLocalConstantHandle = default(LocalConstantHandle);
                
                // methods:
                metadata.SetCapacity(TableIndex.MethodDebugInformation, metadataReader.MethodDefinitions.Count);
                foreach (var methodHandle in metadataReader.MethodDefinitions)
                {
                    var methodDef = metadataReader.GetMethodDefinition(methodHandle);

                    StandaloneSignatureHandle localSignatureHandle;
                    if (methodDef.RelativeVirtualAddress != 0)
                    {
                        var methodBody = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                        localSignatureHandle = methodBody.LocalSignature.IsNil ? default(StandaloneSignatureHandle) : methodBody.LocalSignature;
                    }
                    else
                    {
                        localSignatureHandle = default(StandaloneSignatureHandle);
                    }

                    var symMethod = symReader.GetMethod(MetadataTokens.GetToken(methodHandle));
                    if (symMethod == null)
                    {
                        metadata.AddMethodDebugInformation(document: default(DocumentHandle), sequencePoints: default(BlobHandle));
                        continue;
                    }

                    var symSequencePoints = symMethod.GetSequencePoints();

                    DocumentHandle singleDocumentHandle;
                    BlobHandle sequencePointsBlob = SerializeSequencePoints(metadata, localSignatureHandle, symSequencePoints, documentIndex, out singleDocumentHandle);

                    metadata.AddMethodDebugInformation(
                        document: singleDocumentHandle,
                        sequencePoints: sequencePointsBlob);

                    // async info:
                    var symAsyncMethod = symMethod.AsAsync();
                    if (symAsyncMethod != null)
                    {
                        // TODO
                    }

                    // enc debug info:
                    // TODO


                    // TODO: import scopes
                    var importScopeHandle = default(ImportScopeHandle);

                    // scopes:
                    var rootScope = symMethod.GetRootScope();

                    // TODO: error
                    Debug.Assert(rootScope.GetNamespaces().IsEmpty && rootScope.GetLocals().IsEmpty && rootScope.GetConstants().IsEmpty);

                    foreach (ISymUnmanagedScope scope in rootScope.GetScopes())
                    {
                        SerializeScope(metadata, methodHandle, importScopeHandle, scope, ref lastLocalVariableHandle, ref lastLocalConstantHandle);
                    }
                }
            }

            var pdbContentId = new ContentId(pdbGuid.ToByteArray(), BitConverter.GetBytes(pdbStamp));
            var serializer = new StandaloneDebugMetadataSerializer(metadata, externalRowCounts, debugEntryPointToken, isMinimalDelta: false);
            
            ContentId metadataContentId;
            var blobBuilder = new BlobBuilder();
            serializer.SerializeMetadata(blobBuilder, builder => pdbContentId, out metadataContentId);
            blobBuilder.WriteContentTo(targetPdbStream);
        }

        private static ImmutableArray<int> GetRowCounts(MetadataReader reader)
        {
            var builder = ImmutableArray.CreateBuilder<int>(MetadataTokens.TableCount);
            for (int i = 0; i < MetadataTokens.TableCount; i++)
            {
                builder.Add(reader.GetTableRowCount((TableIndex)i));
            }

            return builder.MoveToImmutable();
        }

        private static void SerializeScope(
            MetadataBuilder metadata,
            MethodDefinitionHandle methodHandle,
            ImportScopeHandle importScope,
            ISymUnmanagedScope scope,
            ref LocalVariableHandle lastLocalVariable, 
            ref LocalConstantHandle lastLocalConstant)
        {
            // TODO: VB inclusive range end
            int start = scope.GetStartOffset();
            int end = scope.GetEndOffset();

            metadata.AddLocalScope(
                method: methodHandle,
                importScope: importScope,
                variableList: NextHandle(lastLocalVariable),
                constantList: NextHandle(lastLocalConstant),
                startOffset: start,
                length: end - start);

            foreach (var symLocal in scope.GetLocals())
            {
                lastLocalVariable = metadata.AddLocalVariable(
                    attributes: (LocalVariableAttributes)symLocal.GetAttributes(),
                    index: symLocal.GetSlot(),
                    name: metadata.GetString(symLocal.GetName()));

                // TODO: dynamic info
            }

            // TODO
            //foreach (var symConstant in scope.GetConstants())
            //{
            //    lastLocalConstantRid = tables.AddLocalConstant(
            //        name: metadata.GetStringIndex(symConstant.GetName()),
            //        signature: new BlobIdx(0)); // TODO

            //    // TODO: dynamic info
            //}

            int previousChildScopeEnd = start;
            foreach (ISymUnmanagedScope child in scope.GetScopes())
            {
                int childScopeStart = child.GetStartOffset();
                int childScopeEnd = child.GetEndOffset();

                // scopes are properly nested:
                if (childScopeStart < previousChildScopeEnd || childScopeEnd > end)
                {
                    throw new BadImageFormatException($"Invalid scope IL offset range: [{childScopeStart}, {childScopeEnd}), method 0x{MetadataTokens.GetToken(methodHandle):x}.");
                }

                previousChildScopeEnd = childScopeEnd;

                SerializeScope(metadata, methodHandle, importScope, child, ref lastLocalVariable, ref lastLocalConstant);
            }
        }

        private static LocalVariableHandle NextHandle(LocalVariableHandle handle) =>
            MetadataTokens.LocalVariableHandle(MetadataTokens.GetRowNumber(handle) + 1);

        private static LocalConstantHandle NextHandle(LocalConstantHandle handle) =>
            MetadataTokens.LocalConstantHandle(MetadataTokens.GetRowNumber(handle) + 1);

        private static BlobHandle SerializeSequencePoints(
            MetadataBuilder metadata,
            StandaloneSignatureHandle localSignatureHandle,
            ImmutableArray<SymUnmanagedSequencePoint> sequencePoints,
            Dictionary<string, DocumentHandle> documentIndex,
            out DocumentHandle singleDocumentHandle)
        {
            if (sequencePoints.Length == 0)
            {
                singleDocumentHandle = default(DocumentHandle);
                return default(BlobHandle);
            }

            var builder = new BlobBuilder();

            int previousNonHiddenStartLine = -1;
            int previousNonHiddenStartColumn = -1;

            // header:
            builder.WriteCompressedInteger(MetadataTokens.GetRowNumber(localSignatureHandle));

            DocumentHandle previousDocument = TryGetSingleDocument(sequencePoints, documentIndex);
            singleDocumentHandle = previousDocument;

            for (int i = 0; i < sequencePoints.Length; i++)
            {
                var currentDocument = documentIndex[sequencePoints[i].Document.GetName()];
                if (previousDocument != currentDocument)
                {
                    // optional document in header or document record:
                    if (!previousDocument.IsNil)
                    {
                        builder.WriteCompressedInteger(0);
                    }

                    builder.WriteCompressedInteger(MetadataTokens.GetRowNumber(currentDocument));
                    previousDocument = currentDocument;
                }

                // delta IL offset:
                if (i > 0)
                {
                    builder.WriteCompressedInteger(sequencePoints[i].Offset - sequencePoints[i - 1].Offset);
                }
                else
                {
                    builder.WriteCompressedInteger(sequencePoints[i].Offset);
                }

                if (sequencePoints[i].IsHidden)
                {
                    builder.WriteInt16(0);
                    continue;
                }

                // Delta Lines & Columns:
                SerializeDeltaLinesAndColumns(builder, sequencePoints[i]);

                // delta Start Lines & Columns:
                if (previousNonHiddenStartLine < 0)
                {
                    Debug.Assert(previousNonHiddenStartColumn < 0);
                    builder.WriteCompressedInteger(sequencePoints[i].StartLine);
                    builder.WriteCompressedInteger(sequencePoints[i].StartColumn);
                }
                else
                {
                    builder.WriteCompressedSignedInteger(sequencePoints[i].StartLine - previousNonHiddenStartLine);
                    builder.WriteCompressedSignedInteger(sequencePoints[i].StartColumn - previousNonHiddenStartColumn);
                }

                previousNonHiddenStartLine = sequencePoints[i].StartLine;
                previousNonHiddenStartColumn = sequencePoints[i].StartColumn;
            }

            return metadata.GetBlob(builder);
        }

        private static DocumentHandle TryGetSingleDocument(ImmutableArray<SymUnmanagedSequencePoint> sequencePoints, Dictionary<string, DocumentHandle> documentIndex)
        {
            DocumentHandle singleDocument = documentIndex[sequencePoints[0].Document.GetName()];
            for (int i = 1; i < sequencePoints.Length; i++)
            {
                if (documentIndex[sequencePoints[i].Document.GetName()] != singleDocument)
                {
                    return default(DocumentHandle);
                }
            }

            return singleDocument;
        }

        private static void SerializeDeltaLinesAndColumns(BlobBuilder writer, SymUnmanagedSequencePoint sequencePoint)
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
    }
}
