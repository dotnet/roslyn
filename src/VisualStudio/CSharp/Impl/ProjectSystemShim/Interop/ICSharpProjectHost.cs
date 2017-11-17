// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
{
    /// <summary>
    /// ICSharpProjectHost is exposed by the language service, and receives notification of project creation or opening.
    /// </summary>
    [ComImport]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1F3B9583-A66A-4be1-A15B-901DA4DB4ACF")]
    internal interface ICSharpProjectHost
    {
        /// <summary>
        /// This function is called when a project is opened/created. The language service must site the project object,
        /// and can keep a pointer to it for its own tracking purposes, etc.
        /// </summary>
        void BindToProject(ICSharpProjectRoot project, IVsHierarchy hierarchy);
    }
}
