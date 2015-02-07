// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Roslyn.Utilities
{
    // TODO (DevDiv workitem 966425): replace with Portable profile APIs when available.

    internal abstract class HashAlgorithm : IDisposable
    {
        private static readonly MethodInfo s_bytesMethod;
        private static readonly MethodInfo s_bytesOffsetCountMethod;
        private static readonly MethodInfo s_streamMethod;

        private readonly IDisposable _hashInstance;

        static HashAlgorithm()
        {
            var type = Type.GetType("System.Security.Cryptography.HashAlgorithm, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var methods = type.GetTypeInfo().GetDeclaredMethods("ComputeHash");

            s_bytesMethod = (from m in methods
                             let ps = m.GetParameters()
                             where ps.Length == 1 && ps[0].ParameterType == typeof(byte[])
                             select m).Single();

            s_bytesOffsetCountMethod = (from m in methods
                             let ps = m.GetParameters()
                             where ps.Length == 3 && ps[0].ParameterType == typeof(byte[]) && ps[1].ParameterType == typeof(int) && ps[2].ParameterType == typeof(int)
                             select m).Single();

            s_streamMethod = (from m in methods
                              let ps = m.GetParameters()
                              where ps.Length == 1 && ps[0].ParameterType == typeof(Stream)
                              select m).Single();
        }

        protected static Type LoadAlgorithm(string name)
        {
            const string Mscorlib = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            var type = Type.GetType("System.Security.Cryptography." + name + "CryptoServiceProvider, " + Mscorlib, throwOnError: false);

            if (type != null && type.GetTypeInfo().IsPublic)
            {
                return type;
            }

            return Type.GetType("System.Security.Cryptography." + name + "Managed, " + Mscorlib, throwOnError: false);
        }

        protected HashAlgorithm(IDisposable hashInstance)
        {
            _hashInstance = hashInstance;
        }

        public byte[] ComputeHash(byte[] bytes)
        {
            return (byte[])s_bytesMethod.Invoke(_hashInstance, new object[] { bytes });
        }

        public byte[] ComputeHash(byte[] bytes, int offset, int count)
        {
            return (byte[])s_bytesOffsetCountMethod.Invoke(_hashInstance, new object[] { bytes, offset, count });
        }

        public byte[] ComputeHash(Stream stream)
        {
            return (byte[])s_streamMethod.Invoke(_hashInstance, new object[] { stream });
        }

        public void Dispose()
        {
            _hashInstance.Dispose();
        }
    }

    internal sealed class SHA1CryptoServiceProvider : HashAlgorithm
    {
        private static Type s_type = LoadAlgorithm("SHA1");

        public SHA1CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(s_type))
        {
        }
    }

    internal sealed class SHA256CryptoServiceProvider : HashAlgorithm
    {
        private static Type s_type = LoadAlgorithm("SHA256");

        public SHA256CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(s_type))
        {
        }
    }

    internal sealed class SHA384CryptoServiceProvider : HashAlgorithm
    {
        private static Type s_type = LoadAlgorithm("SHA384");

        public SHA384CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(s_type))
        {
        }
    }

    internal sealed class SHA512CryptoServiceProvider : HashAlgorithm
    {
        private static Type s_type = LoadAlgorithm("SHA512");

        public SHA512CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(s_type))
        {
        }
    }

    internal sealed class MD5CryptoServiceProvider : HashAlgorithm
    {
        private static Type s_type = LoadAlgorithm("MD5");

        public MD5CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(s_type))
        {
        }
    }
}
