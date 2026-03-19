// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ContentType;

internal static class ContentTypeDefinitions
{
    /// <summary>
    /// Definition of the primary C# content type.
    /// </summary>
    [Export]
    [Name(ContentTypeNames.CSharpContentType)]
    [BaseDefinition(ContentTypeNames.RoslynContentType)]
    // Adds the LSP base content type to ensure the LSP client activates on C# files.
    // From Microsoft.VisualStudio.LanguageServer.Client.CodeRemoteContentDefinition.CodeRemoteBaseTypeName
    // We cannot directly reference the LSP client package in EditorFeatures as it is a VS dependency.
    [BaseDefinition("code-languageserver-base")]
    public static readonly ContentTypeDefinition CSharpContentTypeDefinition = null!;

    [Export]
    [Name(ContentTypeNames.CSharpSignatureHelpContentType)]
    [BaseDefinition("sighelp")]
    public static readonly ContentTypeDefinition SignatureHelpContentTypeDefinition = null!;
}
