// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DiaSymReader;

namespace Roslyn.Test.PdbUtilities
{
    internal class DelegatingSymUnmanagedWriter : SymUnmanagedWriter
    {
        private readonly SymUnmanagedWriter _target;

        public DelegatingSymUnmanagedWriter(SymUnmanagedWriter target)
        {
            _target = target;
        }

        public override int DocumentTableCapacity
        {
            get => _target.DocumentTableCapacity;
            set => _target.DocumentTableCapacity = value;
        }

        public override void Dispose() => _target.Dispose();
        public override void CloseMethod() => _target.CloseMethod();
        public override void CloseScope(int endOffset) => _target.CloseScope(endOffset);
        public override void CloseTokensToSourceSpansMap() => _target.CloseTokensToSourceSpansMap();
        public override void DefineCustomMetadata(byte[] metadata) => _target.DefineCustomMetadata(metadata);

        public override int DefineDocument(string name, Guid language, Guid vendor, Guid type, Guid algorithmId, ReadOnlySpan<byte> checksum, ReadOnlySpan<byte> source)
            => _target.DefineDocument(name, language, vendor, type, algorithmId, checksum, source);

        public override bool DefineLocalConstant(string name, object value, int constantSignatureToken)
            => _target.DefineLocalConstant(name, value, constantSignatureToken);

        public override void DefineLocalVariable(int index, string name, int attributes, int localSignatureToken)
            => _target.DefineLocalVariable(index, name, attributes, localSignatureToken);

        public override void DefineSequencePoints(int documentIndex, int count, int[] offsets, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns)
            => _target.DefineSequencePoints(documentIndex, count, offsets, startLines, startColumns, endLines, endColumns);

        public override void GetSignature(out Guid guid, out uint stamp, out int age)
            => _target.GetSignature(out guid, out stamp, out age);

        public override IEnumerable<ArraySegment<byte>> GetUnderlyingData()
            => _target.GetUnderlyingData();

        public override void MapTokenToSourceSpan(int token, int documentIndex, int startLine, int startColumn, int endLine, int endColumn)
            => _target.MapTokenToSourceSpan(token, documentIndex, startLine, startColumn, endLine, endColumn);

        public override void OpenMethod(int methodToken)
            => _target.OpenMethod(methodToken);

        public override void OpenScope(int startOffset)
            => _target.OpenScope(startOffset);

        public override void OpenTokensToSourceSpansMap()
            => _target.OpenTokensToSourceSpansMap();

        public override void SetAsyncInfo(int moveNextMethodToken, int kickoffMethodToken, int catchHandlerOffset, ReadOnlySpan<int> yieldOffsets, ReadOnlySpan<int> resumeOffsets)
            => _target.SetAsyncInfo(moveNextMethodToken, kickoffMethodToken, catchHandlerOffset, yieldOffsets, resumeOffsets);

        public override void SetEntryPoint(int entryMethodToken)
            => _target.SetEntryPoint(entryMethodToken);

        public override void SetSourceLinkData(byte[] data)
            => _target.SetSourceLinkData(data);

        public override void SetSourceServerData(byte[] data)
            => _target.SetSourceServerData(data);

        public override void UpdateSignature(Guid guid, uint stamp, int age)
            => _target.UpdateSignature(guid, stamp, age);

        public override void UsingNamespace(string importString)
            => _target.UsingNamespace(importString);

        public override void WriteTo(Stream stream)
            => _target.WriteTo(stream);
    }
}
