// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Description("Visual Studio Interactive Window")]
    [ProvideKeyBindingTable(Guids.InteractiveToolWindowIdString, 200)] // Resource ID: "Interactive Window"
    [ProvideMenuResource("Menus.ctmenu", 3)]
    [Guid(Guids.InteractiveWindowPackageIdString)]
    [ProvideBindingPath]  // make sure our DLLs are loadable from other packages
    public sealed class InteractiveWindowPackage : Package
    {
    }
}
