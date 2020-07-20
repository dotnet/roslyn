// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Specifies the exact type of the service exported by the ILanguageService.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportContentTypeLanguageServiceAttribute : ExportLanguageServiceAttribute
    {
        public string DefaultContentType { get; set; }

        public ExportContentTypeLanguageServiceAttribute(string defaultContentType, string language, string layer = ServiceLayer.Default)
            : base(typeof(IContentTypeLanguageService), language, layer)
        {
            this.DefaultContentType = defaultContentType ?? throw new ArgumentNullException(nameof(defaultContentType));
        }
    }
}
