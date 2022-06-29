// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim
{
    [Export(typeof(ILanguageServiceBrokerShim))]
    internal class LanguageServiceBrokerShim : ILanguageServiceBrokerShim
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker2;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageServiceBrokerShim(ILanguageServiceBroker2 languageServiceBroker2)
        {
            _languageServiceBroker2 = languageServiceBroker2;
        }

        public async Task<JToken?> RequestAsync(ITextBuffer textBuffer, Func<JToken, bool> capabilitiesFilter, string languageServerName, string method, Func<ITextSnapshot, JToken> parameterFactory, CancellationToken cancellationToken)
        {
            return (await _languageServiceBroker2.RequestAsync(textBuffer, capabilitiesFilter, languageServerName, method, parameterFactory, cancellationToken).ConfigureAwait(false))?.Response;
        }
    }
}
