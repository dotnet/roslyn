﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(LegacyDiagnosticItemSourceProvider))]
    [Order]
    [AppliesToProject("(CSharp | VisualBasic) & !CPS")]
    internal sealed class LegacyDiagnosticItemSourceProvider : AttachedCollectionSourceProvider<AnalyzerItem>
    {
        private readonly IAnalyzersCommandHandler _commandHandler;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyDiagnosticItemSourceProvider(
            [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler,
            IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _commandHandler = commandHandler;
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
        }

        protected override IAttachedCollectionSource? CreateCollectionSource(AnalyzerItem item, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                return new LegacyDiagnosticItemSource(item, _commandHandler, _diagnosticAnalyzerService);
            }

            return null;
        }
    }
}
