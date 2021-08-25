// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.AddFileBanner;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.FileHeaders;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FileHeaders;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.AddFileBanner
{
    [ExportNewDocumentFormattingProvider(LanguageNames.CSharp), Shared]
    internal class CSharpAddFileBannerNewDocumentFormattingProvider : AbstractAddFileBannerNewDocumentFormattingProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpAddFileBannerNewDocumentFormattingProvider()
        {
        }

        protected override SyntaxGenerator SyntaxGenerator => CSharpSyntaxGenerator.Instance;
        protected override SyntaxGeneratorInternal SyntaxGeneratorInternal => CSharpSyntaxGeneratorInternal.Instance;
        protected override AbstractFileHeaderHelper FileHeaderHelper => CSharpFileHeaderHelper.Instance;
    }
}
