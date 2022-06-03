// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using VSLangProj140;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// This class is responsible for showing an add analyzer dialog to the user and adding the analyzer
    /// that is selected by the user in the dialog.
    /// </summary>
    [Export]
    internal class AnalyzerReferenceManager : IVsReferenceManagerUser
    {
        private readonly IServiceProvider _serviceProvider;
        private IVsReferenceManager? _referenceManager;
        private readonly AnalyzerItemsTracker _tracker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerReferenceManager(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            AnalyzerItemsTracker analyzerItemsTracker)
        {
            _serviceProvider = serviceProvider;
            _tracker = analyzerItemsTracker;
        }

        /// <summary>
        /// Show the add analyzer dialog.
        /// </summary>
        public void ShowDialog()
        {
            var referenceManager = GetReferenceManager();
            if (referenceManager != null &&
                _tracker.SelectedHierarchy != null)
            {
                referenceManager.ShowReferenceManager(this,
                                                      SolutionExplorerShim.Add_Analyzer,
                                                      null,
                                                      VSConstants.FileReferenceProvider_Guid,
                                                      fForceShowDefaultProvider: false);
            }
        }

        /// <summary>
        /// Called by the ReferenceManagerDialog to apply the user selection of references.
        /// </summary>
        public void ChangeReferences(uint operation, IVsReferenceProviderContext changedContext)
        {
            var referenceOperation = (__VSREFERENCECHANGEOPERATION)operation;

            // The items selected in Solution Explorer should correspond to exactly one
            // IVsHierarchy, otherwise we shouldn't have even tried to show the dialog.
            Contract.ThrowIfNull(_tracker.SelectedHierarchy);
            if (_tracker.SelectedHierarchy.TryGetProject(out var project))
            {

                if (project.Object is VSProject3 vsproject)
                {
                    foreach (IVsReference reference in changedContext.References)
                    {
                        var path = reference.FullPath;

                        switch (referenceOperation)
                        {
                            case __VSREFERENCECHANGEOPERATION.VSREFERENCECHANGEOPERATION_ADD:
                                vsproject.AnalyzerReferences.Add(path);
                                break;
                            case __VSREFERENCECHANGEOPERATION.VSREFERENCECHANGEOPERATION_REMOVE:
                                vsproject.AnalyzerReferences.Remove(path);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called by the ReferenceManagerDialog to determine what contexts to show in the UI.
        /// </summary>
        public Array GetProviderContexts()
        {
            // Return just the File provider context so that just the browse tab shows up.
            var context = (IVsFileReferenceProviderContext)GetReferenceManager().CreateProviderContext(VSConstants.FileReferenceProvider_Guid);
            context.BrowseFilter = string.Format("{0} (*.dll)\0*.dll\0", SolutionExplorerShim.Analyzer_Files);
            return new[] { context };
        }

        private IVsReferenceManager GetReferenceManager()
        {
            if (_referenceManager == null)
            {
                _referenceManager = _serviceProvider.GetService(typeof(SVsReferenceManager)) as IVsReferenceManager;
                Assumes.Present(_referenceManager);
            }

            return _referenceManager;
        }
    }
}
