// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <param name="IsStartTagContext">When true, the snippet was triggered inside a start tag (after &lt;)</param>
internal sealed record SnippetCompletionResolutionContext(bool IsStartTagContext = false) : ICompletionResolveContext;
