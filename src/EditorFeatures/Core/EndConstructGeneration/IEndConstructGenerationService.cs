// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration;

internal interface IEndConstructGenerationService : ILanguageService
{
    Task<bool> TryDoAsync(ITextView textView, ITextBuffer subjectBuffer, char typedChar, CancellationToken cancellationToken);
}
