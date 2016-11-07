// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Manage lifetime of the host that is executing analyzers.
    /// 
    /// <see cref="CompilationWithAnalyzers"/> can be used to execute <see cref="DiagnosticAnalyzer"/> on a <see cref="Compilation"/>. 
    /// However, same instance of an analyzer can be used across different CompilationWithAnalyzers, hence we need CompilationWithAnalyzers 
    /// to take this object that represents the lifetime of the host
    /// 
    /// It ensures the following for analyzer host:
    /// 1) <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> is invoked only once per-analyzer.
    /// 2) <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is invoked only once per-analyzer.
    /// 3) <see cref="CompilationStartAnalyzerAction"/> registered during Initialize are invoked only once per-compilation
    /// 4) <see cref="Dispose()"/> clears all the saved analyzer state for all analyzers that were executed by the host.
    /// </summary>
    public abstract class CompilationWithAnalyzersHost : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Create new default <see cref="CompilationWithAnalyzersHost"/>
        /// </summary>
        public static CompilationWithAnalyzersHost Create()
        {
            return new DefaultCompilationWithAnalyzersHost();
        }

        internal CompilationWithAnalyzersHost()
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

        ~CompilationWithAnalyzersHost()
        {
            // make sure we dispose context to prevent
            // internal states from leaking
            Dispose(false);
        }
    }
}
