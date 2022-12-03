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
    [Export(typeof(IOptionPersister)), Shared]
    internal class LspOptionPersister : IOptionPersister
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LspOptionPersister() { }

        public bool TryFetch(OptionKey optionKey, out object? value)
        {
            // Whenever a service try to read a option, it should hit the cache.
            throw ExceptionUtilities.Unreachable();
        }

        public bool TryPersist(OptionKey optionKey, object? value)
        {
            throw new NotImplementedException("LSP doesn't support write option from server to client");
        }
    }
}
