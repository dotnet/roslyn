// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Xaml
{
    public static class XamlStaticTypeDefinitions
    {
        /// <summary>
        /// Definition of the XAML content type.
        /// </summary>
        [Export]
        [Name(ContentTypeNames.XamlContentType)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static readonly ContentTypeDefinition XamlContentType;

        // Associate .xaml as the Xaml content type.
        [Export]
        [FileExtension(StringConstants.XamlFileExtension)]
        [ContentType(ContentTypeNames.XamlContentType)]
        internal static readonly FileExtensionToContentTypeDefinition XamlFileExtension;
    }
}
