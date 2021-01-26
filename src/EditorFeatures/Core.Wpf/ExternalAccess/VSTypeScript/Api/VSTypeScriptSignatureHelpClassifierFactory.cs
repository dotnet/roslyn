// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    [Export]
    internal class VSTypeScriptSignatureHelpClassifierFactory
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptSignatureHelpClassifierFactory(ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public IClassifier Create(ITextBuffer textBuffer)
            => new SignatureHelpClassifier(textBuffer, _typeMap);
    }
}
