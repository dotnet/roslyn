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
