// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

/// <summary>
/// The text style for a <see cref="QuickInfoClassifiedTextRun"/>.
/// </summary>
/// 
/// <remarks>
/// By default, text is displayed using tooltip preferences, but colorized using
/// text editor colors in order to make tooltips that look visually like UI, but
/// match the semantic colorization of the code.
/// </remarks>
[Flags]
internal enum QuickInfoClassifiedTextStyle
{
    /// <summary>
    /// Plain text.
    /// </summary>
    Plain = 0,

    /// <summary>
    /// Bolded text.
    /// </summary>
    Bold = 1 << 0,

    /// <summary>
    /// Italic text.
    /// </summary>
    Italic = 1 << 1,

    /// <summary>
    /// Underlined text.
    /// </summary>
    Underline = 1 << 2,

    /// <summary>
    /// Use the font specified by the classification.
    /// </summary>
    /// 
    /// <remarks>
    /// If applied, the classification's code font is used instead of the default tooltip font.
    /// </remarks>
    UseClassificationFont = 1 << 3,

    /// <summary>
    /// Use the style specified by the classification.
    /// </summary>
    /// 
    /// <remarks>
    /// If applied, the classification's bold, italic, and underline settings are used
    /// instead of the default tooltip style. Note that additional styles can be layered
    /// on top of the classification's style by adding <see cref="Bold"/>, <see cref="Italic"/>,
    /// or <see cref="Underline"/>.
    /// </remarks>
    UseClassificationStyle = 1 << 4
}
