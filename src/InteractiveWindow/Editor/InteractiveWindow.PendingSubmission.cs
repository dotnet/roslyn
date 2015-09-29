// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private class PendingSubmission
        {
            public readonly string Input;

            /// <remarks>
            /// Set only on the last submission in each batch (to notify the caller).
            /// </remarks>
            public readonly TaskCompletionSource<object> Completion;

            public Task Task;

            public PendingSubmission(string input, TaskCompletionSource<object> completion)
            {
                Input = input;
                Completion = completion;
            }
        }
    }
}