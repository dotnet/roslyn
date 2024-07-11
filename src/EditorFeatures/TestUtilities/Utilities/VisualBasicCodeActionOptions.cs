// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.VisualBasic.CodeGeneration;
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
    };

    public static CodeActionOptions With(this CodeActionOptions options, VisualBasicSyntaxFormattingOptions value)
        => options with { CleanupOptions = options.CleanupOptions with { FormattingOptions = value } };

    public static CodeActionOptions With(this CodeActionOptions options, ImplementTypeOptions value)
        => options with { ImplementTypeOptions = value };
}
