// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
{
    /// <summary>
    /// Matches OutputFileTypes in the legacy vsl\idl\csharppublic\csiface\csiface.idl
    /// </summary>
    internal enum OutputFileType
    {
        Console,
        Windows,
        Library,
        Module,
        AppContainer,
        WinMDObj
    }
}
