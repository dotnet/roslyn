﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class InternalsVisibleToAndStrongNameTests : CSharpTestBase
    {
        /// <summary>
        /// A strong name provider which throws an IOException while creating
        /// the input stream.
        /// </summary>
        private class StrongNameProviderWithBadInputStream : StrongNameProvider
        {
            private StrongNameProvider _underlyingProvider;

            public Exception ThrownException;

            public StrongNameProviderWithBadInputStream(StrongNameProvider underlyingProvider)
            {
                _underlyingProvider = underlyingProvider;
            }

            public override bool Equals(object other) => this == other;

            public override int GetHashCode() => _underlyingProvider.GetHashCode();

            internal override Stream CreateInputStream()
            {
                ThrownException = new IOException("This is a test IOException");
                throw ThrownException;
            }

            internal override StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider) =>
                _underlyingProvider.CreateKeys(keyFilePath, keyContainerName, messageProvider);

            internal override void SignAssembly(StrongNameKeys keys, Stream inputStream, Stream outputStream) =>
                _underlyingProvider.SignAssembly(keys, inputStream, outputStream);
        }

        private class TestDesktopStrongNameProvider : DesktopStrongNameProvider
        {
            private readonly Func<string, byte[]> m_readAllBytes;

            internal delegate void ReadKeysFromContainerDelegate(
                string keyContainer,
                out ImmutableArray<byte> publicKey);

            private readonly ReadKeysFromContainerDelegate m_readKeysFromContainer;

            public TestDesktopStrongNameProvider(
                Func<string, byte[]> readAllBytes = null,
                ReadKeysFromContainerDelegate readKeysFromContainer = null)
            {
                m_readAllBytes = readAllBytes;
                m_readKeysFromContainer = readKeysFromContainer;
            }

            internal override byte[] ReadAllBytes(string fullPath) =>
                m_readAllBytes != null ? m_readAllBytes(fullPath)
                                       : base.ReadAllBytes(fullPath);

            internal override void ReadKeysFromContainer(string keyContainer, out ImmutableArray<byte> publicKey)
            {
                if (m_readKeysFromContainer != null)
                {
                    m_readKeysFromContainer(keyContainer, out publicKey);
                }
                else
                {
                    base.ReadKeysFromContainer(keyContainer, out publicKey);
                }
            }
        }

        [Fact]
        [WorkItem(209695, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=209694")]
        public void ExceptionInReadAllBytes()
        {
            var ex = new Exception("Crazy exception you could never have predicted!");
            var provider = new TestDesktopStrongNameProvider((_) =>
            {
                throw ex;
            });

            var src = @"class C {}";
            var keyFile = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey).Path;
            var options = TestOptions.DebugDll
                .WithStrongNameProvider(provider)
                .WithCryptoKeyFile(keyFile);

            var comp = CreateCompilationWithMscorlib(src, options: options);
            comp.VerifyEmitDiagnostics(
                // error CS7027: Error signing output with public key from file '{0}' -- '{1}'
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(keyFile, ex.Message).WithLocation(1, 1));
        }

        [Fact]
        public void ExceptionInReadKeysFromContainer()
        {
            var ex = new Exception("Crazy exception you could never have predicted!");
            var provider = new TestDesktopStrongNameProvider(readKeysFromContainer:
                (string _1, out ImmutableArray<byte> _2) =>
            {
                throw ex;
            });

            var src = @"class C {}";
            var options = TestOptions.DebugDll
                .WithStrongNameProvider(provider)
                .WithCryptoKeyContainer("RoslynTestContainer");

            var comp = CreateCompilationWithMscorlib(src, options: options);
            comp.VerifyEmitDiagnostics(
                // error CS7028: Error signing output with public key from container 'RoslynTestContainer' -- Crazy exception you could never have predicted!
                Diagnostic(ErrorCode.ERR_PublicKeyContainerFailure).WithArguments("RoslynTestContainer", ex.Message).WithLocation(1, 1));
        }

        [Fact]
        public void BadInputStream()
        {
            string src = @"
class C
{
    public static void Main(string[] args) { }
}";
            var testProvider = new StrongNameProviderWithBadInputStream(s_defaultProvider);
            var options = TestOptions.DebugExe
                .WithStrongNameProvider(testProvider)
                .WithCryptoKeyContainer("RoslynTestContainer");

            var comp = CreateCompilationWithMscorlib(src,
                options: options);

            comp.Emit(new MemoryStream()).Diagnostics.Verify(
                // error CS8104: An error occurred while writing the Portable Executable file.
                Diagnostic(ErrorCode.ERR_PeWritingFailure).WithArguments(testProvider.ThrownException.ToString()).WithLocation(1, 1));
        }
    }
}
