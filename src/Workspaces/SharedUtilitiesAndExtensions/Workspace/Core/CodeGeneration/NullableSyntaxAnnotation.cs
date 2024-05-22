// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// Annotation placed on <see cref="ITypeSymbol"/>s that the <see cref="SyntaxGenerator"/> converts to a node. This
/// information tracks the original nullable state of the symbol and is used by metadata-as-source to determine if
/// it needs to add <c>#nullable</c> directives in the file.
/// </summary>
internal sealed class NullableSyntaxAnnotation
{
    /// <summary>
    /// For <c>string~</c> types.
    /// </summary>
    public static readonly SyntaxAnnotation Oblivious = new($"{nameof(NullableSyntaxAnnotation)}.{Oblivious}");
    /// <summary>
    /// For <c>string!</c> or <c>string?</c> types.
    /// </summary>
    public static readonly SyntaxAnnotation AnnotatedOrNotAnnotated = new($"{nameof(NullableSyntaxAnnotation)}.{AnnotatedOrNotAnnotated}");
}
