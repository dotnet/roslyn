// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    internal interface ILanguageDebugInfoService : ILanguageService
    {
        Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Find an appropriate span to pass the debugger given a point in a snapshot.  Optionally
        /// pass back a string to pass to the debugger instead if no good span can be found.  For
        /// example, if the user hovers on "var" then we actually want to pass the fully qualified
        /// name of the type that 'var' binds to, to the debugger.
        /// </summary>
        Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
