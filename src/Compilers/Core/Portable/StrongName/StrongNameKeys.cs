// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    internal sealed class StrongNameKeys
    {
        /// <summary>
        /// The strong name key associated with the identity of this assembly. 
        /// This contains the contents of the user-supplied key file exactly as extracted.
        /// </summary>
        internal readonly ImmutableArray<byte> KeyPair;

        /// <summary>
        /// Determines source assembly identity.
        /// </summary>
        internal readonly ImmutableArray<byte> PublicKey;

        /// <summary>
        /// A diagnostic created in the process of determining the key.
        /// </summary>
        internal readonly Diagnostic DiagnosticOpt;

        /// <summary>
        /// The CSP key container containing the public key used to produce the key,
        /// or null if the key was retrieved from <see cref="KeyFilePath"/>.
        /// </summary>
        /// <remarks>
        /// The original value as specified by <see cref="System.Reflection.AssemblyKeyNameAttribute"/> or 
        /// <see cref="CompilationOptions.CryptoKeyContainer"/>.
        /// </remarks>
        internal readonly string KeyContainer;

        /// <summary>
        /// Original key file path, or null if the key is provided by the <see cref="KeyContainer"/>.
        /// </summary>
        /// <remarks>
        /// The original value as specified by <see cref="System.Reflection.AssemblyKeyFileAttribute"/> or 
        /// <see cref="CompilationOptions.CryptoKeyFile"/>
        /// </remarks>
        internal readonly string KeyFilePath;

        internal static readonly StrongNameKeys None = new StrongNameKeys();

        private StrongNameKeys()
        {
        }

        internal StrongNameKeys(Diagnostic diagnostic)
        {
            Debug.Assert(diagnostic != null);
            this.DiagnosticOpt = diagnostic;
        }

        internal StrongNameKeys(ImmutableArray<byte> keyPair, ImmutableArray<byte> publicKey, string keyContainerName, string keyFilePath)
        {
            Debug.Assert(keyContainerName == null || keyPair.IsDefault);
            Debug.Assert(keyPair.IsDefault || keyFilePath != null);

            this.KeyPair = keyPair;
            this.PublicKey = publicKey;
            this.KeyContainer = keyContainerName;
            this.KeyFilePath = keyFilePath;
        }

        internal static StrongNameKeys Create(ImmutableArray<byte> publicKey, CommonMessageProvider messageProvider)
        {
            Debug.Assert(!publicKey.IsDefaultOrEmpty);

            if (MetadataHelpers.IsValidPublicKey(publicKey))
            {
                return new StrongNameKeys(default(ImmutableArray<byte>), publicKey, null, null);
            }
            else
            {
                return new StrongNameKeys(messageProvider.CreateDiagnostic(messageProvider.ERR_BadCompilationOptionValue, Location.None,
                    nameof(CompilationOptions.CryptoPublicKey), BitConverter.ToString(publicKey.ToArray())));
            }
        }

        internal static StrongNameKeys Create(string keyFilePath, CommonMessageProvider messageProvider)
        {
            if (string.IsNullOrEmpty(keyFilePath))
            {
                return None;
            }

            ImmutableArray<byte> content;
            try
            {
                Debug.Assert(PathUtilities.IsAbsolute(keyFilePath));
                content = ImmutableArray.Create(PortableShim.File.ReadAllBytes(keyFilePath));
            }
            catch (IOException ex)
            {
                return new StrongNameKeys(GetKeyFileError(messageProvider, keyFilePath, ex.Message));
            }

            if (MetadataHelpers.IsValidPublicKey(content))
            {
                return new StrongNameKeys(default(ImmutableArray<byte>), content, null, keyFilePath);
            }
            else
            {
                return new StrongNameKeys(
                    messageProvider.CreateDiagnostic(messageProvider.ERR_InvalidPublicKeyFile,
                        Location.None, keyFilePath));
            }
        }

        internal static StrongNameKeys Create(StrongNameProvider providerOpt, string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider)
        {
            if (string.IsNullOrEmpty(keyFilePath) && string.IsNullOrEmpty(keyContainerName))
            {
                return None;
            }

            if (providerOpt == null)
            {
                var diagnostic = GetError(keyFilePath, keyContainerName, new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.AssemblySigningNotSupported)), messageProvider);
                return new StrongNameKeys(diagnostic);
            }

            return providerOpt.CreateKeys(keyFilePath, keyContainerName, messageProvider);
        }

        /// <summary>
        /// True if the compilation can be signed using these keys.
        /// </summary>
        internal bool CanSign
        {
            get
            {
                return !KeyPair.IsDefault || KeyContainer != null;
            }
        }

        /// <summary>
        /// True if a strong name can be created for the compilation using these keys.
        /// </summary>
        internal bool CanProvideStrongName
        {
            get
            {
                return CanSign || !PublicKey.IsDefault;
            }
        }

        internal static Diagnostic GetError(string keyFilePath, string keyContainerName, object message, CommonMessageProvider messageProvider)
        {
            if (keyContainerName != null)
            {
                return GetContainerError(messageProvider, keyContainerName, message);
            }
            else
            {
                return GetKeyFileError(messageProvider, keyFilePath, message);
            }
        }

        internal static Diagnostic GetContainerError(CommonMessageProvider messageProvider, string name, object message)
        {
            return messageProvider.CreateDiagnostic(messageProvider.ERR_PublicKeyContainerFailure, Location.None, name, message);
        }

        internal static Diagnostic GetKeyFileError(CommonMessageProvider messageProvider, string path, object message)
        {
            return messageProvider.CreateDiagnostic(messageProvider.ERR_PublicKeyFileFailure, Location.None, path, message);
        }

        internal static bool IsValidPublicKeyString(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey) || publicKey.Length % 2 != 0)
            {
                return false;
            }

            foreach (char c in publicKey)
            {
                if (!(c >= '0' && c <= '9') &&
                    !(c >= 'a' && c <= 'f') &&
                    !(c >= 'A' && c <= 'F'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
