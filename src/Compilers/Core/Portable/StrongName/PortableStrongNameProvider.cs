// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class PortableStrongNameProvider : StrongNameProvider
    {
        private readonly DesktopStrongNameProvider _provider;

        public PortableStrongNameProvider(ImmutableArray<string> keySearchPaths, StrongNameFileSystem strongNameFileSystem, string tempPath)
        {
            _provider = new DesktopStrongNameProvider(keySearchPaths, tempPath, strongNameFileSystem);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        internal override StrongNameFileSystem FileSystem => _provider.FileSystem;

        internal override StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider)
        {
            return _provider.CreateKeys(keyFilePath, keyContainerName, messageProvider);
        }

        internal override void SignPeBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privateKey)
        {
            _provider.SignPeBuilder(peBuilder, peBlob, privateKey);
        }

        internal override Stream CreateInputStream()
        {
            return _provider.CreateInputStream();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PortableStrongNameProvider other))
            {
                return false;
            }

            return FileSystem == other.FileSystem;
        }
    }
}
