// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics.EngineV2;

namespace Microsoft.VisualStudio.LanguageServices.Diagnostics
{
    /// <summary>
    /// This interface is solely here to prevent VS.Next dll to be not loaded
    /// when RemoteHost option is off
    /// </summary>
    internal interface IRemoteHostDiagnosticAnalyzerExecutor : ICodeAnalysisDiagnosticAnalyzerExecutor
    {
    }
}
