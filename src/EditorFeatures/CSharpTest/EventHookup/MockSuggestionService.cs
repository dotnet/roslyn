// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = Microsoft.VisualStudio.Threading.IAsyncDisposable;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup;

[Export(typeof(SuggestionServiceBase)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class MockSuggestionService() : SuggestionServiceBase
{
    public bool WasDismissAndBlockCalled { get; private set; }

    public override async Task<IAsyncDisposable> DismissAndBlockProposalsAsync(ITextView textView, ReasonForDismiss reason, CancellationToken cancellationToken)
    {
        WasDismissAndBlockCalled = true;
        return new AsyncDisposableStub();
    }

    public override Task<SuggestionManagerBase?> TryRegisterProviderAsync(SuggestionProviderBase provider, ITextView view, string name, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private sealed class AsyncDisposableStub : IAsyncDisposable
    {
        Task IAsyncDisposable.DisposeAsync() => Task.CompletedTask;
    }
}
