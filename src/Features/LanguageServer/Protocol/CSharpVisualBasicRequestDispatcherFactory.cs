﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Shared]
    [Export(typeof(CSharpVisualBasicRequestDispatcherFactory))]
    internal sealed class CSharpVisualBasicRequestDispatcherFactory : AbstractRequestDispatcherFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVisualBasicRequestDispatcherFactory([ImportMany] IEnumerable<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders)
            : base(requestHandlerProviders)
        {
        }
    }
}
