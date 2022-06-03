// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IRefactorNotifyService))]
    internal sealed class VsRefactorNotifyService : IRefactorNotifyService
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VsRefactorNotifyService(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            if (TryGetRenameAPIRequiredArguments(workspace, changedDocumentIDs, symbol, out var hierarchyToItemIDsMap, out var rqnames))
            {
                foreach (var hierarchy in hierarchyToItemIDsMap.Keys)
                {
                    var itemIDs = hierarchyToItemIDsMap[hierarchy];

                    if (hierarchy is IVsHierarchyRefactorNotify refactorNotify)
                    {
                        var hresult = refactorNotify.OnBeforeGlobalSymbolRenamed(
                            (uint)itemIDs.Count,
                            itemIDs.ToArray(),
                            (uint)rqnames.Length,
                            rqnames,
                            newName,
                            promptContinueOnFail: 1);

                        if (hresult < 0)
                        {
                            if (throwOnFailure)
                            {
                                Marshal.ThrowExceptionForHR(hresult);
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            if (TryGetRenameAPIRequiredArguments(workspace, changedDocumentIDs, symbol, out var hierarchyToItemIDsMap, out var rqnames))
            {
                foreach (var hierarchy in hierarchyToItemIDsMap.Keys)
                {
                    var itemIDs = hierarchyToItemIDsMap[hierarchy];

                    if (hierarchy is IVsHierarchyRefactorNotify refactorNotify)
                    {
                        var hresult = refactorNotify.OnGlobalSymbolRenamed(
                            (uint)itemIDs.Count,
                            itemIDs.ToArray(),
                            (uint)rqnames.Length,
                            rqnames,
                            newName);

                        if (hresult < 0)
                        {
                            if (throwOnFailure)
                            {
                                Marshal.ThrowExceptionForHR(hresult);
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool TryGetRenameAPIRequiredArguments(
            Workspace workspace,
            IEnumerable<DocumentId> changedDocumentIDs,
            ISymbol symbol,
            out Dictionary<IVsHierarchy, List<uint>> hierarchyToItemIDsMap,
            out string[] rqnames)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            rqnames = null;
            if (!TryGetItemIDsAndRQName(workspace, changedDocumentIDs, symbol, out hierarchyToItemIDsMap, out var rqname))
            {
                return false;
            }

            rqnames = new string[1] { rqname };
            return true;
        }

        private bool TryGetItemIDsAndRQName(
            Workspace workspace,
            IEnumerable<DocumentId> changedDocumentIDs,
            ISymbol symbol,
            out Dictionary<IVsHierarchy, List<uint>> hierarchyToItemIDsMap,
            out string rqname)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            hierarchyToItemIDsMap = null;
            rqname = null;

            if (!changedDocumentIDs.Any())
            {
                return false;
            }

            if (workspace is not VisualStudioWorkspace visualStudioWorkspace)
            {
                return false;
            }

            if (!TryGetRenamingRQNameForSymbol(symbol, out rqname))
            {
                return false;
            }

            hierarchyToItemIDsMap = GetHierarchiesAndItemIDsFromDocumentIDs(visualStudioWorkspace, changedDocumentIDs);
            return true;
        }

        private static bool TryGetRenamingRQNameForSymbol(ISymbol symbol, out string rqname)
        {
            if (symbol.Kind == SymbolKind.Method)
            {
                var methodSymbol = symbol as IMethodSymbol;

                if (methodSymbol.MethodKind is MethodKind.Constructor or
                    MethodKind.Destructor)
                {
                    symbol = symbol.ContainingType;
                }
            }

            rqname = LanguageServices.RQName.From(symbol);
            return rqname != null;
        }

        private Dictionary<IVsHierarchy, List<uint>> GetHierarchiesAndItemIDsFromDocumentIDs(VisualStudioWorkspace visualStudioWorkspace, IEnumerable<DocumentId> changedDocumentIDs)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var hierarchyToItemIDsMap = new Dictionary<IVsHierarchy, List<uint>>();

            foreach (var documentId in changedDocumentIDs)
            {
                var hierarchy = visualStudioWorkspace.GetHierarchy(documentId.ProjectId);

                if (hierarchy == null)
                {
                    continue;
                }

                var document = visualStudioWorkspace.CurrentSolution.GetDocument(documentId);
                var itemID = hierarchy.TryGetItemId(document.FilePath);

                if (itemID == VSConstants.VSITEMID_NIL)
                {
                    continue;
                }

                if (!hierarchyToItemIDsMap.TryGetValue(hierarchy, out var itemIDsForCurrentHierarchy))
                {
                    itemIDsForCurrentHierarchy = new List<uint>();
                    hierarchyToItemIDsMap.Add(hierarchy, itemIDsForCurrentHierarchy);
                }

                if (!itemIDsForCurrentHierarchy.Contains(itemID))
                {
                    itemIDsForCurrentHierarchy.Add(itemID);
                }
            }

            return hierarchyToItemIDsMap;
        }
    }
}
