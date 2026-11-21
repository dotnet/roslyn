// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities
{
    internal sealed class TestableCompilerServerLogger : ICompilerServerLogger
    {
        public Func<CompilerServerLogKind, bool> IsEnabledFunc { get; set; } = static _ => true;
        public Action<CompilerServerLogKind, string> LogFunc { get; set; } = delegate { throw new InvalidOperationException(); };

        public bool IsEnabled(CompilerServerLogKind kind) => IsEnabledFunc(kind);

        public void Log(CompilerServerLogKind kind, string message) => LogFunc(kind, message);
    }
}
