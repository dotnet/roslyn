// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.CodeAnalysis
{
    public interface IUniversalCodingConventions
    {
        bool TryGetIndentStyle(out IndentStyle indentStyle);

        bool TryGetIndentSize(out int indentSize);

        bool TryGetTabWidth(out int tabWidth);

        bool TryGetLineEnding(out string lineEnding);

        bool TryGetEncoding(out Encoding encoding);

        bool TryGetAllowTrailingWhitespace(out bool allowTrailingWhitespace);

        bool TryGetRequireFinalNewline(out bool requireFinalNewline);
    }
}
