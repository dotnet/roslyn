// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SourceFileCollection : List<(string filename, SourceText content)>
    {
        public void Add((string filename, string content) file)
        {
            Add((file.filename, SourceText.From(file.content)));
        }

        public void Add((Type sourceGeneratorType, string filename, string content) file)
        {
            var contentWithEncoding = SourceText.From(file.content, Encoding.UTF8);
            Add((file.sourceGeneratorType, file.filename, contentWithEncoding));
        }

        public void Add((Type sourceGeneratorType, string filename, SourceText content) file)
        {
            var generatedPath = Path.Combine(file.sourceGeneratorType.GetTypeInfo().Assembly.GetName().Name ?? string.Empty, file.sourceGeneratorType.FullName!, file.filename);
            Add((generatedPath, file.content));
        }
    }
}
