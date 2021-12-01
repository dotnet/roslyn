// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing
{
    /// <summary>
    /// An abstraction over an editorconfig option that reprsents some type <typeparamref name="T"/> and the span in which that option was defined.
    /// </summary>
    internal record class EditorConfigOption<T>(T Value, TextSpan? Span, Section Section)
        : EditorConfigOption(Section, Span)
    {
        public static implicit operator T(EditorConfigOption<T> option) => option.Value;
        public static implicit operator EditorConfigOption<T>((Section section, TextSpan? span, T value) tuple)
            => new(tuple.value, tuple.span, tuple.section);
        public static implicit operator EditorConfigOption<T>((Section section, T value) tuple)
            => new(tuple.value, null, tuple.section);
    }
}
