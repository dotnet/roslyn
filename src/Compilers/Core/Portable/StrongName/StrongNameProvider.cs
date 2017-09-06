// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis
{
    internal enum SigningCapability
    {
        SignsStream,
        SignsPeBuilder,
    }

    /// <summary>
    /// Provides strong name and signs source assemblies.
    /// </summary>
    public abstract class StrongNameProvider
    {
        protected StrongNameProvider()
        {
        }

        public abstract override int GetHashCode();
        public override abstract bool Equals(object other);

        internal abstract SigningCapability Capability { get; }

        /// <exception cref="IOException"></exception>
        internal abstract Stream CreateInputStream();

        internal abstract StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider);

        internal virtual void SignStream(StrongNameKeys keys, Stream inputStream, Stream outputStream)
        {
            throw new NotSupportedException();
        }

        internal virtual void SignPeBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privkey)
        {
            throw new NotSupportedException();
        }
    }
}
