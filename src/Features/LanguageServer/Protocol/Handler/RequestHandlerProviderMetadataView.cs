// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines a custom view to allow MEF to import the correct handlers via metadata.
    /// This is a work around for MEF by default being unable to handle an attribute with AllowMultiple = true
    /// defined only once on a class.
    /// </summary>
    internal class RequestHandlerProviderMetadataView
    {
        public string[] LanguageNames { get; set; }

        public string[] Methods { get; set; }

        public RequestHandlerProviderMetadataView(IDictionary<string, object> metadata)
        {
            var methodMetadata = metadata["Method"];
            Methods = ConvertMetadataToArray(methodMetadata);

            var languageMetadata = metadata["LanguageNames"];
            LanguageNames = ConvertMetadataToArray(languageMetadata);
        }

        /// <summary>
        /// When multiple of the same attribute are defined on a class, the metadata
        /// is aggregated into an array.  However, when just one of the same attribute is defined,
        /// the metadata is not aggregated and is just a string.
        /// MEF cannot construct the metadata object when it sees just the string type with AllowMultiple = true,
        /// so we override and construct it ourselves here.
        /// </summary>
        private static string[] ConvertMetadataToArray(object metadata)
        {
            if (metadata is string[] arrayData)
            {
                return arrayData;
            }
            else
            {
                return new string[] { (string)metadata };
            }
        }
    }
}
