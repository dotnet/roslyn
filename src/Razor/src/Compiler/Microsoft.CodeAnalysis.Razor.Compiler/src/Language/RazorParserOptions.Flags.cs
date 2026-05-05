// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorParserOptions
{
    [Flags]
    private enum Flags
    {
        DesignTime = 1 << 0,
        ParseLeadingDirectives = 1 << 1,
        UseRoslynTokenizer = 1 << 2,
        EnableSpanEditHandlers = 1 << 3,
        AllowMinimizedBooleanTagHelperAttributes = 1 << 4,
        AllowHtmlCommentsInTagHelpers = 1 << 5,
        AllowComponentFileKind = 1 << 6,
        AllowRazorInAllCodeBlocks = 1 << 7,
        AllowUsingVariableDeclarations = 1 << 8,
        AllowConditionalDataDashAttributes = 1 << 9,
        AllowCSharpInMarkupAttributeArea = 1 << 10,
        AllowNullableForgivenessOperator = 1 << 11
    }

    private static Flags GetDefaultFlags(RazorLanguageVersion languageVersion, RazorFileKind fileKind)
    {
        Flags result = 0;

        result.SetFlag(Flags.AllowCSharpInMarkupAttributeArea);

        if (languageVersion >= RazorLanguageVersion.Version_2_1)
        {
            // Added in 2.1
            result.SetFlag(Flags.AllowMinimizedBooleanTagHelperAttributes);
            result.SetFlag(Flags.AllowHtmlCommentsInTagHelpers);
        }

        if (languageVersion >= RazorLanguageVersion.Version_3_0)
        {
            // Added in 3.0
            result.SetFlag(Flags.AllowComponentFileKind);
            result.SetFlag(Flags.AllowRazorInAllCodeBlocks);
            result.SetFlag(Flags.AllowUsingVariableDeclarations);
            result.SetFlag(Flags.AllowNullableForgivenessOperator);
        }

        if (fileKind.IsComponent())
        {
            result.SetFlag(Flags.AllowConditionalDataDashAttributes);
            result.ClearFlag(Flags.AllowCSharpInMarkupAttributeArea);
        }

        if (languageVersion >= RazorLanguageVersion.Experimental)
        {
            result.SetFlag(Flags.AllowConditionalDataDashAttributes);
        }

        return result;
    }
}
