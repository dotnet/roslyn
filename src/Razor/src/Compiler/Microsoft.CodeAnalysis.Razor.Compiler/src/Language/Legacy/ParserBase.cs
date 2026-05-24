// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal abstract class ParserBase(ParserContext context)
{
    public ParserContext Context { get; } = context;

    protected CancellationToken CancellationToken => Context.CancellationToken;
}
