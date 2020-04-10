// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioErrorReportingService : IErrorReportingService
    {
        private readonly IInfoBarService _infoBarService;

        public VisualStudioErrorReportingService(IInfoBarService infoBarService)
            => _infoBarService = infoBarService;

        public void ShowErrorInfoInActiveView(string message, params InfoBarUI[] items)
            => _infoBarService.ShowInfoBarInActiveView(message, items);

        public void ShowGlobalErrorInfo(string message, params InfoBarUI[] items)
            => _infoBarService.ShowInfoBarInGlobalView(message, items);

        public void ShowDetailedErrorInfo(Exception exception)
        {
            var errorInfo = GetFormattedExceptionStack(exception);
            (new DetailedErrorInfoDialog(exception.Message, errorInfo)).ShowModal();
        }
    }
}
