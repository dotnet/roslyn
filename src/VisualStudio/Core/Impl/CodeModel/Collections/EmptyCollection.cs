// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;

[ComVisible(true)]
[ComDefaultInterface(typeof(ICodeElements))]
public sealed class EmptyCollection : AbstractCodeElementCollection
{
    private static readonly Snapshot s_snapshot = new CodeElementSnapshot([]);

    internal static EnvDTE.CodeElements Create(
        CodeModelState state,
        object parent)
    {
        var collection = new EmptyCollection(state, parent);
        return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
    }

    private EmptyCollection(
        CodeModelState state,
        object parent)
        : base(state, parent)
    {
    }

    internal override Snapshot CreateSnapshot()
        => s_snapshot;

    protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
    {
        element = null;
        return false;
    }

    protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
    {
        element = null;
        return false;
    }

    public override int Count
    {
        get { return 0; }
    }
}
