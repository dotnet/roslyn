// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(CompletionListCache))]
internal class CohostCompletionListCache : CompletionListCache;
