// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig
{
    internal interface IEditorConfigOptionsCollection
    {
        ImmutableArray<(string feature, ImmutableArray<IOption2> options)> GetEditorConfigOptions();
    }
}
