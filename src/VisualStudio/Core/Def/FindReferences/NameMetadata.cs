// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal sealed class NameMetadata
{
    public string? Name { get; }

    public NameMetadata(IDictionary<string, object> data)
        => this.Name = (string?)data.GetValueOrDefault(nameof(Name));
}
