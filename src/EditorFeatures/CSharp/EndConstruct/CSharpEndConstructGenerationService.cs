// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EndConstructGeneration;

[ExportLanguageService(typeof(IEndConstructGenerationService), LanguageNames.CSharp), Shared]
[ExcludeFromCodeCoverage]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEndConstructGenerationService() : IEndConstructGenerationService
{
    public async Task<bool> TryDoAsync(ITextView textView, ITextBuffer subjectBuffer, char typedChar, CancellationToken cancellationToken)
        => false;
}
