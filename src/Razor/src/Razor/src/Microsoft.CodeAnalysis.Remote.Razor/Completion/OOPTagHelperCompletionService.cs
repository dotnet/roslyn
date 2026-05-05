// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

[Export(typeof(ITagHelperCompletionService)), Shared]
internal sealed class OOPTagHelperCompletionService : TagHelperCompletionService
{
}
