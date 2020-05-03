// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal class NullErrorLogger : ErrorLogger
    {
        internal static ErrorLogger Instance => new NullErrorLogger();

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
        }
    }
}
