// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;

[ComVisible(true)]
[ComDefaultInterface(typeof(ICodeElements))]
public sealed class UnionCollection : AbstractCodeElementCollection
{
    internal static EnvDTE.CodeElements Create(
        CodeModelState state,
        AbstractCodeElement parent,
        params ICodeElements[] collections)
    {
        var collection = new UnionCollection(state, parent, collections);
        return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
    }

    private readonly ICodeElements[] _collections;

    private UnionCollection(
        CodeModelState state,
        AbstractCodeElement parent,
        ICodeElements[] collections)
        : base(state, parent)
    {
        _collections = collections;
    }

    protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
    {
        var currentIndex = 0;

        foreach (var collection in _collections)
        {
            var count = collection.Count;
            if (index < currentIndex + count)
            {
                // Note: We use index + 1 because CodeModel expects 1-based indices
                return ErrorHandler.Succeeded(collection.Item(index - currentIndex + 1, out element));
            }

            currentIndex += count;
        }

        element = null;
        return false;
    }

    protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
    {
        foreach (var collection in _collections)
        {
            if (ErrorHandler.Succeeded(collection.Item(name, out element)))
            {
                return true;
            }
        }

        element = null;
        return false;
    }

    public override int Count
    {
        get { return _collections.Sum(c => c.Count); }
    }
}
