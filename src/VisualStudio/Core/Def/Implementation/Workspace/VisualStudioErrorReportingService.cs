// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioErrorReportingService : IErrorReportingService
    {
        private readonly IInfoBarService _infoBarService;

        public VisualStudioErrorReportingService(IInfoBarService infoBarService)
        {
            _infoBarService = infoBarService;
        }

        public void ShowErrorInfoInActiveView(string message, params InfoBarUI[] items)
        {
            _infoBarService.ShowInfoBarInActiveView(message, items);
        }

        public void ShowGlobalErrorInfo(string message, params InfoBarUI[] items)
        {
            _infoBarService.ShowInfoBarInGlobalView(message, items);
        }

        public void ShowDetailedErrorInfo(Exception exception)
        {
            var errorInfo = GetFormattedExceptionStack(exception);
            (new DetailedErrorInfoDialog(exception.Message, errorInfo)).ShowModal();
        }
    }
}
