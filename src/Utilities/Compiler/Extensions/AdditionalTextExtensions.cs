// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities.Extensions
{
    internal static class AdditionalTextExtensions
    {
        private static readonly Encoding s_utf8bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        private static readonly SourceText s_emptySourceText = SourceText.From("", s_utf8bom, SourceHashAlgorithm.Sha256);

        public static SourceText GetTextOrEmpty(this AdditionalText text, CancellationToken cancellationToken)
            => text.GetText(cancellationToken) ?? s_emptySourceText;
    }
}
