// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private partial class TagSource
        {
            /// <summary>How many taggers are currently using us.</summary>
            private int _taggers = 0;
            private bool _disposed = false;

            ~TagSource()
            {
                if (!Environment.HasShutdownStarted)
                {
#if DEBUG
                    Contract.Fail($@"Should have been disposed!
DataSource-StackTrace:
{_dataSource.StackTrace}

StackTrace:
{_stackTrace}");
#else
                    Contract.Fail($@"Should have been disposed! Try running in Debug to get the allocation callstack");
#endif
                }
            }

            public event EventHandler Disposed = (s, e) => { };

            private void Dispose()
            {
                if (_disposed)
                {
                    Debug.Fail("Tagger already disposed");
                    return;
                }

                _disposed = true;
                this.Disposed(this, EventArgs.Empty);
                GC.SuppressFinalize(this);

                this.Disconnect();
            }

            internal void OnTaggerAdded(Tagger tagger)
            {
                // this should be only called from UI thread. 
                // in unit test, must be called from same thread as OnTaggerDisposed
                Contract.ThrowIfTrue(_disposed);
                Contract.ThrowIfFalse(_taggers >= 0);

                _taggers++;

                DebugRecordCurrentThread();
            }

            internal void OnTaggerDisposed(Tagger tagger)
            {
                // this should be only called from UI thread.
                // in unit test, must be called from same thread as OnTaggerAdded
                Contract.ThrowIfFalse(_taggers > 0);

                _taggers--;

                if (_taggers == 0)
                {
                    this.Dispose();

                    DebugVerifyThread();
                }
            }

            internal void TestOnly_Dispose()
            {
                Dispose();
            }

#if DEBUG
            private Thread _thread;
            private string _stackTrace;

            private void DebugRecordInitialStackTrace()
            {
                _stackTrace = new StackTrace().ToString();
            }

            private void DebugRecordCurrentThread()
            {
                if (_taggers != 1)
                {
                    return;
                }

                _thread = Thread.CurrentThread;
            }

            private void DebugVerifyThread()
            {
                Contract.ThrowIfFalse(Thread.CurrentThread == _thread);
            }
#else
            private void DebugRecordInitialStackTrace()
            {
            }

            private void DebugRecordCurrentThread()
            {
            }

            private void DebugVerifyThread()
            {
            }
#endif
        }
    }
}
