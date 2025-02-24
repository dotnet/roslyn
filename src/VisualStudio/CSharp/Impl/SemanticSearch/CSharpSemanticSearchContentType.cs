// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

internal static class CSharpSemanticSearchContentType
{
    public const string Name = "SemanticSearch-CSharp";

    [Export]
    [Name(Name)]
    [BaseDefinition(ContentTypeNames.CSharpContentType)]
    public static readonly ContentTypeDefinition Definition = null!;
}
