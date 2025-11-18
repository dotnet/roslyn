// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class RootSymbolTreeItemSourceProvider
{
    private sealed class SymbolItemContextMenuController(
        RootSymbolTreeItemSourceProvider rootProvider) : IContextMenuController
    {
        public bool ShowContextMenu(IEnumerable<object> items, Point location)
        {
            if (items.FirstOrDefault() is not SymbolTreeItem item)
                return false;

            var guidContextMenu = Guids.RoslynGroupId;
            if (Shell.Package.GetGlobalService(typeof(SVsUIShell)) is not IVsUIShell shell)
                return false;

            var result = shell.ShowContextMenu(
                dwCompRole: 0,
                ref guidContextMenu,
                //0x400,
                ID.RoslynCommands.SolutionExplorerSymbolItemContextMenu,
                [new() { x = (short)location.X, y = (short)location.Y }],
                pCmdTrgtActive: new SymbolTreeItemOleCommandTarget(rootProvider, item));
            return ErrorHandler.Succeeded(result);
        }

        private sealed class SymbolTreeItemOleCommandTarget(
            RootSymbolTreeItemSourceProvider rootProvider,
            SymbolTreeItem item) : IOleCommandTarget
        {
            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                // All commands are available on all items currently.  Note: we could refine this if we want to.
                // For example "go to base/derived" doesn't really make sense on fields.  For now though, we just
                // keep things simple.  This is similar to how you always get all these nav options in the editor
                // when you right click on any location in a C# file.
                return HResult.OK;
            }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                var navigationService = nCmdID switch
                {
                    ID.RoslynCommands.SolutionExplorerSymbolItemGoToBase => rootProvider._goToBaseNavigationService,
                    ID.RoslynCommands.SolutionExplorerSymbolItemGoToImplementation => rootProvider._goToImplementationNavigationService,
                    ID.RoslynCommands.SolutionExplorerSymbolItemFindAllReferences => rootProvider._findReferencesNavigationService,
                    _ => null,
                };

                if (navigationService != null)
                {
                    var document = rootProvider._workspace.CurrentSolution.GetDocument(item.ItemKey.DocumentId);
                    if (document != null)
                    {
                        navigationService.ExecuteCommand(
                            document, item.ItemSyntax.NavigationToken.SpanStart,
                            // May be calling this on stale data.  Allow the position to be invalid
                            allowInvalidPosition: true);
                    }
                }

                return HResult.OK;
            }
        }
    }
}
