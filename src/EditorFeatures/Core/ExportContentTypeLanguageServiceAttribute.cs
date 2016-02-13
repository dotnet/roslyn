// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            if (defaultContentType == null)
            {
                throw new ArgumentNullException(nameof(defaultContentType));
            }

            this.DefaultContentType = defaultContentType;
        }
    }
}
