// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    internal sealed class AbstractContainedLanguage
    {
        public AbstractContainedLanguage(IVsContainedLanguageHost host)
            => ContainedLanguageHost = host;

        public IVsContainedLanguageHost ContainedLanguageHost { get; }
    }
}
