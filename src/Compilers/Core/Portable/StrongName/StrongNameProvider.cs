// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides strong name and signs source assemblies.
    /// </summary>
    public abstract class StrongNameProvider
    {
        protected StrongNameProvider()
        {
        }

        public abstract override int GetHashCode();
        public abstract override bool Equals(object? other);

        internal abstract StrongNameFileSystem FileSystem { get; }

        /// <summary>
        /// Signs the <paramref name="filePath"/> value using <paramref name="keys"/>.
        /// </summary>
        internal abstract void SignFile(StrongNameKeys keys, string filePath);

        /// <summary>
        /// Signs the contents of <paramref name="peBlob"/> using <paramref name="peBuilder"/> and <paramref name="privateKey"/>.
        /// </summary>
        internal abstract void SignBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privateKey);

        /// <summary>
        /// Create a <see cref="StrongNameKeys"/> for the provided information.
        /// </summary>
        internal abstract StrongNameKeys CreateKeys(string? keyFilePath, string? keyContainerName, CommonMessageProvider messageProvider);

        internal static StrongNameKeys CreateKeys(StrongNameProvider? provider, string? keyFilePath, string? keyContainerName, CommonMessageProvider messageProvider)
        {
            if (string.IsNullOrEmpty(keyFilePath) && string.IsNullOrEmpty(keyContainerName))
            {
                return StrongNameKeys.None;
            }

            if (provider is null)
            {
                var diagnostic = StrongNameKeys.GetError(keyFilePath, keyContainerName, new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.AssemblySigningNotSupported)), messageProvider);
                return new StrongNameKeys(diagnostic);
            }

            return provider.CreateKeys(keyFilePath, keyContainerName, messageProvider);
        }
    }
}
