// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Interop;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal static class SigningTestHelpers
    {
        public static readonly StrongNameProvider DefaultDesktopStrongNameProvider =
            new DesktopStrongNameProvider(ImmutableArray<string>.Empty, new VirtualizedStrongNameFileSystem(Path.GetTempPath()));

        // these are virtual paths that don't exist on disk
        internal static readonly string KeyFileDirectory = ExecutionConditionUtil.IsWindows
            ? @"R:\__Test__\"
            : "/r/__Test__/";
        internal static readonly string KeyPairFile = KeyFileDirectory + @"KeyPair_" + Guid.NewGuid() + ".snk";
        internal static readonly string PublicKeyFile = KeyFileDirectory + @"PublicKey_" + Guid.NewGuid() + ".snk";

        internal static readonly string KeyPairFile2 = KeyFileDirectory + @"KeyPair2_" + Guid.NewGuid() + ".snk";
        internal static readonly string PublicKeyFile2 = KeyFileDirectory + @"PublicKey2_" + Guid.NewGuid() + ".snk";

        internal static readonly string MaxSizeKeyFile = KeyFileDirectory + @"MaxSizeKey_" + Guid.NewGuid() + ".snk";

        private static bool s_keyInstalled;
        internal const string TestContainerName = "RoslynTestContainer";

        internal static readonly ImmutableArray<byte> PublicKey = ImmutableArray.Create(TestResources.General.snPublicKey);

        internal static object s_keyInstalledLock = new object();

        /// <summary>
        /// Installs the keys used for testing into the machine cache on Windows.
        /// </summary>
        internal static unsafe void InstallKey()
        {
            if (ExecutionConditionUtil.IsWindows)
            {
                lock (s_keyInstalledLock)
                {
                    if (!s_keyInstalled)
                    {
                        InstallKey(TestResources.General.snKey, TestContainerName);
                        s_keyInstalled = true;
                    }
                }
            }
        }

        private static unsafe void InstallKey(byte[] keyBlob, string keyName)
        {
            try
            {
                IClrStrongName strongName = new DesktopStrongNameProvider().GetStrongNameInterface();

                //EDMAURER use marshal to be safe?
                fixed (byte* p = keyBlob)
                {
                    strongName.StrongNameKeyInstall(keyName, (IntPtr)p, keyBlob.Length);
                }
            }
            catch (COMException ex)
            {
                if (unchecked((uint)ex.ErrorCode) != 0x8009000F)
                    throw;
            }
        }

        internal sealed class VirtualizedStrongNameFileSystem : StrongNameFileSystem
        {
            internal VirtualizedStrongNameFileSystem(string tempPath = null) : base(tempPath)
            {

            }
            private static bool PathEquals(string left, string right)
            {
                return string.Equals(
                    FileUtilities.NormalizeAbsolutePath(left),
                    FileUtilities.NormalizeAbsolutePath(right),
                    StringComparison.OrdinalIgnoreCase);
            }

            internal override bool FileExists(string fullPath)
            {
                return PathEquals(fullPath, KeyPairFile) || PathEquals(fullPath, PublicKeyFile);
            }

            internal override byte[] ReadAllBytes(string fullPath)
            {
                if (PathEquals(fullPath, KeyPairFile))
                {
                    return TestResources.General.snKey;
                }
                else if (PathEquals(fullPath, PublicKeyFile))
                {
                    return TestResources.General.snPublicKey;
                }
                else if (PathEquals(fullPath, KeyPairFile2))
                {
                    return TestResources.General.snKey2;
                }
                else if (PathEquals(fullPath, PublicKeyFile2))
                {
                    return TestResources.General.snPublicKey2;
                }
                else if (PathEquals(fullPath, MaxSizeKeyFile))
                {
                    return TestResources.General.snMaxSizeKey;
                }

                throw new FileNotFoundException("File not found", fullPath);
            }
        }
    }
}
