﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Interop;
using Roslyn.Utilities;

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
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "keyFileSearchPaths");
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
                    ReadKeysFromPath(resolvedKeyFile, out keyPair, out publicKey);
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

        private void ReadKeysFromPath(string fullPath, out ImmutableArray<byte> keyPair, out ImmutableArray<byte> publicKey)
        {
            byte[] fileContent;
            try
            {
                fileContent = ReadAllBytes(fullPath);
                if (IsPublicKeyBlob(fileContent))
                {
                    publicKey = ImmutableArray.CreateRange(fileContent);
                    keyPair = default(ImmutableArray<byte>);
                }
                else
                {
                    publicKey = GetPublicKey(fileContent);
                    keyPair = ImmutableArray.CreateRange(fileContent);
                }
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

        //Last seen key file blob and corresponding public key.
        //In IDE typing scenarios we often need to infer public key from the same 
        //key file blob repeatedly and it is relatively expensive.
        //So we will store last seen blob and corresponding key here.
        private static Tuple<byte[], ImmutableArray<byte>> s_lastSeenKeyPair;

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

        //The definition of a public key blob from StrongName.h

        //typedef struct {
        //    unsigned int SigAlgId;
        //    unsigned int HashAlgId;
        //    ULONG cbPublicKey;
        //    BYTE PublicKey[1]
        //} PublicKeyBlob; 

        //__forceinline bool IsValidPublicKeyBlob(const PublicKeyBlob *p, const size_t len)
        //{
        //    return ((VAL32(p->cbPublicKey) + (sizeof(ULONG) * 3)) == len &&         // do the lengths match?
        //            GET_ALG_CLASS(VAL32(p->SigAlgID)) == ALG_CLASS_SIGNATURE &&     // is it a valid signature alg?
        //            GET_ALG_CLASS(VAL32(p->HashAlgID)) == ALG_CLASS_HASH);         // is it a valid hash alg?
        //}

        private static uint GET_ALG_CLASS(uint x) { return x & (7 << 13); }

        internal static unsafe bool IsPublicKeyBlob(byte[] keyFileContents)
        {
            const uint ALG_CLASS_SIGNATURE = 1 << 13;
            const uint ALG_CLASS_HASH = 4 << 13;

            if (keyFileContents.Length < (4 * 3))
            {
                return false;
            }

            fixed (byte* p = keyFileContents)
            {
                return (GET_ALG_CLASS((uint)Marshal.ReadInt32((IntPtr)p)) == ALG_CLASS_SIGNATURE) &&
                    (GET_ALG_CLASS((uint)Marshal.ReadInt32((IntPtr)p, 4)) == ALG_CLASS_HASH) &&
                    (Marshal.ReadInt32((IntPtr)p, 8) + (4 * 3) == keyFileContents.Length);
            }
        }

        // internal for testing
        /// <exception cref="IOException"/>
        internal ImmutableArray<byte> GetPublicKey(byte[] keyFileContents)
        {
            try
            {
                var lastSeen = s_lastSeenKeyPair;
                if (lastSeen != null && ByteSequenceComparer.Equals(lastSeen.Item1, keyFileContents))
                {
                    return lastSeen.Item2;
                }

                IClrStrongName strongName = GetStrongNameInterface();

                IntPtr keyBlob;
                int keyBlobByteCount;

                //EDMAURER use marshal to be safe?
                unsafe
                {
                    fixed (byte* p = keyFileContents)
                    {
                        strongName.StrongNameGetPublicKey(null, (IntPtr)p, keyFileContents.Length, out keyBlob, out keyBlobByteCount);
                    }
                }

                byte[] pubKey = new byte[keyBlobByteCount];
                Marshal.Copy(keyBlob, pubKey, 0, keyBlobByteCount);
                strongName.StrongNameFreeBuffer(keyBlob);

                var result = pubKey.AsImmutableOrNull();
                s_lastSeenKeyPair = Tuple.Create(keyFileContents, result);

                return result;
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message);
            }
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
