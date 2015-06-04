// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using System;
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
            public StrongNameProviderWithBadInputStream(StrongNameProvider underlyingProvider)
            {
                _underlyingProvider = underlyingProvider;
            }

            public override bool Equals(object other) => this == other;

            public override int GetHashCode() => _underlyingProvider.GetHashCode();

            internal override Stream CreateInputStream()
            {
                throw new IOException("This is a test IOException");
            }

            internal override StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider) =>
                _underlyingProvider.CreateKeys(keyFilePath, keyContainerName, messageProvider);

            internal override void SignAssembly(StrongNameKeys keys, Stream inputStream, Stream outputStream) =>
                _underlyingProvider.SignAssembly(keys, inputStream, outputStream);
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

            comp.VerifyEmitDiagnostics(
    // error CS7028: Error signing output with public key from container 'RoslynTestContainer' -- This is a test IOException
    Diagnostic(ErrorCode.ERR_PublicKeyContainerFailure).WithArguments("RoslynTestContainer", "This is a test IOException").WithLocation(1, 1));
        }
    }
}
