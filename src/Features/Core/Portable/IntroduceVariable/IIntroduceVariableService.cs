// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.IntroduceVariable;

internal interface IIntroduceVariableService : ILanguageService
{
    Task<CodeAction> IntroduceVariableAsync(Document document, TextSpan textSpan, CodeCleanupOptions options, CancellationToken cancellationToken);
}
