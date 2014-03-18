// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.UnitTests
{
    /// <summary>
    /// Encapsulates assembly portability information inside an opaque class for use
    /// by AssemblyIdentity in comparing assembly identities.
    /// </summary>
    internal sealed class FusionAssemblyPortabilityPolicy : IDisposable
    {
        // Pointer to unmanaged small CLR struct that must be allocated and deallocated by
        // CLR calls
        private IntPtr assemblyConfigCookie;
        private readonly byte[] fileHash;
        
        private FusionAssemblyPortabilityPolicy(IntPtr asmConfigCookie, byte[] fileHash)
        {
            this.assemblyConfigCookie = asmConfigCookie;
            this.fileHash = fileHash;
        }

        /// <summary>
        /// Loads the assembly portability policy from the given path using the CLR API. 
        /// If any problems are encountered by the CLR, the errors are passed through via CLR exception.
        /// Can throw IO exceptions if any are encountered during file access.
        /// </summary>
        /// <param name="appConfigPath">Absolute path to the config file.</param>
        /// <returns></returns>
        public static FusionAssemblyPortabilityPolicy LoadFromFile(string appConfigPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(appConfigPath));

            IntPtr asmConfigCookie;
            // May throw CLR exception
            CreateAssemblyConfigCookie(appConfigPath, out asmConfigCookie);

            var hash = (new SHA1CryptoServiceProvider()).ComputeHash(File.ReadAllBytes(appConfigPath));
            return new FusionAssemblyPortabilityPolicy(asmConfigCookie, hash);
        }

        internal IntPtr ConfigCookie
        {
            get { return assemblyConfigCookie; }
        }

        [DllImport("clr", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void CreateAssemblyConfigCookie([MarshalAs(UnmanagedType.LPWStr)] string configPath, out IntPtr assemblyConfig);

        [DllImport("clr", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int DestroyAssemblyConfigCookie(IntPtr assemblyConfig);

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        private void DisposeInternal()
        {
            IntPtr ptr = Interlocked.Exchange(ref assemblyConfigCookie, IntPtr.Zero);
            if (ptr != IntPtr.Zero)
            {
                DestroyAssemblyConfigCookie(ptr);
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FusionAssemblyPortabilityPolicy);
        }

        public bool Equals(FusionAssemblyPortabilityPolicy other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // This is not foolproof, but we just assume that if both assembly
            // policies have the same path and the same timestamp that they are the same.
            // We can't do any better because we don't have access to the config cookie internals.
            return (object)other != null &&
                   Enumerable.SequenceEqual(this.fileHash, other.fileHash);
        }

        public override int GetHashCode()
        {
            // Modified FNV hash
            return Hash.GetFNVHashCode(this.fileHash);
        }

        ~FusionAssemblyPortabilityPolicy()
        {
            DisposeInternal();
        }
    }
}
