// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Apply this annotation to an appropriate Syntax element to request that it should be 
/// navigated to by the user after a code action is applied.  If present the host should
/// try to place the user's caret at the beginning of the element.
/// </summary>
/// <remarks>
/// By using a <see cref="SyntaxAnnotation"/> this navigation location will be resilient
/// to the transformations performed by the <see cref="CodeAction"/> infrastructure.  
/// Namely it will be resilient to the formatting, reduction or case correction that
/// automatically occures.  This allows a code action to specify a desired location for
/// the user caret to be placed without knowing what actual position that location will
/// end up at when the action is finally applied.
/// </remarks>
internal static class NavigationAnnotation
{
    public const string Kind = "CodeAction_Navigation";

    public static SyntaxAnnotation Create()
        => new(Kind);
}
