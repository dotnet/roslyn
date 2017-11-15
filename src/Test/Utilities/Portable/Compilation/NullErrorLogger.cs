// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
