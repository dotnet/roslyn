// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.RQName;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(IRefactorNotifyService))]
    internal sealed class VsRefactorNotifyService : ForegroundThreadAffinitizedObject, IRefactorNotifyService
    {
        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            AssertIsForeground();

            Dictionary<IVsHierarchy, List<uint>> hierarchyToItemIDsMap;
            string[] rqnames;

            if (TryGetRenameAPIRequiredArguments(workspace, changedDocumentIDs, symbol, out hierarchyToItemIDsMap, out rqnames))
            {
                foreach (var hierarchy in hierarchyToItemIDsMap.Keys)
                {
                    var itemIDs = hierarchyToItemIDsMap[hierarchy];
                    var refactorNotify = hierarchy as IVsHierarchyRefactorNotify;

                    if (refactorNotify != null)
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
            AssertIsForeground();

            Dictionary<IVsHierarchy, List<uint>> hierarchyToItemIDsMap;
            string[] rqnames;

            if (TryGetRenameAPIRequiredArguments(workspace, changedDocumentIDs, symbol, out hierarchyToItemIDsMap, out rqnames))
            {
                foreach (var hierarchy in hierarchyToItemIDsMap.Keys)
                {
                    var itemIDs = hierarchyToItemIDsMap[hierarchy];
                    var refactorNotify = hierarchy as IVsHierarchyRefactorNotify;

                    if (refactorNotify != null)
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
            AssertIsForeground();

            hierarchyToItemIDsMap = null;
            rqnames = null;

            string rqname;
            VisualStudioWorkspaceImpl visualStudioWorkspace;

            if (!TryGetItemIDsAndRQName(workspace, changedDocumentIDs, symbol, out visualStudioWorkspace, out hierarchyToItemIDsMap, out rqname))
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
            out VisualStudioWorkspaceImpl visualStudioWorkspace,
            out Dictionary<IVsHierarchy, List<uint>> hierarchyToItemIDsMap,
            out string rqname)
        {
            AssertIsForeground();

            visualStudioWorkspace = null;
            hierarchyToItemIDsMap = null;
            rqname = null;

            if (!changedDocumentIDs.Any())
            {
                return false;
            }

            visualStudioWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (visualStudioWorkspace == null)
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

        private bool TryGetRenamingRQNameForSymbol(ISymbol symbol, out string rqname)
        {
            if (symbol.Kind == SymbolKind.Method)
            {
                var methodSymbol = symbol as IMethodSymbol;

                if (methodSymbol.MethodKind == MethodKind.Constructor ||
                    methodSymbol.MethodKind == MethodKind.Destructor)
                {
                    symbol = symbol.ContainingType;
                }
            }

            rqname = LanguageServices.RQName.From(symbol);
            return rqname != null;
        }

        private Dictionary<IVsHierarchy, List<uint>> GetHierarchiesAndItemIDsFromDocumentIDs(VisualStudioWorkspaceImpl visualStudioWorkspace, IEnumerable<DocumentId> changedDocumentIDs)
        {
            AssertIsForeground();

            var hierarchyToItemIDsMap = new Dictionary<IVsHierarchy, List<uint>>();

            foreach (var docID in changedDocumentIDs)
            {
                var project = visualStudioWorkspace.GetHostProject(docID.ProjectId);
                var itemID = project.GetDocumentOrAdditionalDocument(docID).GetItemId();

                if (itemID == (uint)VSConstants.VSITEMID.Nil)
                {
                    continue;
                }

                List<uint> itemIDsForCurrentHierarchy;
                if (!hierarchyToItemIDsMap.TryGetValue(project.Hierarchy, out itemIDsForCurrentHierarchy))
                {
                    itemIDsForCurrentHierarchy = new List<uint>();
                    hierarchyToItemIDsMap.Add(project.Hierarchy, itemIDsForCurrentHierarchy);
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
