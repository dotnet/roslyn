// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options;

internal interface IEditorConfigOptionsEnumerator
{
    /// <summary>
    /// Returns all editorconfig options defined by the implementing language, grouped by feature.
    /// </summary>
    /// <param name="includeUnsupported">True to include undocumented options that the user can set in editorconfig file but we provide no support for them.</param>
    public abstract IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetOptions(bool includeUnsupported);
}
