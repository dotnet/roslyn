// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// Called by the Solution Explorer to show a context menu on the items we add to it
    /// </summary>
    internal class ContextMenuController : IContextMenuController
    {
        private readonly int _menuId;
        private readonly Func<IEnumerable<object>, bool> _shouldShowMenu;
        private readonly Action _updateMenu;

        public ContextMenuController(int menuId, Func<IEnumerable<object>, bool> shouldShowMenu, Action updateMenu)
        {
            _menuId = menuId;
            _shouldShowMenu = shouldShowMenu;
            _updateMenu = updateMenu;
        }

        public bool ShowContextMenu(IEnumerable<object> items, Point location)
        {
            if (!_shouldShowMenu(items))
            {
                return false;
            }

            _updateMenu();

            var shell = Shell.Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            var guidContextMenu = Guids.RoslynGroupId;
            var locationPoints = new[] { new POINTS() { x = (short)location.X, y = (short)location.Y } };
            return shell != null && ErrorHandler.Succeeded(shell.ShowContextMenu(
                0,
                ref guidContextMenu,
                _menuId,
                locationPoints,
                pCmdTrgtActive: null));
        }
    }
}
