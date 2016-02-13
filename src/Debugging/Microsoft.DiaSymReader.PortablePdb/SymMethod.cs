// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymMethod : ISymUnmanagedMethod, ISymUnmanagedAsyncMethod, ISymEncUnmanagedMethod
    {
        internal sealed class ByHandleComparer : IComparer<ISymUnmanagedMethod>
        {
            public static readonly ByHandleComparer Default = new ByHandleComparer();
            public int Compare(ISymUnmanagedMethod x, ISymUnmanagedMethod y) => HandleComparer.Default.Compare(((SymMethod)x).DebugHandle, ((SymMethod)y).DebugHandle);
        }

        internal MethodDebugInformationHandle DebugHandle { get; }
        internal MethodDefinitionHandle DefinitionHandle => DebugHandle.ToDefinitionHandle();
        internal SymReader SymReader { get; }
        private RootScopeData _lazyRootScopeData;
        private AsyncMethodData _lazyAsyncMethodData;

        internal MetadataReader MetadataReader => SymReader.MetadataReader;

        internal SymMethod(SymReader symReader, MethodDebugInformationHandle handle)
        {
            Debug.Assert(symReader != null);
            SymReader = symReader;
            DebugHandle = handle;
        }

        private SequencePointCollection.Enumerator GetSequencePointEnumerator()
        {
            return SymReader.MetadataReader.GetMethodDebugInformation(DebugHandle).GetSequencePoints().GetEnumerator();
        }

        private RootScopeData GetRootScopeData()
        {
            if (_lazyRootScopeData == null)
            {
                _lazyRootScopeData = new RootScopeData(this);
            }

            return _lazyRootScopeData;
        }

        private int GetILSize()
        {
            // SymWriter sets the size of the method to the end offset of the root scope in CloseMethod:
            return GetRootScopeData().EndOffset;
        }

        #region ISymUnmanagedMethod

        public int GetNamespace([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedNamespace @namespace)
        {
            // SymReader doesn't support namespaces
            @namespace = null;
            return HResult.E_NOTIMPL;
        }

        public int GetOffset(ISymUnmanagedDocument document, int line, int column, out int offset)
        {
            if (line <= 0)
            {
                offset = 0;
                return HResult.E_INVALIDARG;
            }

            // Note that DiaSymReader completely ignores column parameter.

            var symDocument = SymReader.AsSymDocument(document);
            if (symDocument == null)
            {
                offset = 0;
                return HResult.E_INVALIDARG;
            }

            // DiaSymReader uses DiaSession::findLinesByLinenum, which results in bad results for lines shared across multiple methods
            // and for lines outside of the current method.

            var spReader = GetSequencePointEnumerator();
            var documentHandle = symDocument.Handle;

            while (spReader.MoveNext())
            {
                if (!spReader.Current.IsHidden &&
                    spReader.Current.Document == documentHandle &&
                    line >= spReader.Current.StartLine &&
                    line <= spReader.Current.EndLine)
                {
                    // Return the first matching IL offset. In common cases there will be a single one 
                    // since sequence points of a single method don't overlap unless forced by #line.
                    offset = spReader.Current.Offset;
                    return HResult.S_OK;
                }
            }

            offset = 0;
            return HResult.E_FAIL;
        }

        public int GetParameters(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedVariable[] parameters)
        {
            // SymReader doesn't support parameter access. 
            count = 0;
            return HResult.E_NOTIMPL;
        }

        public int GetRanges(
            ISymUnmanagedDocument document,
            int line,
            int column,
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out]int[] ranges)
        {
            if (line <= 0)
            {
                count = 0;
                return HResult.E_INVALIDARG;
            }

            // Note that DiaSymReader completely ignores column parameter.

            var symDocument = SymReader.AsSymDocument(document);
            if (symDocument == null)
            {
                count = 0;
                return HResult.E_INVALIDARG;
            }

            // DiaSymReader uses DiaSession::findLinesByLinenum, which results in bad results for lines shared across multiple methods.

            var spReader = GetSequencePointEnumerator();
            var documentHandle = symDocument.Handle;

            bool setEndOffset = false;
            int i = 0;
            while (spReader.MoveNext())
            {
                if (setEndOffset)
                {
                    ranges[i - 1] = spReader.Current.Offset;
                    setEndOffset = false;
                }

                if (!spReader.Current.IsHidden &&
                    spReader.Current.Document == documentHandle &&
                    line >= spReader.Current.StartLine &&
                    line <= spReader.Current.EndLine)
                {
                    if (i + 1 < bufferLength)
                    {
                        ranges[i] = spReader.Current.Offset;
                        setEndOffset = true;
                    }

                    // pair of offsets for each sequence point
                    i += 2;
                }
            }

            if (setEndOffset)
            {
                ranges[i - 1] = GetILSize();
            }

            count = i;
            return HResult.S_OK;
        }

        public int GetRootScope([MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            // SymReader always creates a new scope instance
            scope = new SymScope(GetRootScopeData());
            return HResult.S_OK;
        }

        public int GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedScope scope)
        {
            // SymReader doesn't support. 
            scope = null;
            return HResult.S_OK;
        }

        public int GetSequencePointCount(out int count)
        {
            var spReader = GetSequencePointEnumerator();

            int i = 0;
            while (spReader.MoveNext())
            {
                i++;
            }

            count = i;
            return HResult.S_OK;
        }

        public int GetSequencePoints(
            int bufferLength,
            out int count,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] offsets,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]ISymUnmanagedDocument[] documents,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] startLines,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] startColumns,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] endLines,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]int[] endColumns)
        {
            SymDocument currentDocument = null;
            var spReader = GetSequencePointEnumerator();

            int i = 0;
            while (spReader.MoveNext())
            {
                if (bufferLength != 0 && i >= bufferLength)
                {
                    break;
                }

                var sp = spReader.Current;

                if (offsets != null)
                {
                    offsets[i] = sp.Offset;
                }

                if (startLines != null)
                {
                    startLines[i] = sp.StartLine;
                }

                if (startColumns != null)
                {
                    startColumns[i] = sp.StartColumn;
                }

                if (endLines != null)
                {
                    endLines[i] = sp.EndLine;
                }

                if (endColumns != null)
                {
                    endColumns[i] = sp.EndColumn;
                }

                if (documents != null)
                {
                    if (currentDocument == null || currentDocument.Handle != sp.Document)
                    {
                        currentDocument = new SymDocument(SymReader, sp.Document);
                    }

                    documents[i] = currentDocument;
                }

                i++;
            }

            count = i;
            return HResult.S_OK;
        }

        public int GetSourceStartEnd(
            ISymUnmanagedDocument[] documents,
            [In, MarshalAs(UnmanagedType.LPArray), Out]int[] lines,
            [In, MarshalAs(UnmanagedType.LPArray), Out]int[] columns,
            out bool defined)
        {
            // This symbol reader doesn't support source start/end for methods.
            defined = false;
            return HResult.E_NOTIMPL;
        }

        public int GetToken(out int methodToken)
        {
            methodToken = MetadataTokens.GetToken(DefinitionHandle);
            return HResult.S_OK;
        }

        #endregion

        #region ISymUnmanagedAsyncMethod

        private AsyncMethodData AsyncMethodData
        {
            get
            {
                if (_lazyAsyncMethodData == null)
                {
                    _lazyAsyncMethodData = ReadAsyncMethodData();
                }

                return _lazyAsyncMethodData;
            }
        }

        private AsyncMethodData ReadAsyncMethodData()
        {
            var reader = MetadataReader;
            var body = reader.GetMethodDebugInformation(DebugHandle);
            var kickoffMethod = body.GetStateMachineKickoffMethod();

            if (kickoffMethod.IsNil)
            {
                return AsyncMethodData.None;
            }

            var value = reader.GetCustomDebugInformation(DefinitionHandle, MetadataUtilities.MethodSteppingInformationBlobId);
            if (value.IsNil)
            {
                return AsyncMethodData.None;
            }

            var blobReader = reader.GetBlobReader(value);

            long catchHandlerOffset = blobReader.ReadUInt32();
            if (catchHandlerOffset > (uint)int.MaxValue + 1)
            {
                throw new BadImageFormatException();
            }

            var yieldOffsets = ImmutableArray.CreateBuilder<int>();
            var resultOffsets = ImmutableArray.CreateBuilder<int>();
            var resumeMethods = ImmutableArray.CreateBuilder<int>();

            while (blobReader.RemainingBytes > 0)
            {
                uint yieldOffset = blobReader.ReadUInt32();
                if (yieldOffset > int.MaxValue)
                {
                    throw new BadImageFormatException();
                }

                uint resultOffset = blobReader.ReadUInt32();
                if (resultOffset > int.MaxValue)
                {
                    throw new BadImageFormatException();
                }

                yieldOffsets.Add((int)yieldOffset);
                resultOffsets.Add((int)resultOffset);
                resumeMethods.Add(MetadataUtilities.MethodDefToken(blobReader.ReadCompressedInteger()));
            }

            return new AsyncMethodData(
                kickoffMethod,
                (int)(catchHandlerOffset - 1),
                yieldOffsets.ToImmutable(),
                resultOffsets.ToImmutable(),
                resumeMethods.ToImmutable());
        }

        public int IsAsyncMethod(out bool value)
        {
            value = !AsyncMethodData.IsNone;
            return HResult.S_OK;
        }

        public int GetKickoffMethod(out int kickoffMethodToken)
        {
            if (AsyncMethodData.IsNone)
            {
                kickoffMethodToken = 0;
                return HResult.E_UNEXPECTED;
            }

            kickoffMethodToken = MetadataTokens.GetToken(AsyncMethodData.KickoffMethod);
            return HResult.S_OK;
        }

        public int HasCatchHandlerILOffset(out bool value)
        {
            if (AsyncMethodData.IsNone)
            {
                value = false;
                return HResult.E_UNEXPECTED;
            }

            value = AsyncMethodData.CatchHandlerOffset >= 0;
            return HResult.S_OK;
        }

        public int GetCatchHandlerILOffset(out int offset)
        {
            if (AsyncMethodData.IsNone || AsyncMethodData.CatchHandlerOffset < 0)
            {
                offset = 0;
                return HResult.E_UNEXPECTED;
            }

            offset = AsyncMethodData.CatchHandlerOffset;
            return HResult.S_OK;
        }

        public int GetAsyncStepInfoCount(out int count)
        {
            if (AsyncMethodData.IsNone)
            {
                count = 0;
                return HResult.E_UNEXPECTED;
            }

            count = AsyncMethodData.YieldOffsets.Length;
            return HResult.S_OK;
        }

        public int GetAsyncStepInfo(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]int[] yieldOffsets,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]int[] breakpointOffsets,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]int[] breakpointMethods)
        {
            if (AsyncMethodData.IsNone)
            {
                count = 0;
                return HResult.E_UNEXPECTED;
            }

            int length = Math.Min(bufferLength, AsyncMethodData.YieldOffsets.Length);

            if (yieldOffsets != null)
            {
                AsyncMethodData.YieldOffsets.CopyTo(0, yieldOffsets, 0, length);
            }

            if (breakpointOffsets != null)
            {
                AsyncMethodData.ResumeOffsets.CopyTo(0, breakpointOffsets, 0, length);
            }

            if (breakpointMethods != null)
            {
                AsyncMethodData.ResumeMethods.CopyTo(0, breakpointMethods, 0, length);
            }

            count = length;
            return HResult.S_OK;
        }

        #endregion

        #region ISymEncUnmanagedMethod

        /// <summary>
        /// Get the file name for the line associated with specified offset.
        /// </summary>
        public int GetFileNameFromOffset(
            int offset,
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] name)
        {
            // TODO: parse sequence points -> document
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the Line information associated with <paramref name="offset"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="offset"/> is not a sequence point it is associated with the previous one.
        /// <paramref name="sequencePointOffset"/> provides the associated sequence point.
        /// </remarks>
        public int GetLineFromOffset(
            int offset,
            out int startLine,
            out int startColumn,
            out int endLine,
            out int endColumn,
            out int sequencePointOffset)
        {
            // TODO: parse sequence points
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the number of Documents that this method has lines in.
        /// </summary>
        public int GetDocumentsForMethodCount(out int count)
        {
            // TODO: parse sequence points
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the documents this method has lines in.
        /// </summary>
        public int GetDocumentsForMethod(
            int bufferLength,
            out int count,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]ISymUnmanagedDocument[] documents)
        {
            // TODO: parse sequence points
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the smallest start line and largest end line, for the method, in a specific document.
        /// </summary>
        public int GetSourceExtentInDocument(ISymUnmanagedDocument document, out int startLine, out int endLine)
        {
            return SymReader.GetMethodSourceExtentInDocument(document, this, out startLine, out endLine);
        }

        #endregion
    }
}