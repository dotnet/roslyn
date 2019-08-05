// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("38B39ADD-D4D6-47E1-B996-7ED16D0295A5")]
    internal interface IProjectSiteEx
    {
        void StartBatch();
        void EndBatch();

        void AddFileEx([MarshalAs(UnmanagedType.LPWStr)] string filePath, [MarshalAs(UnmanagedType.LPWStr)] string linkMetadata);

        /// <summary>
        /// Allows the project system to pass along property values not covered by the
        /// compiler's command line arguments.
        /// See <see cref="LanguageServices.ProjectSystem.IWorkspaceProjectContext.SetProperty(string, string)"/>
        /// for the corresponding method for CPS-based projects.
        /// </summary>
        void SetProperty([MarshalAs(UnmanagedType.LPWStr)] string property, [MarshalAs(UnmanagedType.LPWStr)] string value);
    }
}
