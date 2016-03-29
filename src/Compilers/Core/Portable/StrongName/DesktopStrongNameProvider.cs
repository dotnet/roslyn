// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Interop;
using Roslyn.Utilities;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides strong name and signs source assemblies.
    /// </summary>
    public class DesktopStrongNameProvider : StrongNameProvider
    {
        private sealed class TempFileStream : Stream
        {
            private readonly string _path;
            private readonly Stream _stream;

            public string Path
            {
                get { return _path; }
            }

            public TempFileStream(string path, Stream stream)
            {
                _path = path;
                _stream = stream;
            }

            public void DisposeUnderlyingStream()
            {
                _stream.Dispose();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                _stream.Dispose();
                try
                {
                    PortableShim.File.Delete(_path);
                }
                catch
                {
                }
            }

            public override bool CanRead
            {
                get { return _stream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _stream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _stream.CanWrite; }
            }

            public override void Flush()
            {
                _stream.Flush();
            }

            public override long Length
            {
                get { return _stream.Length; }
            }

            public override long Position
            {
                get { return _stream.Position; }
                set { _stream.Position = value; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _stream.Write(buffer, offset, count);
            }
        }

        private readonly ImmutableArray<string> _keyFileSearchPaths;

        // for testing/mocking
        internal Func<IClrStrongName> TestStrongNameInterfaceFactory;

        /// <summary>
        /// Creates an instance of <see cref="DesktopStrongNameProvider"/>.
        /// </summary>
        /// <param name="keyFileSearchPaths">
        /// An ordered set of fully qualified paths which are searched when locating a cryptographic key file.
        /// </param>
        public DesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths = default(ImmutableArray<string>))
        {
            if (!keyFileSearchPaths.IsDefault && keyFileSearchPaths.Any(path => !PathUtilities.IsAbsolute(path)))
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, nameof(keyFileSearchPaths));
            }

            _keyFileSearchPaths = keyFileSearchPaths.NullToEmpty();
        }

        internal virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath == null || PathUtilities.IsAbsolute(fullPath));
            return PortableShim.File.Exists(fullPath);
        }

        internal virtual byte[] ReadAllBytes(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return PortableShim.File.ReadAllBytes(fullPath);
        }

        /// <summary>
        /// Resolves assembly strong name key file path.
        /// Internal for testing.
        /// </summary>
        /// <returns>Normalized key file path or null if not found.</returns>
        internal string ResolveStrongNameKeyFile(string path)
        {
            // Dev11: key path is simply appended to the search paths, even if it starts with the current (parent) directory ("." or "..").
            // This is different from PathUtilities.ResolveRelativePath.

            if (PathUtilities.IsAbsolute(path))
            {
                if (FileExists(path))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(path);
                }

                return path;
            }

            foreach (var searchPath in _keyFileSearchPaths)
            {
                string combinedPath = PathUtilities.CombineAbsoluteAndRelativePaths(searchPath, path);

                Debug.Assert(combinedPath == null || PathUtilities.IsAbsolute(combinedPath));

                if (FileExists(combinedPath))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(combinedPath);
                }
            }

            return null;
        }

        internal override Stream CreateInputStream()
        {
            var path = PortableShim.Path.GetTempFileName();
            Func<string, Stream> streamConstructor = lPath => new TempFileStream(lPath, PortableShim.FileStream.Create(lPath, PortableShim.FileMode.Create, PortableShim.FileAccess.ReadWrite, PortableShim.FileShare.ReadWrite));
            return FileUtilities.CreateFileStreamChecked(streamConstructor, path);
        }

        internal override StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider)
        {
            var keyPair = default(ImmutableArray<byte>);
            var publicKey = default(ImmutableArray<byte>);
            string container = null;

            if (!string.IsNullOrEmpty(keyFilePath))
            {
                try
                {
                    string resolvedKeyFile = ResolveStrongNameKeyFile(keyFilePath);
                    if (resolvedKeyFile == null)
                    {
                        throw new FileNotFoundException(CodeAnalysisResources.FileNotFound, keyFilePath);
                    }

                    Debug.Assert(PathUtilities.IsAbsolute(resolvedKeyFile));
                    var fileContent = ImmutableArray.Create(ReadAllBytes(resolvedKeyFile));
                    return StrongNameKeys.CreateHelper(fileContent, keyFilePath);
                }
                catch (IOException ex)
                {
                    return new StrongNameKeys(StrongNameKeys.GetKeyFileError(messageProvider, keyFilePath, ex.Message));
                }
            }
            else if (!string.IsNullOrEmpty(keyContainerName))
            {
                try
                {
                    ReadKeysFromContainer(keyContainerName, out publicKey);
                    container = keyContainerName;
                }
                catch (IOException ex)
                {
                    return new StrongNameKeys(StrongNameKeys.GetContainerError(messageProvider, keyContainerName, ex.Message));
                }
            }

            return new StrongNameKeys(keyPair, publicKey, container, keyFilePath);
        }

        private void ReadKeysFromContainer(string keyContainer, out ImmutableArray<byte> publicKey)
        {
            try
            {
                publicKey = GetPublicKey(keyContainer);
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message);
            }
        }

        /// <exception cref="IOException"></exception>
        internal override void SignAssembly(StrongNameKeys keys, Stream inputStream, Stream outputStream)
        {
            Debug.Assert(inputStream is TempFileStream);

            var tempStream = (TempFileStream)inputStream;
            string assemblyFilePath = tempStream.Path;
            tempStream.DisposeUnderlyingStream();

            if (keys.KeyContainer != null)
            {
                Sign(assemblyFilePath, keys.KeyContainer);
            }
            else
            {
                Sign(assemblyFilePath, keys.KeyPair);
            }

            using (var fileToSign = PortableShim.FileStream.Create(assemblyFilePath, PortableShim.FileMode.Open))
            {
                fileToSign.CopyTo(outputStream);
            }
        }

        // EDMAURER in the event that the key is supplied as a file,
        // this type could get an instance member that caches the file
        // contents to avoid reading the file twice - once to get the
        // public key to establish the assembly name and another to do 
        // the actual signing

        // internal for testing
        internal IClrStrongName GetStrongNameInterface()
        {
            return TestStrongNameInterfaceFactory?.Invoke() ?? ClrStrongName.GetInstance();
        }

        internal ImmutableArray<byte> GetPublicKey(string keyContainer)
        {
            IClrStrongName strongName = GetStrongNameInterface();

            IntPtr keyBlob;
            int keyBlobByteCount;

            strongName.StrongNameGetPublicKey(keyContainer, default(IntPtr), 0, out keyBlob, out keyBlobByteCount);

            byte[] pubKey = new byte[keyBlobByteCount];
            Marshal.Copy(keyBlob, pubKey, 0, keyBlobByteCount);
            strongName.StrongNameFreeBuffer(keyBlob);

            return pubKey.AsImmutableOrNull();
        }

        /// <exception cref="IOException"/>
        private void Sign(string filePath, string keyName)
        {
            try
            {
                IClrStrongName strongName = GetStrongNameInterface();

                int unused;
                strongName.StrongNameSignatureGeneration(filePath, keyName, IntPtr.Zero, 0, null, out unused);
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }

        /// <exception cref="IOException"/>
        private unsafe void Sign(string filePath, ImmutableArray<byte> keyPair)
        {
            try
            {
                IClrStrongName strongName = GetStrongNameInterface();

                fixed (byte* pinned = keyPair.ToArray())
                {
                    int unused;
                    strongName.StrongNameSignatureGeneration(filePath, null, (IntPtr)pinned, keyPair.Length, null, out unused);
                }
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }

        public override bool Equals(object obj)
        {
            // Explicitly check that we're not comparing against a derived type
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (DesktopStrongNameProvider)obj;
            return _keyFileSearchPaths.SequenceEqual(other._keyFileSearchPaths, StringComparer.Ordinal);
        }

        public override int GetHashCode()
        {
            return Hash.CombineValues(_keyFileSearchPaths, StringComparer.Ordinal);
        }
    }
}
