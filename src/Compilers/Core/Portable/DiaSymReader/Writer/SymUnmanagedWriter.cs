// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// Windows PDB writer.
    /// </summary>
    internal abstract class SymUnmanagedWriter : IDisposable
    {
        /// <summary>
        /// Disposes the writer.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Gets the raw data blobs that comprise the written PDB content so far.
        /// </summary>
        public abstract IEnumerable<ArraySegment<byte>> GetUnderlyingData();

        /// <summary>
        /// Writes the PDB data to specified stream. Once called no more changes to the data can be made using this writer.
        /// May be called multiple times. Always writes the same data. 
        /// </summary>
        /// <param name="stream">Stream to write PDB data to.</param>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing data to the stream.</exception>
        public abstract void WriteTo(Stream stream);

        /// <summary>
        /// The capacity of document table. 
        /// </summary>
        /// <remarks>
        /// Whenever a document is defined an entry is added to this table. 
        /// If the number of documents is known upfront setting this value may reduce memory consumption.
        /// </remarks>
        public abstract int DocumentTableCapacity { get; set; }

        /// <summary>
        /// Defines a source document.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract int DefineDocument(string name, Guid language, Guid vendor, Guid type, Guid algorithmId, ReadOnlySpan<byte> checksum, ReadOnlySpan<byte> source);

        /// <summary>
        /// Defines sequence points.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void DefineSequencePoints(int documentIndex, int count, int[] offsets, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns);

        /// <summary>
        /// Opens a method.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void OpenMethod(int methodToken);

        /// <summary>
        /// Closes a method previously open using <see cref="OpenMethod(int)"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void CloseMethod();

        /// <summary>
        /// Opens a local scope.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        public abstract void OpenScope(int startOffset);

        /// <summary>
        /// Closes a local scope previously open using <see cref="OpenScope(int)"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void CloseScope(int endOffset);

        /// <summary>
        /// Defines a local variable.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void DefineLocalVariable(int index, string name, int attributes, int localSignatureToken);

        /// <summary>
        /// Defines a local constant.
        /// </summary>
        /// <param name="name">Name of the constant.</param>
        /// <param name="value">Value.</param>
        /// <param name="constantSignatureToken">Standalone signature token encoding the static type of the constant.</param>
        /// <returns>False if the constant representation is too long (e.g. long string).</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null</exception>
        public abstract bool DefineLocalConstant(string name, object value, int constantSignatureToken);

        /// <summary>
        /// Adds namespace import.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="importString"/> is null</exception>
        public abstract void UsingNamespace(string importString);

        /// <summary>
        /// Sets method async information.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="yieldOffsets"/> or <paramref name="resumeOffsets"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="yieldOffsets"/> or <paramref name="resumeOffsets"/> differ in length.</exception>
        public abstract void SetAsyncInfo(
            int moveNextMethodToken,
            int kickoffMethodToken,
            int catchHandlerOffset,
            ReadOnlySpan<int> yieldOffsets,
            ReadOnlySpan<int> resumeOffsets);

        /// <summary>
        /// Associates custom debug information blob with the current method.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null</exception>
        public abstract void DefineCustomMetadata(byte[] metadata);

        /// <summary>
        /// Designates specified method as an entry point.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void SetEntryPoint(int entryMethodToken);

        /// <summary>
        /// Updates the current PDB signature.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void UpdateSignature(Guid guid, uint stamp, int age);

        /// <summary>
        /// Gets the current PDB signature.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void GetSignature(out Guid guid, out uint stamp, out int age);

        /// <summary>
        /// Sets source server data blob (srcsvr stream).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null</exception>
        public abstract void SetSourceServerData(byte[] data);

        /// <summary>
        /// Sets source link data blob (sourcelink stream).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null</exception>
        public abstract void SetSourceLinkData(byte[] data);

        /// <summary>
        /// Opens a map of tokens to source spans.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void OpenTokensToSourceSpansMap();

        /// <summary>
        /// Maps specified token to a source span.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="documentIndex"/> doesn't correspond to any defined document.</exception>
        public abstract void MapTokenToSourceSpan(int token, int documentIndex, int startLine, int startColumn, int endLine, int endColumn);

        /// <summary>
        /// Closes map of tokens to source spans previously opened using <see cref="OpenTokensToSourceSpansMap"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Writes are not allowed to the underlying stream.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        public abstract void CloseTokensToSourceSpansMap();

        /// <summary>
        /// Writes compiler version and name to the PDB.
        /// </summary>
        /// <param name="major">Major version</param>
        /// <param name="minor">Minor version</param>
        /// <param name="build">Build</param>
        /// <param name="revision">Revision</param>
        /// <param name="name">Compiler name</param>
        /// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error occurred while writing PDB data.</exception>
        /// <exception cref="NotSupportedException">The PDB writer does not support adding compiler info.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
        public virtual void AddCompilerInfo(ushort major, ushort minor, ushort build, ushort revision, string name)
            => throw new NotSupportedException();
    }
}
