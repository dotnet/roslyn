// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    internal sealed class InMemoryLsifJsonWriter : ILsifJsonWriter
    {
        private readonly List<Element> _elements = new List<Element>();

        public void Write(Element element)
        {
            _elements.Add(element);
        }

        public void CopyToAndEmpty(ILsifJsonWriter writer)
        {
            foreach (var element in _elements)
            {
                writer.Write(element);
            }

            _elements.Clear();
        }
    }
}
