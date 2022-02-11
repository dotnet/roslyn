// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines an attribute used to export instances of <see cref="IRequestHandlerProvider{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportLspRequestHandlerProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// The document languages that this handler supports.
        /// </summary>
        public string[] LanguageNames { get; }

        public ExportLspRequestHandlerProviderAttribute(params string[] languageNames) : base(typeof(IRequestHandlerProvider))
        {
            LanguageNames = languageNames;
        }
    }

    /// <summary>
    /// Defines an easy to use subclass for ExportLspRequestHandlerProviderAttribute that contains
    /// all the language names that the default Roslyn servers support.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportRoslynLanguagesLspRequestHandlerProviderAttribute : ExportLspRequestHandlerProviderAttribute
    {
        public ExportRoslynLanguagesLspRequestHandlerProviderAttribute() : base(ProtocolConstants.RoslynLspLanguages.ToArray())
        {
        }
    }
}
