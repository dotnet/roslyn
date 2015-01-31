// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StandardTableDataSources.ErrorTableDataSourceString)]
    [DataSource(MiscellaneousDiagnosticListTable.IdentifierString)]
    [Name(Name)]
    [Order(Before = "default")]
    internal class MiscDiagnosticTableControlEventProcessorProvider : AbstractTableControlEventProcessorProvider<DiagnosticData>
    {
        internal const string Name = "Misc C#/VB Diagnostic Table Event Processor";
    }
}
