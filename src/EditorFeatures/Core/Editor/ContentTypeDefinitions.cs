// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ContentTypes;

internal static class ContentTypeDefinitions
{
    /// <summary>
    /// Definition of a content type that is a base definition for all content types supported by Roslyn.
    /// </summary>
    [Export]
    [Name(ContentTypeNames.RoslynContentType)]
    [BaseDefinition("code")]
    public static readonly ContentTypeDefinition RoslynContentTypeDefinition;
}
