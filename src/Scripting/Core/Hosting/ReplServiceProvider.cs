// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Provides basic REPL functionality.
    /// </summary>
    internal abstract class ReplServiceProvider
    {
        public abstract ObjectFormatter ObjectFormatter { get; }
        public abstract CommandLineParser CommandLineParser { get; }
        public abstract DiagnosticFormatter DiagnosticFormatter { get; }
        public abstract string Logo { get; }

        public abstract Script<T> CreateScript<T>(string code, ScriptOptions options, Type globalsTypeOpt, InteractiveAssemblyLoader assemblyLoader);
    }
}
