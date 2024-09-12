// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal interface IVSTypeScriptSignatureHelpClassifierProvider
{
    IClassifier Create(ITextBuffer textBuffer, ClassificationTypeMap typeMap);
}
