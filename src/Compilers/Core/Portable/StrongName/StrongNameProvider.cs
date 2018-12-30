// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.Cci;
using Roslyn.Utilities;

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

        // TODO: delete these they don't matter anymore.
        public abstract override int GetHashCode();
        public override abstract bool Equals(object other);

        internal abstract StrongNameFileSystem FileSystem { get; }

        /// <summary>
        /// Create a <see cref="Stream"/> for use in when not signing with a builder (<see cref="Compilation.SignUsingBuilder"/>).
        /// </summary>
        // TODO: delete this entirely as the stream management is now done via EmitStream
        // TOOD: Create and expose a SigningStream. This avoids unnecessary casting in the SignStream method
        internal virtual Stream CreateInputStream() => throw new NotSupportedException();

        /// <summary>
        /// Signs the <paramref name="inputStream"/> value using <paramref name="keys"/> and copies the final result
        /// to <paramref name="outputStream"/>
        /// </summary>
        // TODO: delete this entirely as the stream management is now done via EmitStream
        internal virtual void SignStream(StrongNameKeys keys, Stream inputStream, Stream outputStream) => throw new NotSupportedException();

        /// <summary>
        /// Signs the <paramref name="filePath"/> value using <paramref name="keys"/>.
        /// </summary>
        internal virtual void SignFile(StrongNameKeys keys, string filePath) => throw new NotSupportedException();

        /// <summary>
        /// Signs the contents of <paramref name="peBuilder"/> using <paramref name="privateKey"/>.
        /// </summary>
        // TODO: rename to SignBuilder
        internal virtual void SignPeBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privateKey) => throw new NotSupportedException();

        /// <summary>
        /// Create a <see cref="StrongNameKeys"/> for the provided information.
        /// </summary>
        internal abstract StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, bool hasCounterSignature, CommonMessageProvider messageProvider);
    }
}
