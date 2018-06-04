// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.	
	
using System;	
using System.ComponentModel.Composition;	
using System.Threading;	
using Microsoft.CodeAnalysis.Editor.Host;	
using Microsoft.CodeAnalysis.Shared.Utilities;	
using VisualStudioIndicator = Microsoft.VisualStudio.Language.Intellisense.Utilities;	
	
namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{	
    [Export(typeof(IWaitIndicator))]
    public sealed class TestWaitIndicator : IWaitIndicator
    {	
        public static readonly TestWaitIndicator Default = new TestWaitIndicator();	
	
        private readonly IWaitContext _waitContext;	
        private readonly Microsoft.VisualStudio.Language.Intellisense.Utilities.IWaitContext _platformWaitContext = new UncancellableWaitContext();	
	
        public TestWaitIndicator()
            : this(new UncancellableWaitContext())	
        {	
        }	
	
        internal TestWaitIndicator(IWaitContext waitContext)
        {	
            _waitContext = waitContext;	
        }	
	
        IWaitContext IWaitIndicator.StartWait(string title, string message, bool allowCancel, bool showProgress)
        {	
            return _waitContext;	
        }

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
