// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines an attribute used to export instances of <see cref="AbstractRequestHandlerProvider"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportLspRequestHandlerProviderAttribute : ExportAttribute
    {
        public string? LanguageName { get; }

        public ExportLspRequestHandlerProviderAttribute(string? languageName = null) : base(typeof(AbstractRequestHandlerProvider))
        {
            LanguageName = languageName;
        }
    }
}
