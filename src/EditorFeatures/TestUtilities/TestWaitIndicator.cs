// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using VisualStudioIndicator = Microsoft.VisualStudio.Language.Intellisense.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    [Export(typeof(IWaitIndicator))]
    [Export(typeof(VisualStudioIndicator.IWaitIndicator))]
    public sealed class TestWaitIndicator : IWaitIndicator, VisualStudioIndicator.IWaitIndicator
    {
        public static readonly TestWaitIndicator Default = new TestWaitIndicator();

        private readonly IWaitContext _waitContext;
        private readonly Microsoft.VisualStudio.Language.Intellisense.Utilities.IWaitContext _platformWaitContext = new UncancellableWaitContext();

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public TestWaitIndicator()
            => _waitContext = new UncancellableWaitContext();

        IWaitContext IWaitIndicator.StartWait(string title, string message, bool allowCancel, bool showProgress)
            => _waitContext;

        WaitIndicatorResult IWaitIndicator.Wait(string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action)
        {
            try
            {
                action(_waitContext);
            }
            catch (OperationCanceledException)
            {
                return WaitIndicatorResult.Canceled;
            }

            return WaitIndicatorResult.Completed;
        }

        VisualStudioIndicator.IWaitContext VisualStudioIndicator.IWaitIndicator.StartWait(string title, string message, bool allowCancel)
            => _platformWaitContext;

        VisualStudioIndicator.WaitIndicatorResult VisualStudioIndicator.IWaitIndicator.Wait(string title, string message, bool allowCancel, Action<VisualStudioIndicator.IWaitContext> action)
        {
            try
            {
                action(_platformWaitContext);
            }
            catch (OperationCanceledException)
            {
                return VisualStudioIndicator.WaitIndicatorResult.Canceled;
            }

            return VisualStudioIndicator.WaitIndicatorResult.Completed;
        }

        private sealed class UncancellableWaitContext : IWaitContext, VisualStudioIndicator.IWaitContext
        {
            public CancellationToken CancellationToken
            {
                get { return CancellationToken.None; }
            }

            public IProgressTracker ProgressTracker { get; } = new ProgressTracker();

            public void UpdateProgress()
            {
            }

            public bool AllowCancel
            {
                get
                {
                    return false;
                }

                set
                {
                }
            }

            public string Message
            {
                get
                {
                    return "";
                }

                set
                {
                }
            }

            public void Dispose()
            {
            }
        }
    }
}
