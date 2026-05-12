// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class TestDesktopStrongNameProvider : DesktopStrongNameProvider
    {
        internal Func<string?, string?, CommonMessageProvider, StrongNameKeys>? CreateKeysFunc { get; set; }
        internal Action<ExtendedPEBuilder, BlobBuilder, RSAParameters>? SignBuilderFunc { get; set; }
        internal Action<StrongNameKeys, string>? SignFileFunc { get; set; }

        public TestDesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths = default, StrongNameFileSystem? fileSystem = null)
            : base(keyFileSearchPaths, fileSystem ?? StrongNameFileSystem.Instance)
        {
            SignBuilderFunc = base.SignBuilder;
            SignFileFunc = base.SignFile;
        }

        internal override StrongNameKeys CreateKeys(string? keyFilePath, string? keyContainerName, CommonMessageProvider messageProvider)
            => CreateKeysFunc != null
                ? CreateKeysFunc(keyFilePath, keyContainerName, messageProvider)
                : base.CreateKeys(keyFilePath, keyContainerName, messageProvider);

        internal override void SignFile(StrongNameKeys keys, string filePath) => SignFileFunc!(keys, filePath);

        internal override void SignBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privateKey) => SignBuilderFunc!(peBuilder, peBlob, privateKey);
    }
}
