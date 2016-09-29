// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Xaml
{
    public static class XamlStaticTypeDefinitions
    {
        [Export]
        [Name(ContentTypeNames.XamlContentType)]
        [BaseDefinition(ContentTypeNames.RoslynContentType)]
        public static readonly ContentTypeDefinition XamlContentTypeDefinition;

        // Associate .xaml as the Xaml content type.
        [Export]
        [FileExtension(StringConstants.XamlFileExtension)]
        [ContentType(ContentTypeNames.XamlContentType)]
        internal static readonly FileExtensionToContentTypeDefinition XamlFileExtension;
    }
}
