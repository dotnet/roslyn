// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

internal partial class AbstractLegacyProject : ICompilerOptionsHostObject
{
    int ICompilerOptionsHostObject.SetCompilerOptions(string compilerOptions, out bool supported)
    {
#pragma warning disable CS0618 // Type or member is obsolete (Legacy API that cannot be changed)
        ProjectSystemProjectOptionsProcessor.SetCommandLine(compilerOptions);
#pragma warning restore CS0618
        supported = true;
        return VSConstants.S_OK;
    }
}
