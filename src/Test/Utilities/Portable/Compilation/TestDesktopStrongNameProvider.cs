// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Interop;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class TestDesktopStrongNameProvider : DesktopStrongNameProvider
    {
        internal delegate void ReadKeysFromContainerDelegate(
            string keyContainer,
            out ImmutableArray<byte> publicKey);

        internal ReadKeysFromContainerDelegate ReadKeysFromContainerFunc { get; set; }
        internal Action<ExtendedPEBuilder, BlobBuilder, RSAParameters> SignBuilderFunc { get; set; }
        internal Action<StrongNameKeys, string> SignFileFunc { get; set; }
        internal Func<IClrStrongName> GetStrongNameInterfaceFunc { get; set; }

        public TestDesktopStrongNameProvider(ImmutableArray<string> keyFileSearchPaths = default, StrongNameFileSystem fileSystem = null)
            : base(keyFileSearchPaths, fileSystem)
        {
            ReadKeysFromContainerFunc = base.ReadKeysFromContainer;
            SignBuilderFunc = base.SignBuilder;
            SignFileFunc = base.SignFile;
            GetStrongNameInterfaceFunc = base.GetStrongNameInterface;
        }

        internal override void ReadKeysFromContainer(string keyContainer, out ImmutableArray<byte> publicKey) => ReadKeysFromContainerFunc(keyContainer, out publicKey);

        internal override void SignFile(StrongNameKeys keys, string filePath) => SignFileFunc(keys, filePath);

        internal override void SignBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privateKey) => SignBuilderFunc(peBuilder, peBlob, privateKey);

        internal override IClrStrongName GetStrongNameInterface() => GetStrongNameInterfaceFunc();
    }
}
