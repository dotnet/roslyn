// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class ThrowingTraceListener : TraceListener
    {
        public override void Fail(string? message, string? detailMessage)
        {
            throw new InvalidOperationException(
                (string.IsNullOrEmpty(message) ? "Assertion failed" : message) +
                (string.IsNullOrEmpty(detailMessage) ? "" : Environment.NewLine + detailMessage));
        }

        public override void Write(object? o)
        {
        }

        public override void Write(object? o, string? category)
        {
        }

        public override void Write(string? message)
        {
        }

        public override void Write(string? message, string? category)
        {
        }

        public override void WriteLine(object? o)
        {
        }

        public override void WriteLine(object? o, string? category)
        {
        }

        public override void WriteLine(string? message)
        {
        }

        public override void WriteLine(string? message, string? category)
        {
        }
    }
}
