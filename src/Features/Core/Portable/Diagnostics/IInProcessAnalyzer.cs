// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A marker interface for <see cref="DiagnosticAnalyzer"/> implementations that do not support out-of-process
    /// execution.
    /// </summary>
    [Obsolete("Diagnostic analyzers must not be restricted in in-process analysis.", error: true)]
    internal interface IInProcessAnalyzer
    {
    }
}
