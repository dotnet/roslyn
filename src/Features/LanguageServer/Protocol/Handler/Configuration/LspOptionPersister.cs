// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    internal class LspOptionPersister : IOptionPersister
    {
        public bool TryFetch(OptionKey optionKey, out object? value)
        {
            throw new NotImplementedException();
        }

        public bool TryPersist(OptionKey optionKey, object? value)
        {
            throw new NotImplementedException();
        }
    }
}
