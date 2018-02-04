﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    [Export(typeof(IStreamingFindUsagesPresenter)), Shared]
    internal partial class StreamingFindUsagesPresenter :
        ForegroundThreadAffinitizedObject, IStreamingFindUsagesPresenter
    {
        public const string RoslynFindUsagesTableDataSourceIdentifier =
            nameof(RoslynFindUsagesTableDataSourceIdentifier);

        public const string RoslynFindUsagesTableDataSourceSourceTypeIdentifier =
            nameof(RoslynFindUsagesTableDataSourceSourceTypeIdentifier);

        private readonly IServiceProvider _serviceProvider;

        public readonly ITextBufferFactoryService TextBufferFactoryService;
        public readonly ITextEditorFactoryService TextEditorFactoryService;
        public readonly IContentTypeRegistryService ContentTypeRegistryService;
        public readonly ClassificationTypeMap TypeMap;
        public readonly IEditorFormatMapService FormatMapService;
        public readonly IClassificationFormatMap ClassificationFormatMap;
        public readonly DeferredContentFrameworkElementFactory DeferredContentFrameworkElementFactory;

        private readonly IFindAllReferencesService _vsFindAllReferencesService;
        private readonly VisualStudioWorkspace _workspace;

        private readonly HashSet<AbstractTableDataSourceFindUsagesContext> _currentContexts =
            new HashSet<AbstractTableDataSourceFindUsagesContext>();

        [ImportingConstructor]
        public StreamingFindUsagesPresenter(
            VisualStudioWorkspace workspace,
            Shell.SVsServiceProvider serviceProvider,
            ITextBufferFactoryService textBufferFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            DeferredContentFrameworkElementFactory frameworkElementFactory,
            ClassificationTypeMap typeMap,
            IEditorFormatMapService formatMapService,
            IClassificationFormatMapService classificationFormatMapService)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
            TextBufferFactoryService = textBufferFactoryService;
            ContentTypeRegistryService = contentTypeRegistryService;
            DeferredContentFrameworkElementFactory = frameworkElementFactory;

            TextEditorFactoryService = textEditorFactoryService;
            TypeMap = typeMap;
            FormatMapService = formatMapService;
            ClassificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("tooltip");

            _vsFindAllReferencesService = (IFindAllReferencesService)_serviceProvider.GetService(typeof(SVsFindAllReferences));
        }

        public void ClearAll()
        {
            this.AssertIsForeground();

            foreach (var context in _currentContexts)
            {
                context.Clear();
            }
        }

        public FindUsagesContext StartSearch(string title, bool supportsReferences)
        {
            this.AssertIsForeground();
            var context = StartSearchWorker(title, supportsReferences);

            // Keep track of this context object as long as it is being displayed in the UI.
            // That way we can Clear it out if requested by a client.  When the context is
            // no longer being displayed, VS will dispose it and it will remove itself from
            // this set.
            _currentContexts.Add(context);
            return context;
        }

        private AbstractTableDataSourceFindUsagesContext StartSearchWorker(string title, bool supportsReferences)
        {
            this.AssertIsForeground();

            // Get the appropriate window for FAR results to go into.
            var window = _vsFindAllReferencesService.StartSearch(title);

            // Keep track of the users preference for grouping by definition if we don't already know it.
            // We need this because we disable the Definition column when wer're not showing references
            // (i.e. GoToImplementation/GoToDef).  However, we want to restore the user's choice if they
            // then do another FindAllReferences.
            var desiredGroupingPriority = _workspace.Options.GetOption(FindUsagesOptions.DefinitionGroupingPriority);
            if (desiredGroupingPriority < 0)
            {
                StoreCurrentGroupingPriority(window);
            }

            return supportsReferences
                ? StartSearchWithReferences(window, desiredGroupingPriority)
                : StartSearchWithoutReferences(window);
        }

        private AbstractTableDataSourceFindUsagesContext StartSearchWithReferences(IFindAllReferencesWindow window, int desiredGroupingPriority)
        {
            // Ensure that the window's definition-grouping reflects what the user wants.
            // i.e. we may have disabled this column for a previous GoToImplementation call. 
            // We want to still show the column as long as the user has not disabled it themselves.
            var definitionColumn = window.GetDefinitionColumn();
            if (definitionColumn.GroupingPriority != desiredGroupingPriority)
            {
                SetDefinitionGroupingPriority(window, desiredGroupingPriority);
            }

            // If the user changes the grouping, then store their current preference.
            var tableControl = (IWpfTableControl2)window.TableControl;
            tableControl.GroupingsChanged += (s, e) => StoreCurrentGroupingPriority(window);

            return new WithReferencesFindUsagesContext(this, window);
        }

        private AbstractTableDataSourceFindUsagesContext StartSearchWithoutReferences(IFindAllReferencesWindow window)
        {
            // If we're not showing references, then disable grouping by definition, as that will
            // just lead to a poor experience.  i.e. we'll have the definition entry buckets, 
            // with the same items showing underneath them.
            SetDefinitionGroupingPriority(window, 0);
            return new WithoutReferencesFindUsagesContext(this, window);
        }

        private void StoreCurrentGroupingPriority(IFindAllReferencesWindow window)
        {
            var definitionColumn = window.GetDefinitionColumn();
            _workspace.Options = _workspace.Options.WithChangedOption(
                FindUsagesOptions.DefinitionGroupingPriority, definitionColumn.GroupingPriority);
        }

        private void SetDefinitionGroupingPriority(IFindAllReferencesWindow window, int priority)
        {
            this.AssertIsForeground();

            var newColumns = ArrayBuilder<ColumnState>.GetInstance();
            var tableControl = (IWpfTableControl2)window.TableControl;

            foreach (var columnState in window.TableControl.ColumnStates)
            {
                var columnState2 = columnState as ColumnState2;
                if (columnState?.Name == StandardTableColumnDefinitions2.Definition)
                {
                    newColumns.Add(new ColumnState2(
                        columnState2.Name,
                        isVisible: false,
                        width: columnState2.Width,
                        sortPriority: columnState2.SortPriority,
                        descendingSort: columnState2.DescendingSort,
                        groupingPriority: priority));
                }
                else
                {
                    newColumns.Add(columnState);
                }
            }

            tableControl.SetColumnStates(newColumns);
            newColumns.Free();
        }
    }
}
