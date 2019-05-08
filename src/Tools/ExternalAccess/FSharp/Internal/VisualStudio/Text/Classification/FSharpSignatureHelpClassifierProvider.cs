// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.VisualStudio.Text.Classification
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(FSharpContentTypeNames.FSharpSignatureHelpContentType)]
    internal class FSharpSignatureHelpClassifierProvider : IClassifierProvider
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpSignatureHelpClassifierProvider(ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new SignatureHelpClassifier(textBuffer, _typeMap);
        }
    }
}
