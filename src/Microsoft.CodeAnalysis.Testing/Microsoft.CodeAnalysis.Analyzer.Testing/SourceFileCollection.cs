// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SourceFileCollection : List<(string filename, SourceText content)>
    {
        public void Add((string filename, string content) file)
        {
            Add((file.filename, SourceText.From(file.content)));
        }
    }
}
