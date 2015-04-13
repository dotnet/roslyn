// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Base type of a type that is used as <see cref="DiagnosticsUpdatedArgs.Id"/> for live diagnostic
    /// </summary>
    internal class AnalyzerUpdateArgsId : ErrorSourceId.Base<DiagnosticAnalyzer>, ISupportLiveUpdate
    {
        public DiagnosticAnalyzer Analyzer => _Field1;

        protected AnalyzerUpdateArgsId(DiagnosticAnalyzer analyzer) :
            base(analyzer)
        {
        }

        public override string ErrorSource
        {
            get
            {
                if (Analyzer == null)
                {
                    return string.Empty;
                }

                return Analyzer.GetAnalyzerAssemblyName();
            }
        }
    }
}
