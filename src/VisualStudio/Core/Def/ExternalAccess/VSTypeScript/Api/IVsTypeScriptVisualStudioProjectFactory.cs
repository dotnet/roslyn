// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api
{
    internal interface IVsTypeScriptVisualStudioProjectFactory
    {
        VSTypeScriptVisualStudioProjectWrapper CreateAndAddToWorkspace(string projectSystemName, string language, string projectFilePath, IVsHierarchy hierarchy, Guid projectGuid);
    }
}
