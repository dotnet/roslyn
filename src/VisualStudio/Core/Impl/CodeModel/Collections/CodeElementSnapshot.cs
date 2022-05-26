// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    internal class CodeElementSnapshot : Snapshot
    {
        private readonly ImmutableArray<EnvDTE.CodeElement> _elements;

        public CodeElementSnapshot(ICodeElements codeElements)
        {
            var count = codeElements.Count;
            var elementsBuilder = ArrayBuilder<EnvDTE.CodeElement>.GetInstance(count);

            for (var i = 0; i < count; i++)
            {
                // We use "i + 1" since CodeModel indices are 1-based
                if (ErrorHandler.Succeeded(codeElements.Item(i + 1, out var element)))
                {
                    elementsBuilder.Add(element);
                }
            }

            _elements = elementsBuilder.ToImmutableAndFree();
        }

        public CodeElementSnapshot(ImmutableArray<EnvDTE.CodeElement> elements)
            => _elements = elements;

        public override int Count
        {
            get { return _elements.Length; }
        }

        public override EnvDTE.CodeElement this[int index]
        {
            get
            {
                if (index < 0 || index >= _elements.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _elements[index];
            }
        }
    }
}
