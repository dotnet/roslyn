// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.DotNet.FileBasedPrograms;

internal interface ILanguageService
{
    /// <param name="reportAllErrors">
    /// If <see langword="true"/>, the whole <paramref name="sourceFile"/> is parsed to find diagnostics about every app directive.
    /// Otherwise, only directives up to the first C# token is checked.
    /// The former is useful for <c>dotnet project convert</c> where we want to report all errors because it would be difficult to fix them up after the conversion.
    /// The latter is useful for <c>dotnet run file.cs</c> where if there are app directives after the first token,
    /// compiler reports <c>ErrorCode.ERR_PPIgnoredFollowsToken</c> anyway, so we speed up success scenarios by not parsing the whole file up front in the SDK CLI.
    /// </param>
    ImmutableArray<CSharpDirective> FindDirectives(
        SourceFile sourceFile,
        bool reportAllErrors,
        ErrorReporter errorReporter,
        bool checkDuplicates = true);
}
