// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines an attribute used to export instances of <see cref="AbstractRequestHandlerProvider"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportLspRequestHandlerProviderAttribute : ExportAttribute
    {
        public Type[] HandlerTypes { get; }

        public ExportLspRequestHandlerProviderAttribute(string contractName, Type first, params Type[] handlerTypes) : base(contractName, typeof(AbstractRequestHandlerProvider))
        {
            HandlerTypes = handlerTypes.Concat(new[] { first }).ToArray();
        }
    }

    /// <summary>
    /// Defines an easy to use subclass for ExportLspRequestHandlerProviderAttribute that contains
    /// all the language names that the default Roslyn servers support.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportRoslynLanguagesLspRequestHandlerProviderAttribute : ExportLspRequestHandlerProviderAttribute
    {
        public ExportRoslynLanguagesLspRequestHandlerProviderAttribute(Type first, params Type[] handlerTypes) : base(ProtocolConstants.RoslynLspLanguagesContract, first, handlerTypes)
        {
        }
    }
}
