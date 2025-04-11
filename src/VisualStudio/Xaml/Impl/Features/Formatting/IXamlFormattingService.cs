// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Formatting;

internal interface IXamlFormattingService : ILanguageService
{
    Task<IList<TextChange>> GetFormattingChangesAsync(TextDocument document, XamlFormattingOptions options, TextSpan? textSpan, CancellationToken cancellationToken);
    Task<IList<TextChange>> GetFormattingChangesAsync(TextDocument document, XamlFormattingOptions options, char typedChar, int position, CancellationToken cancellationToken);
}
