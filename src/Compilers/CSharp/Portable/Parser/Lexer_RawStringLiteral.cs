// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        private void ScanRawStringLiteral(ref TokenInfo info)
        {
            _builder.Length = 0;

            var quoteCount = 0;
            while (TextWindow.PeekChar() == '"')
            {
                quoteCount++;
                TextWindow.AdvanceChar();
            }

            Debug.Assert(quoteCount >= 3);
            var isSingleLine = true;
            while (true)
            {
                var currentChar = TextWindow.PeekChar();

                // See if we reached the end of the file.
                if (currentChar == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd())
                {
                    Debug.Assert(TextWindow.Width > 0);
                    this.AddError(ErrorCode.ERR_NewlineInConst);
                    break;
                }

                if (SyntaxFacts.IsNewLine(currentChar))
                {
                    isSingleLine = false;
                    break;
                }
            }

            info.Text = TextWindow.GetText(true);
            info.Kind = SyntaxKind.RawStringLiteralToken;
            info.StringValue = _builder.Length > 0 ? TextWindow.Intern(_builder) : string.Empty;
        }
    }
}
