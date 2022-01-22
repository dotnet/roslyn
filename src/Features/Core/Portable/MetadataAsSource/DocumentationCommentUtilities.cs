// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal static class DocumentationCommentUtilities
    {
        private static readonly ObjectPool<List<string>> s_pool = SharedPools.Default<List<string>>();

        public static string ExtractXMLFragment(string input, string docCommentPrefix)
        {
            using var reader = new StringReader(input);
            using var list = s_pool.GetPooledObject();
            while (reader.ReadLine() is string str)
            {
                str = str.TrimStart();
                if (str.StartsWith(docCommentPrefix, StringComparison.Ordinal))
                {
                    str = str[docCommentPrefix.Length..];
                }

                list.Object.Add(str);
            }

            return string.Join("\r\n", list.Object);
        }
    }
}
