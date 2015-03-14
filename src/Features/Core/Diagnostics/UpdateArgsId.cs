// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class UpdateArgsId
    {
        public readonly DiagnosticAnalyzer Analyzer;

        /// <summary>
        /// Base type for <see cref="DiagnosticsUpdatedArgs.Id"/> for live diagnostic
        /// </summary>
        /// <param name="analyzer"></param>
        protected UpdateArgsId(DiagnosticAnalyzer analyzer)
        {
            Analyzer = analyzer;
        }

        public override bool Equals(object obj)
        {
            var other = obj as UpdateArgsId;
            if (other == null)
            {
                return false;
            }

            return Analyzer == other.Analyzer;
        }

        public override int GetHashCode()
        {
            return Analyzer.GetHashCode();
        }
    }
}
