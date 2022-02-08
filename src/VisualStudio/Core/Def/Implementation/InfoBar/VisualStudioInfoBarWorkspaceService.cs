// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IInfoBarService), layer: ServiceLayer.Host), Shared]
    internal sealed class VisualStudioInfoBarWorkspaceService : IInfoBarService
    {
        private readonly VisualStudioInfoBarService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioInfoBarWorkspaceService(VisualStudioInfoBarService service)
            => _service = service;

        public void ShowInfoBar(string message, params InfoBarUI[] items)
            => _service.ShowInfoBar(message, items);
    }
}
