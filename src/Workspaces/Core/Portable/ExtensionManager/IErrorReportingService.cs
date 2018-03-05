// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
