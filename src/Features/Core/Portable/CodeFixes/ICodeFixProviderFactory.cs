// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// CodeFixProvider factory. if an analyzer reference implements this, we call this to get CodeFixProviders
/// </summary>
internal interface ICodeFixProviderFactory
{
    ImmutableArray<CodeFixProvider> GetFixers();
}
