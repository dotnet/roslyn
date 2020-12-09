// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog
{
    [Export(typeof(UnusedReferencesTableProvider))]
    internal partial class UnusedReferencesTableProvider
    {
        private readonly ITableManager _tableManager;
        private readonly IWpfTableControlProvider _tableControlProvider;
        private readonly UnusedReferencesDataSource _dataSource;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnusedReferencesTableProvider(
            ITableManagerProvider tableMangerProvider,
            IWpfTableControlProvider tableControlProvider)
        {
            _tableManager = tableMangerProvider.GetTableManager(nameof(UnusedReferencesDataSource));
            _tableControlProvider = tableControlProvider;

            _dataSource = new UnusedReferencesDataSource();
            _tableManager.AddSource(_dataSource, UnusedReferencesColumnDefinitions.ColumnNames);
        }

        public IWpfTableControl4 CreateTableControl()
        {
            return (IWpfTableControl4)_tableControlProvider.CreateControl(
                _tableManager,
                autoSubscribe: true,
                BuildColumnStates(),
                UnusedReferencesColumnDefinitions.SolutionName,
                UnusedReferencesColumnDefinitions.ProjectName,
                UnusedReferencesColumnDefinitions.ReferenceType,
                UnusedReferencesColumnDefinitions.ReferenceName,
                UnusedReferencesColumnDefinitions.UpdateAction);
        }

        public void SetTableData(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates)
        {
            _dataSource.ReplaceData(project, referenceUpdates.OrderBy(update => update.ReferenceInfo.ItemSpecification).ToImmutableArray());
        }

        private ImmutableArray<ColumnState> BuildColumnStates()
        {
            return ImmutableArray.Create(
                new ColumnState2(UnusedReferencesColumnDefinitions.SolutionName, isVisible: false, width: 200, sortPriority: 0, descendingSort: false, groupingPriority: 1),
                new ColumnState2(UnusedReferencesColumnDefinitions.ProjectName, isVisible: false, width: 200, sortPriority: 0, descendingSort: false, groupingPriority: 2),
                new ColumnState2(UnusedReferencesColumnDefinitions.ReferenceType, isVisible: false, width: 200, sortPriority: 0, descendingSort: false, groupingPriority: 3),
                new ColumnState(UnusedReferencesColumnDefinitions.ReferenceName, isVisible: true, width: 300, sortPriority: 0, descendingSort: false),
                new ColumnState(UnusedReferencesColumnDefinitions.UpdateAction, isVisible: true, width: 100, sortPriority: 0, descendingSort: false));
        }
    }
}
