// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptLanguageDebugInfoService
    {
        Task<VSTypeScriptDebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Find an appropriate span to pass the debugger given a point in a snapshot.  Optionally
        /// pass back a string to pass to the debugger instead if no good span can be found.  For
        /// example, if the user hovers on "var" then we actually want to pass the fully qualified
        /// name of the type that 'var' binds to, to the debugger.
        /// </summary>
        Task<VSTypeScriptDebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
