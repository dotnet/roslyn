// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.DiaSymReader
{
    internal sealed class SymUnmanagedWriterImpl : SymUnmanagedWriter
    {
        private static readonly object s_zeroInt32 = 0;

        private ISymUnmanagedWriter5 _symWriter;
        private readonly ComMemoryStream _pdbStream;
        private readonly List<ISymUnmanagedDocumentWriter> _documentWriters;
        private readonly string _symWriterModuleName;
        private bool _disposed;

        internal SymUnmanagedWriterImpl(ComMemoryStream pdbStream, ISymUnmanagedWriter5 symWriter, string symWriterModuleName)
        {
            Debug.Assert(pdbStream != null);
            Debug.Assert(symWriter != null);
            Debug.Assert(symWriterModuleName != null);

            _pdbStream = pdbStream;
            _symWriter = symWriter;
            _documentWriters = new List<ISymUnmanagedDocumentWriter>();
            _symWriterModuleName = symWriterModuleName;
        }

        private ISymUnmanagedWriter5 GetSymWriter()
            => _symWriter ?? throw (_disposed ? new ObjectDisposedException(nameof(SymUnmanagedWriterImpl)) : new InvalidOperationException());

        private ISymUnmanagedWriter8 GetSymWriter8()
            => GetSymWriter() is ISymUnmanagedWriter8 symWriter8 ? symWriter8 : throw PdbWritingException(new NotSupportedException());

        private Exception PdbWritingException(Exception inner)
            => new SymUnmanagedWriterException(inner, _symWriterModuleName);

        /// <summary>
        /// Writes the content to the given stream. The writer is disposed and can't be used for further writing.
        /// </summary>
        public override void WriteTo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // SymWriter flushes data to the native stream on close.
            // Closing the writer also ensures no further modifications.
            CloseSymWriter();

            try
            {
                _pdbStream.CopyTo(stream);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex); // TODO
            }
        }

        public override void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~SymUnmanagedWriterImpl()
        {
            DisposeImpl();
        }

        private void DisposeImpl()
        {
            try
            {
                CloseSymWriter();
            }
            catch
            {
                // Dispose shall not throw
            }

            _disposed = true;
        }

        private void CloseSymWriter()
        {
            var symWriter = Interlocked.Exchange(ref _symWriter, null);
            if (symWriter == null)
            {
                return;
            }

            try
            {
                symWriter.Close();
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
            finally
            {
                // We leave releasing SymWriter and document writer COM objects the to GC -- 
                // we write to an in-memory stream hence no files are being locked.
                // We need to keep these alive until the symWriter is closed because the
                // symWriter seems to have a un-ref-counted reference to them.  
                _documentWriters.Clear();
            }
        }

        public override IEnumerable<ArraySegment<byte>> GetUnderlyingData()
        {
            // Commit, so that all data are flushed to the underlying stream.
            GetSymWriter().Commit();

            return _pdbStream.GetChunks();
        }

        public override int DocumentTableCapacity
        {
            get => _documentWriters.Capacity;

            set
            {
                if (value > _documentWriters.Count)
                {
                    _documentWriters.Capacity = value;
                }
            }
        }

        public override int DefineDocument(string name, Guid language, Guid vendor, Guid type, Guid algorithmId, ReadOnlySpan<byte> checksum, ReadOnlySpan<byte> source)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var symWriter = GetSymWriter();

            int index = _documentWriters.Count;
            ISymUnmanagedDocumentWriter documentWriter;

            try
            {
                documentWriter = symWriter.DefineDocument(name, ref language, ref vendor, ref type);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }

            _documentWriters.Add(documentWriter);

            if (algorithmId != default(Guid) && checksum.Length > 0)
            {
                try
                {
                    unsafe
                    {
                        fixed (byte* bytes = checksum)
                        {
                            documentWriter.SetCheckSum(algorithmId, (uint)checksum.Length, bytes);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw PdbWritingException(ex);
                }
            }

            if (source != null)
            {
                try
                {
                    unsafe
                    {
                        fixed (byte* bytes = source)
                        {
                            documentWriter.SetSource((uint)source.Length, bytes);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw PdbWritingException(ex);
                }
            }

            return index;
        }

        public override void DefineSequencePoints(int documentIndex, int count, int[] offsets, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns)
        {
            if (documentIndex < 0 || documentIndex >= _documentWriters.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(documentIndex));
            }

            if (offsets == null) throw new ArgumentNullException(nameof(offsets));
            if (startLines == null) throw new ArgumentNullException(nameof(startLines));
            if (startColumns == null) throw new ArgumentNullException(nameof(startColumns));
            if (endLines == null) throw new ArgumentNullException(nameof(endLines));
            if (endColumns == null) throw new ArgumentNullException(nameof(endColumns));

            if (count < 0 || count > startLines.Length || count > startColumns.Length || count > endLines.Length || count > endColumns.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var symWriter = GetSymWriter();

            try
            {
                symWriter.DefineSequencePoints(
                    _documentWriters[documentIndex],
                    count,
                    offsets,
                    startLines,
                    startColumns,
                    endLines,
                    endColumns);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void OpenMethod(int methodToken)
        {
            var symWriter = GetSymWriter();

            try
            {
                symWriter.OpenMethod(unchecked((uint)methodToken));
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void CloseMethod()
        {
            var symWriter = GetSymWriter();
            try

            {
                symWriter.CloseMethod();
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void OpenScope(int startOffset)
        {
            var symWriter = GetSymWriter();

            try
            {
                symWriter.OpenScope(startOffset);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void CloseScope(int endOffset)
        {
            var symWriter = GetSymWriter();

            try
            {
                symWriter.CloseScope(endOffset);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void DefineLocalVariable(int index, string name, int attributes, int localSignatureToken)
        {
            var symWriter = GetSymWriter();

            try
            {
                const uint ADDR_IL_OFFSET = 1;
                symWriter.DefineLocalVariable2(name, attributes, localSignatureToken, ADDR_IL_OFFSET, index, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override bool DefineLocalConstant(string name, object value, int constantSignatureToken)
        {
            var symWriter = GetSymWriter();

            switch (value)
            {
                case string str:
                    return DefineLocalStringConstant(symWriter, name, str, constantSignatureToken);

                case DateTime dateTime:
                    // Note: Do not use DefineConstant as it doesn't set the local signature token, which is required in order to avoid callbacks to IMetadataEmit.

                    // Marshal.GetNativeVariantForObject would create a variant with type VT_DATE and value equal to the
                    // number of days since 1899/12/30.  However, ConstantValue::VariantFromConstant in the native VB
                    // compiler actually created a variant with type VT_DATE and value equal to the tick count.
                    // http://blogs.msdn.com/b/ericlippert/archive/2003/09/16/eric-s-complete-guide-to-vt-date.aspx
                    try
                    {
                        symWriter.DefineConstant2(name, new VariantStructure(dateTime), constantSignatureToken);
                    }
                    catch (Exception ex)
                    {
                        throw PdbWritingException(ex);
                    }

                    return true;

                default:
                    try
                    {
                        // ISymUnmanagedWriter2.DefineConstant2 throws an ArgumentException
                        // if you pass in null - Dev10 appears to use 0 instead.
                        // (See EMITTER::VariantFromConstVal)
                        DefineLocalConstantImpl(symWriter, name, value ?? s_zeroInt32, constantSignatureToken);
                    }
                    catch (Exception ex)
                    {
                        throw PdbWritingException(ex);
                    }

                    return true;
            }
        }

        private unsafe void DefineLocalConstantImpl(ISymUnmanagedWriter5 symWriter, string name, object value, int constantSignatureToken)
        {
#if NET6_0_OR_GREATER
            Debug.Assert(OperatingSystem.IsWindows());
#endif
            VariantStructure variant = new VariantStructure();
#pragma warning disable CS0618 // Type or member is obsolete
            Marshal.GetNativeVariantForObject(value, new IntPtr(&variant));
#pragma warning restore CS0618 // Type or member is obsolete
            symWriter.DefineConstant2(name, variant, constantSignatureToken);
        }

        private bool DefineLocalStringConstant(ISymUnmanagedWriter5 symWriter, string name, string value, int constantSignatureToken)
        {
            Debug.Assert(value != null);

            int encodedLength;

            // ISymUnmanagedWriter2 doesn't handle unicode strings with unmatched unicode surrogates.
            // We use the .NET UTF-8 encoder to replace unmatched unicode surrogates with unicode replacement character.

            if (!IsValidUnicodeString(value))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                encodedLength = bytes.Length;
                value = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            }
            else
            {
                encodedLength = Encoding.UTF8.GetByteCount(value);
            }

            // +1 for terminating NUL character
            encodedLength++;

            // If defining a string constant and it is too long (length limit is not documented by the API), DefineConstant2 throws an ArgumentException.
            // However, diasymreader doesn't calculate the length correctly in presence of NUL characters in the string.
            // Until that's fixed we need to check the limit ourselves. See http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/178988
            if (encodedLength > 2032)
            {
                return false;
            }

            try
            {
                DefineLocalConstantImpl(symWriter, name, value, constantSignatureToken);
            }
            catch (ArgumentException)
            {
                // writing the constant value into the PDB failed because the string value was most probably too long.
                // We will report a warning for this issue and continue writing the PDB. 
                // The effect on the debug experience is that the symbol for the constant will not be shown in the local
                // window of the debugger. Nor will the user be able to bind to it in expressions in the EE.

                //The triage team has deemed this new warning undesirable. The effects are not significant. The warning
                //is showing up in the DevDiv build more often than expected. We never warned on it before and nobody cared.
                //The proposed warning is not actionable with no source location.
                return false;
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }

            return true;
        }

        private static bool IsValidUnicodeString(string str)
        {
            int i = 0;
            while (i < str.Length)
            {
                char c = str[i++];

                // (high surrogate, low surrogate) makes a valid pair, anything else is invalid:
                if (char.IsHighSurrogate(c))
                {
                    if (i < str.Length && char.IsLowSurrogate(str[i]))
                    {
                        i++;
                    }
                    else
                    {
                        // high surrogate not followed by low surrogate
                        return false;
                    }
                }
                else if (char.IsLowSurrogate(c))
                {
                    // previous character wasn't a high surrogate
                    return false;
                }
            }

            return true;
        }

        public override void UsingNamespace(string importString)
        {
            if (importString == null)
            {
                throw new ArgumentNullException(nameof(importString));
            }

            var symWriter = GetSymWriter();

            try
            {
                symWriter.UsingNamespace(importString);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void SetAsyncInfo(
            int moveNextMethodToken,
            int kickoffMethodToken,
            int catchHandlerOffset,
            ReadOnlySpan<int> yieldOffsets,
            ReadOnlySpan<int> resumeOffsets)
        {
            if (yieldOffsets == null) throw new ArgumentNullException(nameof(yieldOffsets));
            if (resumeOffsets == null) throw new ArgumentNullException(nameof(resumeOffsets));

            if (yieldOffsets.Length != resumeOffsets.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(yieldOffsets));
            }

            if (GetSymWriter() is ISymUnmanagedAsyncMethodPropertiesWriter asyncMethodPropertyWriter)
            {
                int count = yieldOffsets.Length;

                if (count > 0)
                {
                    var methods = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        methods[i] = moveNextMethodToken;
                    }

                    try
                    {
                        unsafe
                        {
                            fixed (int* yieldPtr = yieldOffsets)
                            fixed (int* resumePtr = resumeOffsets)
                            fixed (int* methodsPtr = methods)
                            {
                                asyncMethodPropertyWriter.DefineAsyncStepInfo(count, yieldPtr, resumePtr, methodsPtr);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw PdbWritingException(ex);
                    }
                }

                try
                {
                    if (catchHandlerOffset >= 0)
                    {
                        asyncMethodPropertyWriter.DefineCatchHandlerILOffset(catchHandlerOffset);
                    }

                    asyncMethodPropertyWriter.DefineKickoffMethod(kickoffMethodToken);
                }
                catch (Exception ex)
                {
                    throw PdbWritingException(ex);
                }
            }
        }

        public override unsafe void DefineCustomMetadata(byte[] metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (metadata.Length == 0)
            {
                return;
            }

            var symWriter = GetSymWriter();

            try
            {
                fixed (byte* pb = metadata)
                {
                    // parent parameter is not used, it must be zero or the current method token passed to OpenMethod.
                    symWriter.SetSymAttribute(0, "MD2", metadata.Length, pb);
                }
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void SetEntryPoint(int entryMethodToken)
        {
            var symWriter = GetSymWriter();

            try
            {
                symWriter.SetUserEntryPoint(entryMethodToken);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void UpdateSignature(Guid guid, uint stamp, int age)
        {
            var symWriter = GetSymWriter8();

            try
            {
                symWriter.UpdateSignature(guid, stamp, age);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override unsafe void SetSourceServerData(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length == 0)
            {
                return;
            }

            var symWriter = GetSymWriter8();

            try
            {
                fixed (byte* dataPtr = data)
                {
                    symWriter.SetSourceServerData(dataPtr, data.Length);
                }
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override unsafe void SetSourceLinkData(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length == 0)
            {
                return;
            }

            var symWriter = GetSymWriter8();

            try
            {
                fixed (byte* dataPtr = data)
                {
                    symWriter.SetSourceLinkData(dataPtr, data.Length);
                }
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void OpenTokensToSourceSpansMap()
        {
            var symWriter = GetSymWriter();

            try
            {
                symWriter.OpenMapTokensToSourceSpans();
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void MapTokenToSourceSpan(int token, int documentIndex, int startLine, int startColumn, int endLine, int endColumn)
        {
            if (documentIndex < 0 || documentIndex >= _documentWriters.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(documentIndex));
            }

            var symWriter = GetSymWriter();

            try
            {
                symWriter.MapTokenToSourceSpan(
                    token,
                    _documentWriters[documentIndex],
                    startLine,
                    startColumn,
                    endLine,
                    endColumn);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override void CloseTokensToSourceSpansMap()
        {
            var symWriter = GetSymWriter();

            try
            {
                symWriter.CloseMapTokensToSourceSpans();
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }

        public override unsafe void GetSignature(out Guid guid, out uint stamp, out int age)
        {
            var symWriter = GetSymWriter();

            // See symwrite.cpp - the data byte[] doesn't depend on the content of metadata tables or IL.
            // The writer only sets two values of the ImageDebugDirectory struct.
            // 
            //   IMAGE_DEBUG_DIRECTORY *pIDD
            // 
            //   if ( pIDD == NULL ) return E_INVALIDARG;
            //   memset( pIDD, 0, sizeof( *pIDD ) );
            //   pIDD->Type = IMAGE_DEBUG_TYPE_CODEVIEW;
            //   pIDD->SizeOfData = cTheData;

            var debugDir = new ImageDebugDirectory();
            uint dataLength;

            try
            {
                symWriter.GetDebugInfo(ref debugDir, 0, out dataLength, null);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }

            byte[] data = new byte[dataLength];
            fixed (byte* pb = data)
            {
                try
                {
                    symWriter.GetDebugInfo(ref debugDir, dataLength, out dataLength, pb);
                }
                catch (Exception ex)
                {
                    throw PdbWritingException(ex);
                }
            }

            // Data has the following structure:
            // struct RSDSI                     
            // {
            //     DWORD dwSig;                 // "RSDS"
            //     GUID guidSig;                // GUID
            //     DWORD age;                   // age
            //     char szPDB[0];               // zero-terminated UTF-8 file name passed to the writer
            // };
            const int GuidSize = 16;
            var guidBytes = new byte[GuidSize];
            Buffer.BlockCopy(data, 4, guidBytes, 0, guidBytes.Length);
            guid = new Guid(guidBytes);

            // Retrieve the timestamp the PDB writer generates when creating a new PDB stream.
            // Note that ImageDebugDirectory.TimeDateStamp is not set by GetDebugInfo, 
            // we need to go through IPdbWriter interface to get it.
            ((IPdbWriter)symWriter).GetSignatureAge(out stamp, out age);
        }

        public override void AddCompilerInfo(ushort major, ushort minor, ushort build, ushort revision, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var symWriter = GetSymWriter();
            if (symWriter is not ISymUnmanagedCompilerInfoWriter infoWriter)
            {
                return;
            }

            try
            {
                infoWriter.AddCompilerInfo(major, minor, build, revision, name);
            }
            catch (Exception ex)
            {
                throw PdbWritingException(ex);
            }
        }
    }
}
