// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
