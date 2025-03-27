// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Text.Adornments;

//
// Summary:
//     The text style for a Microsoft.VisualStudio.Text.Adornments.ClassifiedTextRun.
//
// Remarks:
//     By default, text is displayed using tooltip preferences, but colorized using
//     text editor colors in order to make tooltips that look visually like UI, but
//     match the semantic colorization of the code.
[Flags]
internal enum ClassifiedTextRunStyle
{
    //
    // Summary:
    //     Plain text.
    Plain = 0x0,
    //
    // Summary:
    //     Bolded text.
    Bold = 0x1,
    //
    // Summary:
    //     Italic text.
    Italic = 0x2,
    //
    // Summary:
    //     Underlined text.
    Underline = 0x4,
    //
    // Summary:
    //     Use the font specified by the classification.
    //
    // Remarks:
    //     If applied, the classification's code font is used instead of the default tooltip
    //     font.
    UseClassificationFont = 0x8,
    //
    // Summary:
    //     Use the style specified by the classification.
    //
    // Remarks:
    //     If applied, the classification's bold, italic, and underline settings are used
    //     instead of the default tooltip style. Note that additional styles can be layered
    //     on top of the classification's style by adding Microsoft.VisualStudio.Text.Adornments.ClassifiedTextRunStyle.Bold,
    //     Microsoft.VisualStudio.Text.Adornments.ClassifiedTextRunStyle.Italic, or Microsoft.VisualStudio.Text.Adornments.ClassifiedTextRunStyle.Underline.
    UseClassificationStyle = 0x10
}