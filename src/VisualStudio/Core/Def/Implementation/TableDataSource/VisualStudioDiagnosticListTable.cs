// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioDiagnosticListTable))]
    internal class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = "{30EE579B-4C9E-432A-9EBD-BF55D0EA47FF}";
        internal static readonly Guid Identifier = new Guid(IdentifierString);

        [ImportingConstructor]
        public VisualStudioDiagnosticListTable(
            SVsServiceProvider serviceProvider, VisualStudioWorkspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            base(serviceProvider, workspace, diagnosticService, Identifier, provider)
        {
            ConnectWorkspaceEvents();

            // create initial project rank map
            this.Source.OnProjectDependencyChanged(workspace.CurrentSolution);
        }

        /// this is for test only
        internal VisualStudioDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            base(null, workspace, diagnosticService, Identifier, provider)
        {
        }
    }
}
