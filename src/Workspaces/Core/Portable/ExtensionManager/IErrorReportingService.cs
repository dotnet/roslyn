// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Extensions
{
    internal interface IErrorReportingService : IWorkspaceService
    {
        /// <summary>
        /// Show error info in an active view.
        ///
        /// Different host can have different definition on what active view means.
        /// </summary>
        void ShowErrorInfoInActiveView(string message, params InfoBarUI[] items);

        /// <summary>
        /// Show global error info.
        ///
        /// this kind error info should be something that affects whole roslyn such as
        /// background compilation is disabled due to memory issue and etc
        /// </summary>
        void ShowGlobalErrorInfo(string message, params InfoBarUI[] items);

        void ShowDetailedErrorInfo(Exception exception);

        /// <summary>
        /// Shows info-bar reporting ServiceHub process crash.
        /// "Unfortunately a process used by Visual Studio has encountered an unrecoverable error".
        /// </summary>
        void ShowRemoteHostCrashedErrorInfo(Exception? exception);
    }
}
