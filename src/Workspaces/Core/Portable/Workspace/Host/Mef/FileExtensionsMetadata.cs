// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used to find exports declared for a specific file extensions.
    /// </summary>
    internal class FileExtensionsMetadata
    {
        public IEnumerable<string> Extensions { get; }

        public FileExtensionsMetadata(IDictionary<string, object> data)
        {
            this.Extensions = ((IReadOnlyDictionary<string, object>)data).GetEnumerableMetadata<string>("Extensions");
        }

        public FileExtensionsMetadata(params string[] extensions)
        {
            if (extensions?.Length == 0)
            {
                throw new ArgumentException(nameof(extensions));
            }

            this.Extensions = extensions;
        }
    }
}
