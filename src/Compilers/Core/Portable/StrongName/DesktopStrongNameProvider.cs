// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides strong name and signs source assemblies.
    /// </summary>
    public class DesktopStrongNameProvider : StrongNameProvider
    {
        // This exception is only used to detect when the acquisition of IClrStrongName fails
        // and the likely reason is that we're running on CoreCLR on a non-Windows platform.
        // The place where the acquisition fails does not have access to localization,
        // so we can't throw some generic exception with a localized message.
        // So this is sort of a token for the eventual message to be generated.

        // The path from where this is thrown to where it is caught is all internal,
        // so there's no chance of an API consumer seeing it.
        internal sealed class ClrStrongNameMissingException : Exception
        {
        }

        private readonly ImmutableArray<string> _keyFileSearchPaths;
        internal override StrongNameFileSystem FileSystem { get; }

        public DesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths) : this(keyFileSearchPaths, StrongNameFileSystem.Instance)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DesktopStrongNameProvider"/>.
        /// </summary>
        /// <param name="tempPath">Path to use for any temporary file generation.</param>
        /// <param name="keyFileSearchPaths">An ordered set of fully qualified paths which are searched when locating a cryptographic key file.</param>
        public DesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths = default, string? tempPath = null)
           : this(keyFileSearchPaths, tempPath == null ? StrongNameFileSystem.Instance : new StrongNameFileSystem(tempPath))
        {

        }

        internal DesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths, StrongNameFileSystem strongNameFileSystem)
        {
            if (!keyFileSearchPaths.IsDefault && keyFileSearchPaths.Any(static path => !PathUtilities.IsAbsolute(path)))
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, nameof(keyFileSearchPaths));
            }

            FileSystem = strongNameFileSystem ?? StrongNameFileSystem.Instance;
            _keyFileSearchPaths = keyFileSearchPaths.NullToEmpty();
        }

        internal override StrongNameKeys CreateKeys(string? keyFilePath, string? keyContainerName, bool hasCounterSignature, CommonMessageProvider messageProvider)
        {
            var keyPair = default(ImmutableArray<byte>);
            var publicKey = default(ImmutableArray<byte>);
            string? container = null;

            if (!string.IsNullOrEmpty(keyFilePath))
            {
                try
                {
                    string? resolvedKeyFile = ResolveStrongNameKeyFile(keyFilePath, FileSystem, _keyFileSearchPaths);
                    if (resolvedKeyFile == null)
                    {
                        return new StrongNameKeys(StrongNameKeys.GetKeyFileError(messageProvider, keyFilePath, CodeAnalysisResources.FileNotFound));
                    }

                    Debug.Assert(PathUtilities.IsAbsolute(resolvedKeyFile));
                    var fileContent = ImmutableArray.Create(FileSystem.ReadAllBytes(resolvedKeyFile));
                    return StrongNameKeys.CreateHelper(fileContent, keyFilePath, hasCounterSignature);
                }
                catch (Exception ex)
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
                catch (ClrStrongNameMissingException)
                {
                    return new StrongNameKeys(StrongNameKeys.GetContainerError(messageProvider, keyContainerName,
                        new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.AssemblySigningNotSupported))));
                }
                catch (Exception ex)
                {
                    return new StrongNameKeys(StrongNameKeys.GetContainerError(messageProvider, keyContainerName, ex.Message));
                }
            }

            return new StrongNameKeys(keyPair, publicKey, privateKey: null, container, keyFilePath, hasCounterSignature);
        }

        /// <summary>
        /// Resolves assembly strong name key file path.
        /// </summary>
        /// <returns>Normalized key file path or null if not found.</returns>
        internal static string? ResolveStrongNameKeyFile(string path, StrongNameFileSystem fileSystem, ImmutableArray<string> keyFileSearchPaths)
        {
            // Dev11: key path is simply appended to the search paths, even if it starts with the current (parent) directory ("." or "..").
            // This is different from PathUtilities.ResolveRelativePath.

            if (PathUtilities.IsAbsolute(path))
            {
                if (fileSystem.FileExists(path))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(path);
                }

                return path;
            }

            foreach (var searchPath in keyFileSearchPaths)
            {
                string? combinedPath = PathUtilities.CombineAbsoluteAndRelativePaths(searchPath, path);

                Debug.Assert(combinedPath == null || PathUtilities.IsAbsolute(combinedPath));

                if (fileSystem.FileExists(combinedPath))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(combinedPath!);
                }
            }

            return null;
        }

        internal virtual void ReadKeysFromContainer(string keyContainer, out ImmutableArray<byte> publicKey)
        {
            try
            {
                publicKey = GetPublicKey(keyContainer);
            }
            catch (ClrStrongNameMissingException)
            {
                // pipe it through so it's catchable directly by type
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message);
            }
        }

        internal override void SignFile(StrongNameKeys keys, string filePath)
        {
            Debug.Assert(string.IsNullOrEmpty(keys.KeyFilePath) != string.IsNullOrEmpty(keys.KeyContainer));

            if (!string.IsNullOrEmpty(keys.KeyFilePath))
            {
                Sign(filePath, keys.KeyPair);
            }
            else
            {
                Sign(filePath, keys.KeyContainer!);
            }
        }

        internal override void SignBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privateKey)
        {
            peBuilder.Sign(peBlob, content => SigningUtilities.CalculateRsaSignature(content, privateKey));
        }

        // EDMAURER in the event that the key is supplied as a file,
        // this type could get an instance member that caches the file
        // contents to avoid reading the file twice - once to get the
        // public key to establish the assembly name and another to do
        // the actual signing

        internal virtual IClrStrongName GetStrongNameInterface()
        {
            try
            {
                return ClrStrongName.GetInstance();
            }
            catch (MarshalDirectiveException) when (PathUtilities.IsUnixLikePlatform)
            {
                // CoreCLR, when not on Windows, doesn't support IClrStrongName (or COM in general).
                // This is really hard to detect/predict without false positives/negatives.
                // It turns out that CoreCLR throws a MarshalDirectiveException when attempting
                // to get the interface (Message "Cannot marshal 'return value': Unknown error."),
                // so just catch that and state that it's not supported.

                // We're deep in a try block that reports the exception's Message as part of a diagnostic.
                // This exception will skip through the IOException wrapping by `Sign` (in this class),
                // then caught by Compilation.SerializeToPeStream or DesktopStringNameProvider.CreateKeys
                throw new ClrStrongNameMissingException();
            }
        }

        internal ImmutableArray<byte> GetPublicKey(string keyContainer)
        {
            IClrStrongName strongName = GetStrongNameInterface();

            IntPtr keyBlob;
            int keyBlobByteCount;

            strongName.StrongNameGetPublicKey(keyContainer, pbKeyBlob: default, 0, out keyBlob, out keyBlobByteCount);

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
                strongName.StrongNameSignatureGeneration(filePath, keyName, IntPtr.Zero, 0, null, pcbSignatureBlob: out _);
            }
            catch (ClrStrongNameMissingException)
            {
                // pipe it through so it's catchable directly by type
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }

        private unsafe void Sign(string filePath, ImmutableArray<byte> keyPair)
        {
            try
            {
                IClrStrongName strongName = GetStrongNameInterface();

                fixed (byte* pinned = keyPair.ToArray())
                {
                    strongName.StrongNameSignatureGeneration(filePath, null, (IntPtr)pinned, keyPair.Length, null, pcbSignatureBlob: out _);
                }
            }
            catch (ClrStrongNameMissingException)
            {
                // pipe it through so it's catchable directly by type
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }

        public override int GetHashCode()
        {
            return Hash.CombineValues(_keyFileSearchPaths, StringComparer.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (DesktopStrongNameProvider)obj;
            if (FileSystem != other.FileSystem)
            {
                return false;
            }

            if (!_keyFileSearchPaths.SequenceEqual(other._keyFileSearchPaths, StringComparer.Ordinal))
            {
                return false;
            }

            return true;
        }
    }
}
