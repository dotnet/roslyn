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

        /// <summary>
        /// Exports an <see cref="AbstractRequestHandlerProvider"/> and specifies the contract and
        /// <see cref="IRequestHandler"/> types this provider is associated with.
        /// </summary>
        /// <param name="contractName">
        /// The contract name this provider is exported.  Used by <see cref="AbstractRequestDispatcherFactory"/>
        /// when importing handlers to ensure that it only imports handlers that match this contract.
        /// This is important to ensure that we only load relevant providers (e.g. don't load Xaml providers when creating the c# server),
        /// otherwise we will get dll load RPS regressions for the <see cref="HandlerTypes"/>
        /// </param>
        /// <param name="firstHandlerType">
        /// The concrete type of the <see cref="IRequestHandler"/> provided in <see cref="AbstractRequestHandlerProvider.CreateRequestHandlers(WellKnownLspServerKinds)"/>
        /// </param>
        /// <param name="additionalHandlerTypes">
        /// Additional <see cref="IRequestHandler"/> if <see cref="AbstractRequestHandlerProvider.CreateRequestHandlers(WellKnownLspServerKinds)"/>
        /// provides more than one handler at once.
        /// </param>
        public ExportLspRequestHandlerProviderAttribute(string contractName, Type firstHandlerType, params Type[] additionalHandlerTypes) : base(contractName, typeof(AbstractRequestHandlerProvider))
        {
            HandlerTypes = additionalHandlerTypes.Concat(new[] { firstHandlerType }).ToArray();
        }
    }

    /// <summary>
    /// Defines an easy to use subclass for ExportLspRequestHandlerProviderAttribute with the roslyn languages contract name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportRoslynLanguagesLspRequestHandlerProviderAttribute : ExportLspRequestHandlerProviderAttribute
    {
        public ExportRoslynLanguagesLspRequestHandlerProviderAttribute(Type firstHandlerType, params Type[] additionalHandlerTypes) : base(ProtocolConstants.RoslynLspLanguagesContract, firstHandlerType, additionalHandlerTypes)
        {
        }
    }
}
