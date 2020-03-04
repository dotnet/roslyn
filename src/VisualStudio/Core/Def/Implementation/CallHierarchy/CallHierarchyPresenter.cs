﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;
using Microsoft.VisualStudio.CallHierarchy.Package.Definitions;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CallHierarchy
{
    [Export(typeof(ICallHierarchyPresenter))]
    internal class CallHierarchyPresenter : ICallHierarchyPresenter
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public CallHierarchyPresenter(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = (IServiceProvider)serviceProvider;
        }

        public void PresentRoot(CallHierarchyItem root)
        {
            var callHierarchy = _serviceProvider.GetService(typeof(SCallHierarchy)) as ICallHierarchy;
            callHierarchy.ShowToolWindow();
            callHierarchy.AddRootItem((ICallHierarchyMemberItem)root);
        }
    }
}
