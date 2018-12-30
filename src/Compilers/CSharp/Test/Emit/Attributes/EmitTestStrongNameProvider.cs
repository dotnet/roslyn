// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// The use of SigningTestHelpers makes this necessary for now. Fix is tracked by https://github.com/dotnet/roslyn/issues/25228
#if NET472
using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SigningTestHelpers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class InternalsVisibleToAndStrongNameTests : CSharpTestBase
    {
        private class TestDesktopStrongNameProvider : DesktopStrongNameProvider
        {
            private class TestStrongNameFileSystem : StrongNameFileSystem
            {
                private readonly Func<string, byte[]> _readAllBytes = null;

                internal TestStrongNameFileSystem(Func<string, byte[]> readAllBytes = null)
                {
                    _readAllBytes = readAllBytes;
                }

                internal override byte[] ReadAllBytes(string fullPath)
                {
                    if (_readAllBytes != null)
                    {
                        return _readAllBytes(fullPath);
                    }
                    else
                    {
                        return base.ReadAllBytes(fullPath);
                    }
                }
            }

            internal delegate void ReadKeysFromContainerDelegate(
                string keyContainer,
                out ImmutableArray<byte> publicKey);

            private readonly ReadKeysFromContainerDelegate m_readKeysFromContainer;

            public TestDesktopStrongNameProvider(
                Func<string, byte[]> readAllBytes = null,
                ReadKeysFromContainerDelegate readKeysFromContainer = null) : base(ImmutableArray<string>.Empty, new TestStrongNameFileSystem(readAllBytes))
            {
                m_readKeysFromContainer = readKeysFromContainer;
            }

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

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30152")]
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

            var comp = CreateCompilation(src, options: options);
            comp.VerifyEmitDiagnostics(
                // error CS7027: Error signing output with public key from file '{0}' -- '{1}'
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(keyFile, ex.Message).WithLocation(1, 1));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30152")]
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

            var comp = CreateCompilation(src, options: options);
            comp.VerifyEmitDiagnostics(
                // error CS7028: Error signing output with public key from container 'RoslynTestContainer' -- Crazy exception you could never have predicted!
                Diagnostic(ErrorCode.ERR_PublicKeyContainerFailure).WithArguments("RoslynTestContainer", ex.Message).WithLocation(1, 1));
        }
    }
}
#endif
