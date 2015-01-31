// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal partial class AsynchronousOperationListener
    {
        protected internal class DiagnosticAsyncToken : AsyncToken
        {
            private readonly string _name;
            private readonly object _tag;
            private Task _task;

            private readonly string _stackTrace;
            private string _completesAsyncOperationStackTrace;

            public DiagnosticAsyncToken(AsynchronousOperationListener listener, string name, object tag)
                : base(listener)
            {
                _name = name;
                _tag = tag;

                _stackTrace = new StackTrace().ToString();
            }

            internal void AssociateWithTask(Task task)
            {
                _task = task;

                _completesAsyncOperationStackTrace = new StackTrace().ToString();
            }
        }
    }
}
