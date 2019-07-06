// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal
{
    internal static class FSharpContentTypeDefinitions
    {
        [Export]
        [Name(FSharpContentTypeNames.FSharpContentType)]
        [BaseDefinition(FSharpContentTypeNames.RoslynContentType)]
        public static readonly ContentTypeDefinition FSharpContentTypeDefinition;

        [Export]
        [Name(FSharpContentTypeNames.FSharpSignatureHelpContentType)]
        [BaseDefinition("sighelp")]
        public static readonly ContentTypeDefinition FSharpSignatureHelpContentTypeDefinition;
    }
}
