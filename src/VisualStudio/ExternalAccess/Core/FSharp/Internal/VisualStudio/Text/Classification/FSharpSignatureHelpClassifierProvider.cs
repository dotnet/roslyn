// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.VisualStudio.Text.Classification;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.VisualStudio.Text.Classification;
#endif

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
