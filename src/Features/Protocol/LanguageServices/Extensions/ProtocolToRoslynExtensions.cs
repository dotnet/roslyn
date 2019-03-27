// Copyright (c) Microsoft. All rights reserved.

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
