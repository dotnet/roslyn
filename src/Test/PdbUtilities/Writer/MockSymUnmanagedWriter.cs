﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DiaSymReader;

namespace Roslyn.Test.Utilities
{
    internal class MockSymUnmanagedWriter : SymUnmanagedWriter
    {
        private Exception MakeException() => new SymUnmanagedWriterException("MockSymUnmanagedWriter error message");

        public override int DocumentTableCapacity { get => throw MakeException(); set => throw MakeException(); }

        public override void Dispose()
        {
            // Dipose shall not throw
        }

        public override void CloseMethod()
        {
            throw MakeException();
        }

        public override void CloseScope(int endOffset)
        {
            throw MakeException();
        }

        public override void CloseTokensToSourceSpansMap()
        {
            throw MakeException();
        }

        public override void DefineCustomMetadata(byte[] metadata)
        {
            throw MakeException();
        }

        public override int DefineDocument(string name, Guid language, Guid vendor, Guid type, Guid algorithmId, byte[] checksum, byte[] source)
        {
            throw MakeException();
        }

        public override bool DefineLocalConstant(string name, object value, int constantSignatureToken)
        {
            throw MakeException();
        }

        public override void DefineLocalVariable(int index, string name, int attributes, int localSignatureToken)
        {
            throw MakeException();
        }

        public override void DefineSequencePoints(int documentIndex, int count, int[] offsets, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns)
        {
            throw MakeException();
        }

        public override void GetSignature(out Guid guid, out uint stamp, out int age)
        {
            throw MakeException();
        }

        public override IEnumerable<ArraySegment<byte>> GetUnderlyingData()
        {
            throw MakeException();
        }

        public override void MapTokenToSourceSpan(int token, int documentIndex, int startLine, int startColumn, int endLine, int endColumn)
        {
            throw MakeException();
        }

        public override void OpenMethod(int methodToken)
        {
            throw MakeException();
        }

        public override void OpenScope(int startOffset)
        {
            throw MakeException();
        }

        public override void OpenTokensToSourceSpansMap()
        {
            throw MakeException();
        }

        public override void SetAsyncInfo(int moveNextMethodToken, int kickoffMethodToken, int catchHandlerOffset, int[] yieldOffsets, int[] resumeOffsets)
        {
            throw MakeException();
        }

        public override void SetEntryPoint(int entryMethodToken)
        {
            throw MakeException();
        }

        public override void SetSourceLinkData(byte[] data)
        {
            throw MakeException();
        }

        public override void SetSourceServerData(byte[] data)
        {
            throw MakeException();
        }

        public override void UpdateSignature(Guid guid, uint stamp, int age)
        {
            throw MakeException();
        }

        public override void UsingNamespace(string importString)
        {
            throw MakeException();
        }

        public override void WriteTo(Stream stream)
        {
            throw MakeException();
        }
    }
}
