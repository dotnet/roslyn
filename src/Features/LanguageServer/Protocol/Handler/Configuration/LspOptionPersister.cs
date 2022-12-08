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
        public LspOptionPersister() { }

        public bool TryFetch(OptionKey optionKey, out object? value)
        {
            value = null;
            return false;
        }

        public bool TryPersist(OptionKey optionKey, object? value)
        {
            value = null;
            return false;
        }
    }
}
