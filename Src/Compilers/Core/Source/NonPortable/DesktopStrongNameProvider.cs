// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.Runtime.Hosting.Interop;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Instrumentation;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides strong name and signs source assemblies.
    /// </summary>
    public class DesktopStrongNameProvider : StrongNameProvider
    {
        private sealed class TempFileStream : FileStream
        {
            public TempFileStream()
                : base(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)
            {
            }

            public void DisposeUnderlyingStream()
            {
                base.Dispose(disposing: true);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                try
                {
                    File.Delete(Name);
                }
                catch
                {
                }
            }
        }

        private readonly ImmutableArray<string> keyFileSearchPaths;

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

            this.keyFileSearchPaths = keyFileSearchPaths.NullToEmpty();
        }

        internal virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath == null || PathUtilities.IsAbsolute(fullPath));
            return File.Exists(fullPath);
        }

        internal virtual byte[] ReadAllBytes(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return File.ReadAllBytes(fullPath);
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

            foreach (var searchPath in this.keyFileSearchPaths)
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
            return new TempFileStream();
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
            catch (COMException ex)
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
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message);
            }

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

        /// <exception cref="IOException"></exception>
        internal override void SignAssembly(StrongNameKeys keys, Stream inputStream, Stream outputStream)
        {
            Debug.Assert(inputStream is TempFileStream);

            var tempStream = (TempFileStream)inputStream;
            string assemblyFilePath = tempStream.Name;
            tempStream.DisposeUnderlyingStream();

            if (keys.KeyContainer != null)
            {
                Sign(assemblyFilePath, keys.KeyContainer);
            }
            else
            {
                Sign(assemblyFilePath, keys.KeyPair);
            }

            using (var fileToSign = new FileStream(assemblyFilePath, FileMode.Open))
            {
                fileToSign.CopyTo(outputStream);
            }
        }

        //Last seen key file blob and corresponding public key.
        //In IDE typing scenarios scenarios we often need to infer public key from the same 
        //key file blob repeatedly and it is relatively expensive.
        //So we will store last seen blob and corresponding key here.
        private static Tuple<byte[], ImmutableArray<byte>> lastSeenKeyPair;

        // EDMAURER in the event that the key is supplied as a file,
        // this type could get an instance member that caches the file
        // contents to avoid reading the file twice - once to get the
        // public key to establish the assembly name and another to do 
        // the actual signing

        private static Guid CLSID_CLRStrongName =
            new Guid(0xB79B0ACD, 0xF5CD, 0x409b, 0xB5, 0xA5, 0xA1, 0x62, 0x44, 0x61, 0x0B, 0x92);

        // internal for testing
        internal static ICLRStrongName GetStrongNameInterface()
        {
            return ClrMetaHost.CurrentRuntime.GetInterface<ICLRStrongName>(CLSID_CLRStrongName);
        }

        // internal for testing
        internal static ImmutableArray<byte> GetPublicKey(string keyContainer)
        {
            ICLRStrongName strongName = GetStrongNameInterface();

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

        private const uint ALG_CLASS_SIGNATURE = 1 << 13;
        private const uint ALG_CLASS_HASH = 4 << 13;

        private static uint GET_ALG_CLASS(uint x) { return x & (7 << 13); }

        internal static unsafe bool IsPublicKeyBlob(byte[] keyFileContents)
        {
            if (keyFileContents.Length < (4 * 3))
                return false;

            fixed (byte* p = keyFileContents)
            {
                return (GET_ALG_CLASS((uint)Marshal.ReadInt32((IntPtr)p)) == ALG_CLASS_SIGNATURE) &&
                    (GET_ALG_CLASS((uint)Marshal.ReadInt32((IntPtr)p, 4)) == ALG_CLASS_HASH) &&
                    (Marshal.ReadInt32((IntPtr)p, 8) + (4 * 3) == keyFileContents.Length);
            }
        }

        // internal for testing
        /// <exception cref="IOException"/>
        internal static ImmutableArray<byte> GetPublicKey(byte[] keyFileContents)
        {
            try
            {
                var lastSeen = lastSeenKeyPair;
                if (lastSeen != null && ByteSequenceComparer.ValueEquals(lastSeen.Item1, keyFileContents))
                {
                    return lastSeen.Item2;
                }

                ICLRStrongName strongName = GetStrongNameInterface();

                IntPtr keyBlob;
                int keyBlobByteCount;

                //EDMAURER use marshal to be safe?
                unsafe
                {
                    fixed (byte* p = keyFileContents)
                    {
                        try
                        {
                            strongName.StrongNameGetPublicKey(null, (IntPtr)p, keyFileContents.Length, out keyBlob, out keyBlobByteCount);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new IOException(ex.Message);
                        }
                    }
                }

                byte[] pubKey = new byte[keyBlobByteCount];
                Marshal.Copy(keyBlob, pubKey, 0, keyBlobByteCount);
                strongName.StrongNameFreeBuffer(keyBlob);

                var result = pubKey.AsImmutableOrNull();
                lastSeenKeyPair = Tuple.Create(keyFileContents, result);

                return result;
            }
            catch (COMException ex)
            {
                throw new IOException(ex.Message);
            }
        }

        /*  leave this out for now. We'll make the command line compilers have this behavior, but
         * not the compiler library. This differs from previous versions because specifying a keyfile
         * and container through either the command line or attributes gave this "install the key" behavior.
         * 
         * 
        //EDMAURER alink had weird, MSDN spec'd behavior. from MSDN "In case both /keyfile and /keycontainer are 
        //specified (either by command-line option or by custom attribute) in the same compilation, the compiler 
        //first tries the key container. If that succeeds, then the assembly is signed with the information in the 
        //key container. If the compiler does not find the key container, it tries the file specified with /keyfile. 
        //If this succeeds, the assembly is signed with the information in the key file, and the key information is 
        //installed in the key container (similar to sn -i) so that on the next compilation, the key container will 
        //be valid.

        private static ImmutableArray<byte> GetPublicKeyAndPossiblyInstall(string keyFilename, string keyContainer)
        {
            if (keyContainer != null)
            {
                ImmutableArray<byte> result = GetPublicKey(keyContainer);

                if (result.IsNotNull)
                    return result;
            }

            if (keyFilename != null)
            {
                byte[] keyFileContents = System.IO.File.ReadAllBytes(keyFilename);

                if (keyContainer != null)
                    InstallKey(keyFileContents, keyFilename);

                return GetPublicKey(keyFileContents);
            }

            return default(ImmutableArray<byte>);
        }
         */

        /// <exception cref="IOException"/>
        private static void Sign(string filePath, string keyName)
        {
            try
            {
                ICLRStrongName strongName = GetStrongNameInterface();

                int unused;
                strongName.StrongNameSignatureGeneration(filePath, keyName, IntPtr.Zero, 0, null, out unused);
            }
            catch (COMException ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }

        /// <exception cref="IOException"/>
        private static void Sign(string filePath, ImmutableArray<byte> keyPair)
        {
            try
            {
                ICLRStrongName strongName = GetStrongNameInterface();

                using (var pinned = PinnedImmutableArray.Create(keyPair))
                {
                    int unused;
                    strongName.StrongNameSignatureGeneration(filePath, null, pinned.Pointer, keyPair.Length, null, out unused);
                }
            }
            catch (COMException ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }
    }
}
