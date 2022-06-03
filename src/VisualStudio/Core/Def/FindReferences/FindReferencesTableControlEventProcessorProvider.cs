// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>
    /// Event processor that we export so we can control how navigation works in the streaming
    /// FAR window.  We need this because the FAR window has no way to know how to do things like
    /// navigate to definitions that are from metadata.  We take control here and handle navigation
    /// ourselves so that we can do things like navigate to MetadataAsSource.
    /// </summary>
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StreamingFindUsagesPresenter.RoslynFindUsagesTableDataSourceSourceTypeIdentifier)]
    [DataSource(StreamingFindUsagesPresenter.RoslynFindUsagesTableDataSourceIdentifier)]
    [Name(nameof(FindUsagesTableControlEventProcessorProvider))]
    [Order(Before = Priority.Default)]
    internal class FindUsagesTableControlEventProcessorProvider : ITableControlEventProcessorProvider
    {
        private readonly IUIThreadOperationExecutor _operationExecutor;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindUsagesTableControlEventProcessorProvider(
            IUIThreadOperationExecutor operationExecutor,
            IAsynchronousOperationListenerProvider asyncProvider)
        {
            _operationExecutor = operationExecutor;
            _listener = asyncProvider.GetListener(FeatureAttribute.FindReferences);
        }

        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
            => new TableControlEventProcessor(_operationExecutor, _listener);

        private class TableControlEventProcessor : TableControlEventProcessorBase
        {
            private readonly IUIThreadOperationExecutor _operationExecutor;
            private readonly IAsynchronousOperationListener _listener;

            public TableControlEventProcessor(IUIThreadOperationExecutor operationExecutor, IAsynchronousOperationListener listener)
            {
                _operationExecutor = operationExecutor;
                _listener = listener;
            }

            public override void PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
            {
                var supportsNavigation = entry.Identity as ISupportsNavigation ??
                    (entry.TryGetValue(StreamingFindUsagesPresenter.SelfKeyName, out var item) ? item as ISupportsNavigation : null);
                if (supportsNavigation != null &&
                    supportsNavigation.CanNavigateTo())
                {
                    // Fire and forget
                    e.Handled = true;
                    _ = ProcessNavigateAsync(supportsNavigation, e, _listener, _operationExecutor);
                }

                base.PreprocessNavigate(entry, e);
                return;

                async static Task ProcessNavigateAsync(
                    ISupportsNavigation supportsNavigation, TableEntryNavigateEventArgs e,
                    IAsynchronousOperationListener listener,
                    IUIThreadOperationExecutor operationExecutor)
                {
                    using var token = listener.BeginAsyncOperation(nameof(ProcessNavigateAsync));
                    using var context = operationExecutor.BeginExecute(
                        ServicesVSResources.IntelliSense,
                        EditorFeaturesResources.Navigating,
                        allowCancellation: true,
                        showProgress: false);

                    var options = new NavigationOptions(PreferProvisionalTab: e.IsPreview, ActivateTab: e.ShouldActivate);
                    await supportsNavigation.NavigateToAsync(options, context.UserCancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
