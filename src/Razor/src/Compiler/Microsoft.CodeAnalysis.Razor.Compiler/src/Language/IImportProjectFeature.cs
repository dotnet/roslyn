// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal interface IImportProjectFeature : IRazorProjectEngineFeature
{
    void CollectImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> imports);
}
