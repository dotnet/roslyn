// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Linq;
using System;

namespace Roslyn.Utilities
{
    // TODO (DevDiv workitem 966425): replace with Portable profile APIs when available.

    internal abstract class HashAlgorithm : IDisposable
    {
        private static readonly MethodInfo bytesMethod;
        private static readonly MethodInfo streamMethod;

        private readonly IDisposable hashInstance;

        static HashAlgorithm()
        {
            var type = Type.GetType("System.Security.Cryptography.HashAlgorithm, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var methods = type.GetTypeInfo().GetDeclaredMethods("ComputeHash");

            bytesMethod = (from m in methods
                           let ps = m.GetParameters()
                           where ps.Length == 1 && ps[0].ParameterType == typeof(byte[])
                           select m).Single();

            streamMethod = (from m in methods
                            let ps = m.GetParameters()
                            where ps.Length == 1 && ps[0].ParameterType == typeof(Stream)
                            select m).Single();
        }

        protected static Type LoadAlgorithm(string name)
        {
            const string mscorlib = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            var type = Type.GetType("System.Security.Cryptography." + name + "CryptoServiceProvider, " + mscorlib, throwOnError: false);

            if (type != null && type.GetTypeInfo().IsPublic)
            {
                return type;
            }

            return Type.GetType("System.Security.Cryptography." + name + "Managed, " + mscorlib, throwOnError: false);
        }

        protected HashAlgorithm(IDisposable hashInstance)
        {
            this.hashInstance = hashInstance;
        }

        public byte[] ComputeHash(byte[] bytes)
        {
            return (byte[])bytesMethod.Invoke(hashInstance, new object[] { bytes });
        }

        public byte[] ComputeHash(Stream stream)
        {
            return (byte[])streamMethod.Invoke(hashInstance, new object[] { stream });
        }

        public void Dispose()
        {
            hashInstance.Dispose();
        }
    }

    internal sealed class SHA1CryptoServiceProvider : HashAlgorithm
    {
        private static Type type = LoadAlgorithm("SHA1");

        public SHA1CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(type))
        {
        }
    }

    internal sealed class SHA256CryptoServiceProvider : HashAlgorithm
    {
        private static Type type = LoadAlgorithm("SHA256");

        public SHA256CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(type))
        {
        }
    }

    internal sealed class SHA384CryptoServiceProvider : HashAlgorithm
    {
        private static Type type = LoadAlgorithm("SHA384");

        public SHA384CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(type))
        {
        }
    }

    internal sealed class SHA512CryptoServiceProvider : HashAlgorithm
    {
        private static Type type = LoadAlgorithm("SHA512");

        public SHA512CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(type))
        {
        }
    }

    internal sealed class MD5CryptoServiceProvider : HashAlgorithm
    {
        private static Type type = LoadAlgorithm("MD5");

        public MD5CryptoServiceProvider()
            : base((IDisposable)Activator.CreateInstance(type))
        {
        }
    }
}
