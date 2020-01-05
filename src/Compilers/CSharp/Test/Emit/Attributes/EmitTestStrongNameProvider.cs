// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SigningTestHelpers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class InternalsVisibleToAndStrongNameTests : CSharpTestBase
    {
        [Fact]
        [WorkItem(209695, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=209694")]
        public void ExceptionInReadAllBytes()
        {
            var ex = new Exception("Crazy exception you could never have predicted!");
            var fileSystem = new TestStrongNameFileSystem()
            {
                ReadAllBytesFunc = _ => throw ex
            };
            var provider = new TestDesktopStrongNameProvider(fileSystem: fileSystem);

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

        [Fact]
        public void ExceptionInReadKeysFromContainer()
        {
            var ex = new Exception("Crazy exception you could never have predicted!");
            var provider = new TestDesktopStrongNameProvider()
            {
                ReadKeysFromContainerFunc = (string _, out ImmutableArray<byte> publicKey) => throw ex
            };

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
