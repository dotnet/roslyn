// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
