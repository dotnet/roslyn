// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    [Export(typeof(IWaitIndicator))]
    internal class TestWaitIndicator : IWaitIndicator
    {
        public IWaitContext StartWait(string title, string message, bool allowCancel)
        {
            return new WaitContext();
        }

        public WaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<IWaitContext> action)
        {
            try
            {
                action(new WaitContext());
            }
            catch
            {
            }

            return WaitIndicatorResult.Completed;
        }

        private class WaitContext : IWaitContext
        {
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

            public CancellationToken CancellationToken
            {
                get
                {
                    return CancellationToken.None;
                }
            }

            public string Message
            {
                get
                {
                    return string.Empty;
                }

                set
                {
                }
            }

            public void UpdateProgress()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
