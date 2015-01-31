// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal partial class AsynchronousOperationListener
    {
        protected internal class DiagnosticAsyncToken : AsyncToken
        {
            private readonly string name;
            private readonly object tag;
            private Task task;

            private readonly string stackTrace;
            private string completesAsyncOperationStackTrace;

            public DiagnosticAsyncToken(AsynchronousOperationListener listener, string name, object tag)
                : base(listener)
            {
                this.name = name;
                this.tag = tag;

                this.stackTrace = new StackTrace().ToString();
            }

            internal void AssociateWithTask(Task task)
            {
                this.task = task;

                this.completesAsyncOperationStackTrace = new StackTrace().ToString();
            }
        }
    }
}
