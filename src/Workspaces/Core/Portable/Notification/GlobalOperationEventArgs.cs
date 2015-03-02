﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Notification
{
    internal class GlobalOperationEventArgs : EventArgs
    {
        public IReadOnlyList<string> Operations { get; }
        public bool Cancelled { get; }

        public GlobalOperationEventArgs(IReadOnlyList<string> operations, bool cancelled)
        {
            this.Operations = operations;
            this.Cancelled = cancelled;
        }
    }
}
