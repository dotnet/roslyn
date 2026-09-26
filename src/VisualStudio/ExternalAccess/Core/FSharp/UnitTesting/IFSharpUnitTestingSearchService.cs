// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTesting;

/// <summary>
/// Identifies a test by the metadata name a test adapter reports it under, following
/// https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md.
/// </summary>
internal readonly struct FSharpUnitTestingSearchQuery(
    string fullyQualifiedTypeName, string? methodName, int methodArity, int methodParameterCount, bool strict)
{
    /// <summary>
    /// Fully qualified metadata name of the type, or of the type containing <see cref="MethodName"/>, using
    /// <c>`</c> for arity and <c>+</c> for nesting.
    /// </summary>
    public string FullyQualifiedTypeName { get; } = fullyQualifiedTypeName;

    /// <summary>
    /// Name of the method being searched for, without arity.  Null when a type is being searched for.
    /// </summary>
    public string? MethodName { get; } = methodName;

    public int MethodArity { get; } = methodArity;

    public int MethodParameterCount { get; } = methodParameterCount;

    /// <summary>
    /// Whether arity and parameter count have to match.  Adapters that do not round-trip metadata names cleanly
    /// produce non-strict queries.
    /// </summary>
    public bool Strict { get; } = strict;
}

internal readonly struct FSharpUnitTestingSourceLocation(Document document, TextSpan sourceSpan, FileLinePositionSpan mappedSpan)
{
    public Document Document { get; } = document;

    public TextSpan SourceSpan { get; } = sourceSpan;

    /// <summary>
    /// Where the test ends up after <c>#line</c> mapping.  Pass the unmapped location when nothing remaps it.
    /// </summary>
    public FileLinePositionSpan MappedSpan { get; } = mappedSpan;
}

/// <summary>
/// Answers where a test named by a test adapter lives in F# source.  Live Unit Testing and Test Explorer ask this
/// because F# projects carry no Roslyn compilation for the shared declared-symbol search to use.
/// </summary>
internal interface IFSharpUnitTestingSearchService
{
    Task<ImmutableArray<FSharpUnitTestingSourceLocation>> GetSourceLocationsAsync(
        Project project, FSharpUnitTestingSearchQuery query, CancellationToken cancellationToken);
}
