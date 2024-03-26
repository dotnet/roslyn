// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.AutomaticCompletion;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;

[ExportLanguageService(typeof(IBraceCompletionServiceFactory), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CSharpBraceCompletionServiceFactory(
    [ImportMany(LanguageNames.CSharp)] IEnumerable<IBraceCompletionService> braceCompletionServices) : AbstractBraceCompletionServiceFactory(braceCompletionServices)
{
}
