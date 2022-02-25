// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    internal partial class BackgroundWorkIndicatorFactory
    {
        /// <summary>
        /// Implementation of an <see cref="IUIThreadOperationScope"/> for the background work indicator. Allows for
        /// features to create nested work with descriptions/progress that will update the all-up indicator tool-tip
        /// shown to the user.
        /// </summary>
        private class BackgroundWorkIndicatorScope : IUIThreadOperationScope, IProgress<ProgressInfo>
        {
            private readonly BackgroundWorkIndicatorContext _indicator;

            private bool _allowCancellation;
            private string _description;
            private ProgressInfo _progressInfo;

            public IUIThreadOperationContext Context => _indicator;
            public IProgress<ProgressInfo> Progress => this;

            public BackgroundWorkIndicatorScope(
                BackgroundWorkIndicatorContext indicator, bool allowCancellation, string description)
            {
                _indicator = indicator;
                _allowCancellation = allowCancellation;
                _description = description;
            }

            public (bool allowCancellation, string description, ProgressInfo progressInfo) ReadData_MustBeCalledUnderLock()
            {
                Contract.ThrowIfFalse(Monitor.IsEntered(_indicator.Gate));
                return (_allowCancellation, _description, _progressInfo);
            }

            /// <summary>
            /// On disposal, just remove ourselves from our parent context.  It will update the UI accordingly.
            /// </summary>
            void IDisposable.Dispose()
                => _indicator.RemoveScope(this);

            bool IUIThreadOperationScope.AllowCancellation
            {
                get
                {
                    lock (_indicator.Gate)
                        return _allowCancellation;
                }
                set
                {
                    lock (_indicator.Gate)
                    {
                        _allowCancellation = value;
                    }

                    // We changed.  Enqueue work to make sure the UI reflects this.
                    _indicator.EnqueueUIUpdate();
                }
            }

            string IUIThreadOperationScope.Description
            {
                get
                {
                    lock (_indicator.Gate)
                        return _description;
                }
                set
                {
                    lock (_indicator.Gate)
                    {
                        _description = value;
                    }

                    // We changed.  Enqueue work to make sure the UI reflects this.
                    _indicator.EnqueueUIUpdate();
                }
            }

            void IProgress<ProgressInfo>.Report(ProgressInfo value)
            {
                lock (_indicator.Gate)
                {
                    _progressInfo = value;
                }

                // We changed.  Enqueue work to make sure the UI reflects this.
                _indicator.EnqueueUIUpdate();
            }
        }
    }
}
