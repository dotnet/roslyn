// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.FoldingRanges;

namespace Microsoft.CodeAnalysis.Remote.Razor.FoldingRanges;

[Shared]
[Export(typeof(IRazorFoldingRangeProvider))]
internal sealed class RemoteRazorCodeBlockFoldingProvider : RazorCodeBlockFoldingProvider;

[Shared]
[Export(typeof(IRazorFoldingRangeProvider))]
internal sealed class RemoteRazorCSharpStatementFoldingProvider : RazorCSharpStatementFoldingProvider;

[Shared]
[Export(typeof(IRazorFoldingRangeProvider))]
internal sealed class RemoteRazorCSharpStatementKeywordFoldingProvider : RazorCSharpStatementKeywordFoldingProvider;

[Shared]
[Export(typeof(IRazorFoldingRangeProvider))]
internal sealed class RemoteSectionDirectiveFoldingProvider : SectionDirectiveFoldingProvider;

[Shared]
[Export(typeof(IRazorFoldingRangeProvider))]
internal sealed class RemoteUsingsFoldingRangeProvider : UsingsFoldingRangeProvider;
