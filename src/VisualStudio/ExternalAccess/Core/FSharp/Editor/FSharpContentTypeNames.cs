// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal static class FSharpContentTypeNames
{
    public const string RoslynContentType = Microsoft.CodeAnalysis.Editor.ContentTypeNames.RoslynContentType;
    public const string FSharpContentType = CodeAnalysis.Editor.ContentTypeNames.FSharpContentType;
    public const string FSharpSignatureHelpContentType = CodeAnalysis.Editor.ContentTypeNames.FSharpSignatureHelpContentType;
}
