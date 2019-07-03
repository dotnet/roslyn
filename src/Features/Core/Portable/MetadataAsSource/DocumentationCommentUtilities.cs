// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal static class DocumentationCommentUtilities
    {
        private static readonly ObjectPool<List<string>> s_pool = new ObjectPool<List<string>>(() => new List<string>());

        public static string ExtractXMLFragment(string input, string docCommentPrefix)
        {
            using (var reader = new StringReader(input))
            using (var list = s_pool.GetPooledObject())
            {
                while (reader.ReadLine() is string str)
                {
                    if (str.StartsWith(docCommentPrefix, StringComparison.Ordinal))
                    {
                        str = str.Substring(docCommentPrefix.Length);
                    }

                    list.Object.Add(str);
                }

                return string.Join("\r\n", list.Object);
            }
        }
    }
}
