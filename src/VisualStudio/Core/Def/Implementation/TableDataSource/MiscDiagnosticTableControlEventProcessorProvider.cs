// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StandardTableDataSources.ErrorTableDataSource)]
    [DataSource(MiscellaneousDiagnosticListTableWorkspaceEventListener.IdentifierString)]
    [Name(Name)]
    [Order(Before = "default")]
    internal sealed class MiscDiagnosticTableControlEventProcessorProvider : AbstractTableControlEventProcessorProvider<DiagnosticTableItem>
    {
        internal const string Name = "Misc C#/VB Diagnostic Table Event Processor";

        [ImportingConstructor]
        public MiscDiagnosticTableControlEventProcessorProvider()
        {
        }
    }
}
