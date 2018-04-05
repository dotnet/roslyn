// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ContentType
{
    internal static class ContentTypeDefinitions
    {
        /// <summary>
        /// Definition of the primary C# content type.
        /// </summary>
        [Export]
        [Name(ContentTypeNames.CSharpContentType)]
        [BaseDefinition(ContentTypeNames.RoslynContentType)]
        public static readonly ContentTypeDefinition CSharpContentTypeDefinition;

        [Export]
        [Name(ContentTypeNames.CSharpSignatureHelpContentType)]
        [BaseDefinition("sighelp")]
        public static readonly ContentTypeDefinition SignatureHelpContentTypeDefinition;
    }
}
