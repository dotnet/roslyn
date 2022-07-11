// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim
{
    // This shim type is required as ILanguageServiceBroker2 cannot be imported directly since the Microsoft.VisualStudio.LanguageServer.Client.Implementation
    // package reference exists in Microsoft.VisualStudio.LanguageServices.Implementation (instead of Microsoft.VisualStudio.LanguageServices).
    // The request for our dependencies to be available on nuget.org is tracked internally by: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1542016/
    internal interface ILanguageServiceBrokerShim
    {
        Task<JToken?> RequestAsync(ITextBuffer textBuffer, Func<JToken, bool> capabilitiesFilter, string languageServerName, string method, Func<ITextSnapshot, JToken> parameterFactory, CancellationToken cancellationToken);
    }
}
