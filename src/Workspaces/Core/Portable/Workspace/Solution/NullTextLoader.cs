// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="TextLoader"/> that does not load text.
/// </summary>
internal sealed class NullTextLoader : TextLoader
{
    public static readonly NullTextLoader Default = new();

    public NullTextLoader()
    {
    }

    public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
