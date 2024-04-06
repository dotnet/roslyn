// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.CodeGeneration;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Shared.Lightup;

internal static class NullableSyntaxAnnotationEx
{
    public static SyntaxAnnotation? Oblivious { get; }
    public static SyntaxAnnotation? AnnotatedOrNotAnnotated { get; }

    static NullableSyntaxAnnotationEx()
    {
        var nullableSyntaxAnnotation = typeof(Workspace).Assembly.GetType("Microsoft.CodeAnalysis.CodeGeneration.NullableSyntaxAnnotation", throwOnError: false);
        if (nullableSyntaxAnnotation is object)
        {
            Oblivious = (SyntaxAnnotation?)nullableSyntaxAnnotation.GetField(nameof(Oblivious), BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            AnnotatedOrNotAnnotated = (SyntaxAnnotation?)nullableSyntaxAnnotation.GetField(nameof(AnnotatedOrNotAnnotated), BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
        }

#if !CODE_STYLE
        Contract.ThrowIfFalse(ReferenceEquals(Oblivious, NullableSyntaxAnnotation.Oblivious));
        Contract.ThrowIfFalse(ReferenceEquals(AnnotatedOrNotAnnotated, NullableSyntaxAnnotation.AnnotatedOrNotAnnotated));
#endif
    }
}
