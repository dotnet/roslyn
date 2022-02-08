// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StandardTableDataSources.ErrorTableDataSource)]
    [DataSource(VisualStudioDiagnosticListTableWorkspaceEventListener.IdentifierString)]
    [Name(Name)]
    [Order(Before = "default")]
    internal partial class DiagnosticTableControlEventProcessorProvider : AbstractTableControlEventProcessorProvider<DiagnosticTableItem>
    {
        internal const string Name = "C#/VB Diagnostic Table Event Processor";
        private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticTableControlEventProcessorProvider(
            IVisualStudioDiagnosticListSuppressionStateService suppressionStateService)
        {
            _suppressionStateService = (VisualStudioDiagnosticListSuppressionStateService)suppressionStateService;
        }

        protected override EventProcessor CreateEventProcessor()
        {
            var suppressionStateEventProcessor = new SuppressionStateEventProcessor(_suppressionStateService);
            return new AggregateDiagnosticTableControlEventProcessor(additionalEventProcessors: suppressionStateEventProcessor);
        }
    }
}
