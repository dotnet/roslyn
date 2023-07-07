// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class RoslynRequestExecutionQueue : RequestExecutionQueue<RequestContext>
    {
        private readonly IInitializeManager _initializeManager;

        public RoslynRequestExecutionQueue(AbstractLanguageServer<RequestContext> languageServer, ILspLogger logger, IHandlerProvider handlerProvider)
            : base(languageServer, logger, handlerProvider)
        {
            _initializeManager = languageServer.GetLspServices().GetRequiredService<IInitializeManager>();
        }

        public override Task WrapStartRequestTaskAsync(Task nonMutatingRequestTask, bool rethrowExceptions)
        {
            TrySetLocale();
            if (rethrowExceptions)
            {
                return nonMutatingRequestTask;
            }
            else
            {
                return nonMutatingRequestTask.ReportNonFatalErrorAsync();
            }
        }

        private void TrySetLocale()
        {
            var locale = _initializeManager.TryGetInitializeParams()?.Locale;
            // The client may not have given us a UI or this is the initialize request and we haven't saved it yet.
            if (!string.IsNullOrWhiteSpace(locale))
            {
                try
                {
                    var desiredUICulture = CultureInfo.CreateSpecificCulture(locale);
                    CultureInfo.CurrentUICulture = desiredUICulture;
                }
                catch (CultureNotFoundException)
                {
                    _logger.LogWarning($"Culture {locale} was not found, falling back to OS culture");
                }
            }
        }
    }
}
