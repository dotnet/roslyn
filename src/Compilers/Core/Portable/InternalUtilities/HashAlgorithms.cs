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
        private static readonly MethodInfo s_ComputeHash_bytes_Method;
        private static readonly MethodInfo s_ComputeHash_bytesOffsetCount_Method;
        private static readonly MethodInfo s_ComputeHash_stream_Method;
        private static readonly MethodInfo s_TransformBlock_Method;
        private static readonly MethodInfo s_TransformFinalBlock_Method;
        private static readonly MethodInfo s_Hash_PropertyGetter;

        private readonly IDisposable _hashInstance;

        private const string MscorlibAssembly = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string HashingAssembly = "System.Security.Cryptography.Hashing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        private const string HashingAlgorithmsAssembly = "System.Security.Cryptography.Hashing.Algorithms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        static HashAlgorithm()
        {
            Type type = GetType("System.Security.Cryptography.HashAlgorithm", new[] { HashingAssembly, MscorlibAssembly });
            Debug.Assert(type != null, "Could not find HashingAlgorithm");
            if (type != null)
            {
                var methods = type.GetTypeInfo().GetDeclaredMethods("ComputeHash");

                // https://msdn.microsoft.com/en-us/library/s02tk69a(v=vs.110).aspx
                s_ComputeHash_bytes_Method = (from m in methods
                                 let ps = m.GetParameters()
                                 where ps.Length == 1 && ps[0].ParameterType == typeof(byte[])
                                 select m).Single();

                // https://msdn.microsoft.com/en-us/library/1e59xaaz(v=vs.110).aspx
                s_ComputeHash_bytesOffsetCount_Method = (from m in methods
                                            let ps = m.GetParameters()
                                            where ps.Length == 3 && ps[0].ParameterType == typeof(byte[]) && ps[1].ParameterType == typeof(int) && ps[2].ParameterType == typeof(int)
                                            select m).Single();

                // https://msdn.microsoft.com/en-us/library/xa627k19(v=vs.110).aspx
                s_ComputeHash_stream_Method = (from m in methods
                                  let ps = m.GetParameters()
                                  where ps.Length == 1 && ps[0].ParameterType == typeof(Stream)
                                  select m).Single();

                // https://msdn.microsoft.com/en-us/library/system.security.cryptography.hashalgorithm.transformblock(v=vs.110).aspx
                s_TransformBlock_Method = (from m in type.GetTypeInfo().GetDeclaredMethods("TransformBlock")
                                          let ps = m.GetParameters()
                                          where ps.Length == 5 && ps[0].ParameterType == typeof(byte[]) &&
                                                                  ps[1].ParameterType == typeof(int) &&
                                                                  ps[2].ParameterType == typeof(int) &&
                                                                  ps[3].ParameterType == typeof(byte[]) &&
                                                                  ps[4].ParameterType == typeof(int)
                                          select m).Single();

                // https://msdn.microsoft.com/en-us/library/system.security.cryptography.hashalgorithm.transformblock(v=vs.110).aspx
                s_TransformFinalBlock_Method = (from m in type.GetTypeInfo().GetDeclaredMethods("TransformFinalBlock")
                                          let ps = m.GetParameters()
                                          where ps.Length == 3 && ps[0].ParameterType == typeof(byte[]) &&
                                                                  ps[1].ParameterType == typeof(int) &&
                                                                  ps[2].ParameterType == typeof(int)
                                          select m).Single();

                // https://msdn.microsoft.com/en-us/library/system.security.cryptography.hashalgorithm.hash(v=vs.110).aspx
                s_Hash_PropertyGetter = type.GetTypeInfo().GetDeclaredProperty("Hash").GetMethod;
            }
        }

        protected static MethodInfo LoadAlgorithmCreate(string name)
        {
            Type t = GetType("System.Security.Cryptography." + name, new[] { HashingAlgorithmsAssembly, MscorlibAssembly });
            if (t != null)
            {
                return (from m in t.GetTypeInfo().GetDeclaredMethods("Create")
                        where m.IsStatic && m.GetParameters().Length == 0
                        select m).Single();
            }

            Debug.Assert(false, "Could not find algorithm " + name);
            return null;
        }

        private static Type GetType(string typeName, string[] assemblyNames)
        {
            foreach (string assemblyName in assemblyNames)
            {
                Type t;
                try
                {
                    t = Type.GetType(typeName + ", " + assemblyName, throwOnError: false);
                }
                catch
                {
                    t = null;
                }

                if (t != null && t.GetTypeInfo().IsPublic)
                {
                    return t;
                }
            }
            return null;
        }

        protected HashAlgorithm(IDisposable hashInstance)
        {
            _hashInstance = hashInstance;
        }

        public byte[] ComputeHash(byte[] bytes)
        {
            return (byte[])s_ComputeHash_bytes_Method.Invoke(_hashInstance, new object[] { bytes });
        }

        public byte[] ComputeHash(byte[] bytes, int offset, int count)
        {
            return (byte[])s_ComputeHash_bytesOffsetCount_Method.Invoke(_hashInstance, new object[] { bytes, offset, count });
        }

        public byte[] ComputeHash(Stream stream)
        {
            return (byte[])s_ComputeHash_stream_Method.Invoke(_hashInstance, new object[] { stream });
        }

        /// <summary>
        /// Invoke the underlying HashAlgorithm's TransformBlock operation on the provided data.
        /// </summary>
        public void TransformBlock(byte[] inputBuffer, int inputCount)
        {
            int inputOffset = 0;
            while (inputCount > 0)
            {
                int written = (int)s_TransformBlock_Method.Invoke(_hashInstance, new object[] { inputBuffer, inputOffset, inputCount, inputBuffer, inputOffset });
                Debug.Assert(inputCount == written); // does the TransformBlock method always consume the complete data given to it?
                inputCount -= written;
                inputOffset += written;
            }
        }

        public void TransformFinalBlock(byte[] inputBuffer, int inputCount)
        {
            s_TransformFinalBlock_Method.Invoke(_hashInstance, new object[] { inputBuffer, 0, inputCount });
        }

        public byte[] Hash => (byte[])s_Hash_PropertyGetter.Invoke(_hashInstance, new object[] { });

        public void Dispose()
        {
            _hashInstance.Dispose();
        }
    }

    internal sealed class SHA1CryptoServiceProvider : HashAlgorithm
    {
        private static readonly MethodInfo s_create = LoadAlgorithmCreate("SHA1");

        public SHA1CryptoServiceProvider()
            : base((IDisposable)s_create.Invoke(null, null))
        {
        }
    }

    internal sealed class SHA256CryptoServiceProvider : HashAlgorithm
    {
        private static readonly MethodInfo s_create = LoadAlgorithmCreate("SHA256");

        public SHA256CryptoServiceProvider()
            : base((IDisposable)s_create.Invoke(null, null))
        {
        }
    }

    internal sealed class SHA384CryptoServiceProvider : HashAlgorithm
    {
        private static readonly MethodInfo s_create = LoadAlgorithmCreate("SHA384");

        public SHA384CryptoServiceProvider()
            : base((IDisposable)s_create.Invoke(null, null))
        {
        }
    }

    internal sealed class SHA512CryptoServiceProvider : HashAlgorithm
    {
        private static readonly MethodInfo s_create = LoadAlgorithmCreate("SHA512");

        public SHA512CryptoServiceProvider()
            : base((IDisposable)s_create.Invoke(null, null))
        {
        }
    }

    internal sealed class MD5CryptoServiceProvider : HashAlgorithm
    {
        private static readonly MethodInfo s_create = LoadAlgorithmCreate("MD5");

        public MD5CryptoServiceProvider()
            : base((IDisposable)s_create.Invoke(null, null))
        {
        }
    }
}
