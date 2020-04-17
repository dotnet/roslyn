// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    internal sealed class InMemoryLsifJsonWriter : ILsifJsonWriter
    {
        private readonly object _gate = new object();
        private List<Element> _elements = new List<Element>();

        public void Write(Element element)
        {
            lock (_gate)
            {
                _elements.Add(element);
            }
        }

        public void CopyToAndEmpty(ILsifJsonWriter writer)
        {
            List<Element> localElements;

            lock (_gate)
            {
                localElements = _elements;
                _elements = new List<Element>();
            }

            foreach (var element in localElements)
            {
                writer.Write(element);
            }
        }
    }
}
