// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <param name="IsStartTagContext">When true, the snippet was triggered inside a start tag (after &lt;)
/// and resolve should strip the leading &lt; from the snippet body to avoid duplication.</param>
internal sealed record SnippetCompletionResolutionContext(bool IsStartTagContext = false) : ICompletionResolveContext;
