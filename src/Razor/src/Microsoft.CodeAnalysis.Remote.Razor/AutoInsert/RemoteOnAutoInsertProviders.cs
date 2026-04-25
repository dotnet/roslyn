// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.AutoInsert;

namespace Microsoft.CodeAnalysis.Remote.Razor.AutoInsert;

[Shared]
[Export(typeof(IOnAutoInsertProvider))]
internal sealed class RemoteAutoClosingTagOnAutoInsertProvider
    : AutoClosingTagOnAutoInsertProvider;

[Shared]
[Export(typeof(IOnAutoInsertProvider))]
internal sealed class RemoteCloseTextTagOnAutoInsertProvider
    : CloseTextTagOnAutoInsertProvider;
