// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Roslyn.Utilities
{
    // TODO (DevDiv workitem 966425): replace with Portable profile APIs when available.

    internal abstract class HashAlgorithm : IDisposable
    {
        private static readonly MethodInfo s_transformBlock = PortableShim.HashAlgorithm.Type
            .GetTypeInfo()
            .GetDeclaredMethod(nameof(TransformBlock), new[] { typeof(byte[]), typeof(int), typeof(int), typeof(byte[]), typeof(int) });

        private static readonly MethodInfo s_transformFinalBlock = PortableShim.HashAlgorithm.Type
            .GetTypeInfo()
            .GetDeclaredMethod(nameof(TransformFinalBlock), new[] { typeof(byte[]), typeof(int), typeof(int) });

        private static readonly PropertyInfo s_hash = PortableShim.HashAlgorithm.Type
            .GetTypeInfo()
            .GetDeclaredProperty(nameof(Hash));

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

        public bool SupportsTransform =>
            s_transformBlock != null &&
            s_transformFinalBlock != null &&
            s_hash != null;

        /// <summary>
        /// Invoke the underlying HashAlgorithm's TransformBlock operation on the provided data.
        /// </summary>
        public void TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            while (inputCount > 0)
            {
                int written = (int)s_transformBlock.Invoke(_hashInstance, new object[] { inputBuffer, inputOffset, inputCount, inputBuffer, inputOffset });
                Debug.Assert(inputCount == written); // does the TransformBlock method always consume the complete data given to it?
                inputCount -= written;
                inputOffset += written;
            }
        }

        public void TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            s_transformFinalBlock.Invoke(_hashInstance, new object[] { inputBuffer, inputOffset, inputCount });
        }

        public byte[] Hash => (byte[])s_hash.GetMethod.Invoke(_hashInstance, new object[] { });

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
