// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace Roslyn.Test.Utilities
{
    public sealed class XunitTraceListener : TraceListener
    {
        private readonly ITestOutputHelper _logger;
        private readonly StringBuilder _lineInProgress = new StringBuilder();
        private bool _disposed;

        public XunitTraceListener(ITestOutputHelper logger)
            => _logger = logger;

        public override bool IsThreadSafe
            => false;

        public override void Write(string? message)
            => _lineInProgress.Append(message);

        public override void WriteLine(string? message)
        {
            if (!_disposed)
            {
                _logger.WriteLine(_lineInProgress.ToString() + message);
                _lineInProgress.Clear();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
