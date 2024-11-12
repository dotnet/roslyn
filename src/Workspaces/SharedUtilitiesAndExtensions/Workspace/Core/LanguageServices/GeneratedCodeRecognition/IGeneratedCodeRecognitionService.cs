// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.GeneratedCodeRecognition;

internal interface IGeneratedCodeRecognitionService : ILanguageService
{
#if !CODE_STYLE
    bool IsGeneratedCode(Document document, CancellationToken cancellationToken);
#endif

    Task<bool> IsGeneratedCodeAsync(Document document, CancellationToken cancellationToken);
}
