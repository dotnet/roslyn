﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Completion
{
    [ExportCompletionProvider("CSharpLspCompletionProvider", StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspCompletionProvider : RoslynCompletionProvider
    {
        [ImportingConstructor]
        public CSharpLspCompletionProvider(RoslynLspClientServiceFactory roslynLspClientServiceFactory)
            : base(roslynLspClientServiceFactory)
        {
        }
    }

    [ExportCompletionProvider("VBLspCompletionProvider", StringConstants.VBLspLanguageName), Shared]
    internal class VBLspCompletionProvider : RoslynCompletionProvider
    {
        [ImportingConstructor]
        public VBLspCompletionProvider(RoslynLspClientServiceFactory roslynLspClientServiceFactory)
            : base(roslynLspClientServiceFactory)
        {
        }
    }
}
