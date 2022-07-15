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
    // Microsoft.VisualStudio.LanguageServer.Client.Implementation does not ship on nuget but Microsoft.VisualStudio.LanguageServices does so we cannot depend
    // on it directly - we instead need this shim to act as a redirect.
    // The request for our dependencies to be available on nuget.org is tracked internally by: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1542016/
    internal interface ILanguageServiceBrokerShim
    {
        Task<JToken?> RequestAsync(
            ITextBuffer textBuffer,
            Func<JToken, bool> capabilitiesFilter,
            string languageServerName,
            string method,
            Func<ITextSnapshot, JToken> parameterFactory,
            CancellationToken cancellationToken);
    }
}
