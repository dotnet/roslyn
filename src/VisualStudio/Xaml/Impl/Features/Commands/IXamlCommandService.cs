// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Commands;

internal interface IXamlCommandService : ILanguageService
{
    /// <summary>
    /// Execute the <paramref name="command"/> with the <paramref name="commandArguments"/>
    /// </summary>
    /// <param name="document">TextDocument command was triggered on</param>
    /// <param name="command">The command that will be executed</param>
    /// <param name="commandArguments">The arguments need by the command</param>
    /// <param name="cancellationToken">cancellationToken</param>
    /// <returns>True if the command has been executed, otherwise false</returns>
    Task<bool> ExecuteCommandAsync(TextDocument document, string command, object[]? commandArguments, CancellationToken cancellationToken);
}
