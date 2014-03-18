// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal static class FailFast
    {
        [DebuggerHidden]
        internal static void OnFatalException(Exception exception)
        {
            // EDMAURER Now using the managed API to fail fast so as to default
            // to the managed VS debug engine and hopefully get great
            // Watson bucketing. Before vanishing trigger anyone listening.
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Environment.FailFast(exception.Message, exception);
        }
    }
}
