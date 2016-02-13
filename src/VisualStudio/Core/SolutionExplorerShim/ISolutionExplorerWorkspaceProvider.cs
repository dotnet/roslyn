// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// this is a workaround to get different workspace per host without text buffer
    /// 
    /// currently, any feature that doesn't work on top of a buffer imports specific type of workspace explicitly. there is
    /// no broker in-between - unlike features that work on top of text buffers. so these components end up tightly coupled with specific
    /// kind of host, and can't be re-used in other host even if code itself could be.
    /// 
    /// this is a workaround interface for that limitation. 
    /// 
    /// * this could be only issue in what I am trying to do since ETA, kind of, emulates VS and that is why this code can be shared at the first place.
    /// </summary>
    internal interface ISolutionExplorerWorkspaceProvider
    {
        Workspace GetWorkspace();
    }
}
