// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Roslyn.Utilities
{
    // TODO (DevDiv workitem 966425): replace with Portable profile APIs when available.

    internal abstract class HashAlgorithm : IDisposable
    {
        private readonly IDisposable _hashInstance;

        protected HashAlgorithm(IDisposable hashInstance)
        {
            _hashInstance = hashInstance;
        }

        public byte[] ComputeHash(byte[] buffer)
        {
            return PortableShim.HashAlgorithm.ComputeHash(_hashInstance, buffer);
        }

        public byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            return PortableShim.HashAlgorithm.ComputeHash(_hashInstance, buffer, offset, count);
        }

        public byte[] ComputeHash(Stream inputStream)
        {
            return PortableShim.HashAlgorithm.ComputeHash(_hashInstance, inputStream);
        }

        public void Dispose()
        {
            _hashInstance.Dispose();
        }
    }

    internal sealed class SHA1CryptoServiceProvider : HashAlgorithm
    {
        public SHA1CryptoServiceProvider()
            : base(PortableShim.SHA1.Create())
        {
        }
    }

    internal sealed class SHA256CryptoServiceProvider : HashAlgorithm
    {
        public SHA256CryptoServiceProvider()
            : base(PortableShim.SHA256.Create())
        {
        }
    }

    internal sealed class SHA384CryptoServiceProvider : HashAlgorithm
    {
        public SHA384CryptoServiceProvider()
            : base(PortableShim.SHA384.Create())
        {
        }
    }

    internal sealed class SHA512CryptoServiceProvider : HashAlgorithm
    {
        public SHA512CryptoServiceProvider()
            : base(PortableShim.SHA512.Create())
        {
        }
    }

    internal sealed class MD5CryptoServiceProvider : HashAlgorithm
    {
        public MD5CryptoServiceProvider()
            : base(PortableShim.MD5.Create())
        {
        }
    }
}
