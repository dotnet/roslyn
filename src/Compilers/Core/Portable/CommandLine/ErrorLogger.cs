// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Base class for logging compiler diagnostics.
    /// </summary>
    internal abstract class ErrorLogger
    {
        public abstract void LogDiagnostic(Diagnostic diagnostic);
    }
}
