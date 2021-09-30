// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteHostService
    {
        void InitializeGlobalState(int uiCultureLCID, int cultureLCID, CancellationToken cancellationToken);

        void InitializeTelemetrySession(int hostProcessId, string serializedSession, CancellationToken cancellationToken);

        /// <summary>
        /// This is only for debugging
        /// 
        /// this lets remote side to set same logging options as VS side
        /// </summary>
        void SetLoggingFunctionIds(List<string> loggerTypes, List<string> functionIds, CancellationToken cancellationToken);
    }
}
