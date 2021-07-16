// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal
{
    internal static class FSharpContentTypeDefinitions
    {
        [Export]
        [Name(FSharpContentTypeNames.FSharpContentType)]
        [BaseDefinition(FSharpContentTypeNames.RoslynContentType)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        public static readonly ContentTypeDefinition FSharpContentTypeDefinition;

        [Export]
        [Name(FSharpContentTypeNames.FSharpSignatureHelpContentType)]
        [BaseDefinition("sighelp")]
        public static readonly ContentTypeDefinition FSharpSignatureHelpContentTypeDefinition;
    }
}
