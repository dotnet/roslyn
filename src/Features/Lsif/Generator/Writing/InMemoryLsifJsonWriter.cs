// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;

namespace Microsoft.CodeAnalysis.Lsif.Generator.Writing
{
    internal sealed class InMemoryLsifJsonWriter : ILsifJsonWriter
    {
        private readonly List<Element> _elements = new List<Element>();

        public void Write(Element element)
        {
            _elements.Add(element);
        }

        public void CopyTo(ILsifJsonWriter writer)
        {
            foreach (var element in _elements)
            {
                writer.Write(element);
            }
        }
    }
}
