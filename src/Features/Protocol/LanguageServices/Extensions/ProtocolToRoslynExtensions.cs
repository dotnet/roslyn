// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Protocol.LanguageServices.Extensions
{
    internal static class ProtocolToRoslynExtensions
    {
        public static LinePosition ToLinePosition(this Position position)
        {
            return new LinePosition(position.Line, position.Character);
        }

        public static LinePositionSpan ToLinePositionSpan(this Range range)
        {
            return new LinePositionSpan(range.Start.ToLinePosition(), range.End.ToLinePosition());
        }

        public static TextSpan ToTextSpan(this Range range, SourceText text)
        {
            var linePositionSpan = range.ToLinePositionSpan();
            return text.Lines.GetTextSpan(linePositionSpan);
        }
    }
}
