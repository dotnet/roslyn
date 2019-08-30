// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue
{
    [Export(typeof(IEditAndContinueService)), Shared]
    internal sealed class VisualStudioEditAndContinueService : EditAndContinueService
    {
        [ImportingConstructor]
        public VisualStudioEditAndContinueService(IDiagnosticAnalyzerService diagnosticService, IActiveStatementProvider activeStatementProvider)
            : base(diagnosticService, activeStatementProvider)
        {
        }
    }
}
