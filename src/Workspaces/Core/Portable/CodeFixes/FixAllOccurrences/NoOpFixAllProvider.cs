// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// A dummy fix all provider to represent a no-change provider.
/// This is only used by public constructors for <see cref="FixAllContext"/>,
/// our internal code fix engine always creates a FixAllContext with a non-null
/// FixAllProvider. Using a <see cref="NoOpFixAllProvider"/> for the public constructors
/// helps us to avoid a nullable <see cref="FixAllContext.FixAllProvider"/>.
/// </summary>
internal sealed class NoOpFixAllProvider : FixAllProvider
{
    public static readonly NoOpFixAllProvider Instance = new();

    private NoOpFixAllProvider()
    {
    }

    public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        => null;
}
