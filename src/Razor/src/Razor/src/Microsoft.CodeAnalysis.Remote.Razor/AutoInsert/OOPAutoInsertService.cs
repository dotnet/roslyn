// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.AutoInsert;

namespace Microsoft.CodeAnalysis.Remote.Razor.AutoInsert;

[Export(typeof(IAutoInsertService)), Shared]
[method: ImportingConstructor]
internal sealed class OOPAutoInsertService([ImportMany] IEnumerable<IOnAutoInsertProvider> providers) : AutoInsertService(providers)
{
}
