﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.VisualBasic.CodeGeneration;
using Microsoft.CodeAnalysis.VisualBasic.CodeStyle;
using Microsoft.CodeAnalysis.VisualBasic.Formatting;
using Microsoft.CodeAnalysis.VisualBasic.Simplification;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal static class VisualBasicCodeActionOptions
{
    public static CodeActionOptions Default = new()
    {
        CleanupOptions = new()
        {
            FormattingOptions = VisualBasicSyntaxFormattingOptions.Default,
            SimplifierOptions = VisualBasicSimplifierOptions.Default,
        },
        CodeGenerationOptions = VisualBasicCodeGenerationOptions.Default,
        CodeStyleOptions = VisualBasicIdeCodeStyleOptions.Default
    };

    public static CodeActionOptions WithWrappingColumn(this CodeActionOptions options, int value)
        => options with { WrappingColumn = value };

    public static CodeActionOptions With(this CodeActionOptions options, VisualBasicSyntaxFormattingOptions value)
        => options with { CleanupOptions = options.CleanupOptions with { FormattingOptions = value } };

    public static CodeActionOptions With(this CodeActionOptions options, ImplementTypeOptions value)
        => options with { ImplementTypeOptions = value };
}
