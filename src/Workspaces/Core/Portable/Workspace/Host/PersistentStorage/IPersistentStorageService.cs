// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Obsolete.  Roslyn no longer supports a mechanism to perform arbitrary persistence of data.  If such functionality
    /// is needed, consumers are responsible for providing it themselves with whatever semantics are needed.
    /// </summary>
    [Obsolete("Roslyn no longer exports a mechanism to perform persistence.", error: true)]
    public interface IPersistentStorageService : IWorkspaceService
    {
        [Obsolete("Roslyn no longer exports a mechanism to perform persistence.", error: true)]
        IPersistentStorage GetStorage(Solution solution);
    }
}
