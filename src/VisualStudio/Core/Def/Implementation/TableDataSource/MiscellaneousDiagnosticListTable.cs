// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(MiscellaneousDiagnosticListTable))]
    internal class MiscellaneousDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = "{55C819C3-98F6-4398-9BE2-5DAC5B690BB8}";
        internal static readonly Guid Identifier = new Guid(IdentifierString);

        [ImportingConstructor]
        public MiscellaneousDiagnosticListTable(
            SVsServiceProvider serviceProvider, MiscellaneousFilesWorkspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            base(serviceProvider, workspace, diagnosticService, Identifier, provider)
        {
            ConnectWorkspaceEvents();
        }

        /// this is for test only
        internal MiscellaneousDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            base(null, workspace, diagnosticService, Identifier, provider)
        {
        }
    }
}
