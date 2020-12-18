// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog
{
    [Export(typeof(RemoveUnusedReferencesDialogProvider)), Shared]
    internal class RemoveUnusedReferencesDialogProvider
    {
        private readonly UnusedReferencesTableProvider _tableProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoveUnusedReferencesDialogProvider(UnusedReferencesTableProvider tableProvider)
        {
            _tableProvider = tableProvider;
        }

        public RemoveUnusedReferencesDialog CreateDialog(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates)
        {
            var tableControl = _tableProvider.CreateTableControl();
            tableControl.ShowGroupingLine = true;
            tableControl.DoColumnsAutoAdjust = true;
            tableControl.DoSortingAndGroupingWhileUnstable = true;

            var dialog = new RemoveUnusedReferencesDialog(tableControl.Control);
            _tableProvider.SetTableData(project, referenceUpdates);

            // The table updates asychronously and is waiting for the UI thread. This allows the table
            // to update and show our data.
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await tableControl.ForceUpdateAsync().ConfigureAwait(true);
            });

            return dialog;
        }
    }
}
