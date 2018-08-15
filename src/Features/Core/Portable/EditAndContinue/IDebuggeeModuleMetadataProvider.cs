// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Provides metadata of modules loaded into processes being debugged.
    /// </summary>
    internal interface IDebuggeeModuleMetadataProvider
    {
        /// <summary>
        /// Finds a module of given MVID in one of the processes being debugged and returns its baseline metadata and symbols.
        /// Shall only be called while in debug mode.
        /// </summary>
        DebuggeeModuleInfo TryGetBaselineModuleInfo(Guid mvid);
    }
}
