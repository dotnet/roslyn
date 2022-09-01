// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines a custom view to allow MEF to import the correct handlers via metadata.
    /// This is a work around for MEF by default being unable to handle an attribute with AllowMultiple = true
    /// defined only once on a class.
    /// </summary>
    internal class RequestHandlerProviderMetadataView
    {
        public ImmutableArray<Type> HandlerTypes { get; set; }

        public RequestHandlerProviderMetadataView(IDictionary<string, object> metadata)
        {
            var handlerMetadata = (Type[])metadata[nameof(ExportLspRequestHandlerProviderAttribute.HandlerTypes)];
            HandlerTypes = handlerMetadata.ToImmutableArray();
        }
    }
}
