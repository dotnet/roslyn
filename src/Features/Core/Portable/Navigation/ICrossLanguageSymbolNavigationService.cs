// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Navigation;

/// <summary>
/// Service to allow 3rd party languages to handle navigating to some metadata symbol.  For example, this can allow
/// a language like F# to navigate to its source definition of a symbol that roslyn views as a metadata symbol.
/// </summary>
internal interface ICrossLanguageSymbolNavigationService
{
    /// <summary>
    /// Attempts to get location that can be navigated to for a particular symbol id.  The symbol id format is
    /// defined at: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings.  Should
    /// return <see langword="null"/> if the 3rd party language cannot navigate to this particular symbol.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly the symbol was defined in. Can be used by the
    /// receiver to quickly filter down to the project/compilation search for the symbol.</param>
    Task<INavigableLocation?> TryGetNavigableLocationAsync(
        string assemblyName, string documentationCommentId, CancellationToken cancellationToken);
}
