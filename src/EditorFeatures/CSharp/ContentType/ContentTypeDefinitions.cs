// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
