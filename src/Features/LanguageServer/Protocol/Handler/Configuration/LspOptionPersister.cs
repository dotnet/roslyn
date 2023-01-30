// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    internal class LspOptionPersister : IOptionPersister
    {
        private readonly IGlobalOptionService _globalOptionService;

        public LspOptionPersister(IGlobalOptionService globalOptionService)
        {
            _globalOptionService = globalOptionService;
        }

        public bool TryFetch(OptionKey2 optionKey, out object? value)
        {
            throw new NotImplementedException();
        }

        public bool TryPersist(OptionKey2 optionKey, object? value)
        {
            throw new NotImplementedException("LSP does not support write option from server to client");
        }
    }
}
