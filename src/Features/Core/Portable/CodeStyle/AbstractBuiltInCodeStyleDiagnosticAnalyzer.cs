// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractBuiltInCodeStyleDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(
            string descriptorId, LocalizableString title,
            LocalizableString messageFormat = null,
            bool configurable = true)
            : base(descriptorId, title, messageFormat, configurable)
        {
        }

        protected AbstractBuiltInCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> supportedDiagnostics)
            : base(supportedDiagnostics)
        {
        }

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        public virtual bool OpenFileOnly(Workspace workspace)
            => false;
    }
}
