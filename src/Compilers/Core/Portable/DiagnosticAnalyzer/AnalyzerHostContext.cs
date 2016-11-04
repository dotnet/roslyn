// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Manages properties of analyzers (such as registered actions, supported diagnostics) for analyzer host
    /// 
    /// It ensures the following for analyzer host:
    /// 1) <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> is invoked only once per-analyzer.
    /// 2) <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is invoked only once per-analyzer.
    /// 3) <see cref="CompilationStartAnalyzerAction"/> registered during Initialize are invoked only once per-compilation
    /// </summary>
    public abstract class AnalyzerHostContext : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Create new default <see cref="AnalyzerHostContext"/>
        /// </summary>
        public static AnalyzerHostContext Create()
        {
            return new AnalyzerManager();
        }

        internal AnalyzerHostContext()
        {
            // we don't allow user to define its own context yet
            _disposed = false;
        }

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AnalyzerHostContext()
        {
            // make sure we dispose context to prevent
            // internal states from leaking
            Dispose(false);
        }
    }
}
