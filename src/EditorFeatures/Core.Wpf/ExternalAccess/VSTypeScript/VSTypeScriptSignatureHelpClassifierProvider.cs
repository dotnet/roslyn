// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Export(typeof(IVSTypeScriptSignatureHelpClassifierProvider))]
    [Shared]
    internal sealed class VSTypeScriptSignatureHelpClassifierProvider
        : IVSTypeScriptSignatureHelpClassifierProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptSignatureHelpClassifierProvider()
        {
        }

        public IClassifier Create(ITextBuffer textBuffer, ClassificationTypeMap typeMap)
            => new SignatureHelpClassifier(textBuffer, typeMap);
    }
}
