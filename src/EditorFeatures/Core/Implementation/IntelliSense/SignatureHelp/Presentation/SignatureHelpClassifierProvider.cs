// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(ContentTypeNames.CSharpSignatureHelpContentType)]
    [ContentType(ContentTypeNames.VisualBasicSignatureHelpContentType)]
    internal partial class SignatureHelpClassifierProvider : IClassifierProvider
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public SignatureHelpClassifierProvider(ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public IClassifier GetClassifier(ITextBuffer subjectBuffer)
        {
            return subjectBuffer.Properties.GetOrCreateSingletonProperty<IClassifier>(
                () => new SignatureHelpClassifier(subjectBuffer, _typeMap));
        }
    }
}
