// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Provides basic REPL functionality.
    /// </summary>
    internal interface IRepl
    {
        ObjectFormatter CreateObjectFormatter();
        Script CreateScript(string code);
        CommandLineParser GetCommandLineParser();
        DiagnosticFormatter GetDiagnosticFormatter();
        string GetLogo();
    }
}
