﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
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

            IVsUIShell shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            Guid guidContextMenu = Guids.RoslynGroupId;
            POINTS[] locationPoints = new[] { new POINTS() { x = (short)location.X, y = (short)location.Y } };
            return shell != null && ErrorHandler.Succeeded(shell.ShowContextMenu(
                0,
                ref guidContextMenu,
                _menuId,
                locationPoints,
                pCmdTrgtActive: null));
        }
    }
}
