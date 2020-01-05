// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

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
        /// Shall only be called on MTA thread.
        /// </summary>
        /// <returns>Null, if the module with the specified MVID is not loaded.</returns>
        DebuggeeModuleInfo? TryGetBaselineModuleInfo(Guid mvid);

        /// <summary>
        /// Returns an error message when any instance of a module with given <paramref name="mvid"/> disallows EnC.
        /// </summary>
        bool IsEditAndContinueAvailable(Guid mvid, out int errorCode, [NotNullWhen(true)]out string localizedMessage);

        /// <summary>
        /// Notifies the debugger that a document changed that may affect the given module when the change is applied.
        /// </summary>
        void PrepareModuleForUpdate(Guid mvid);
    }
}
