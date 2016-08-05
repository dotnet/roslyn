// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignTool
{
    internal sealed class FileSignInfo
    {
        /// <summary>
        /// The binary to be signed.
        /// </summary>
        internal FileName FileName { get; }

        internal SignInfo FileSignData { get; }

        /// <summary>
        /// The authenticode certificate which should be used to sign the binary.
        /// </summary>
        internal string Certificate => FileSignData.Certificate;

        /// <summary>
        /// This will be null in the case a strong name signing is not required.
        /// </summary>
        internal string StrongName => FileSignData.StrongName;

        internal FileSignInfo(FileName name, SignInfo fileSignData)
        {
            Debug.Assert(name.IsAssembly || fileSignData.StrongName == null);

            FileName = name;
            FileSignData = fileSignData;
        }
    }
}
