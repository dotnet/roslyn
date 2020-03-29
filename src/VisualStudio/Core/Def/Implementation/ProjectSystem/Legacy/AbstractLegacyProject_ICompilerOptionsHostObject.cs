// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal partial class AbstractLegacyProject : ICompilerOptionsHostObject
    {
        int ICompilerOptionsHostObject.SetCompilerOptions(string compilerOptions, out bool supported)
        {
            VisualStudioProjectOptionsProcessor.SetCommandLine(compilerOptions);
            supported = true;
            return VSConstants.S_OK;
        }
    }
}
