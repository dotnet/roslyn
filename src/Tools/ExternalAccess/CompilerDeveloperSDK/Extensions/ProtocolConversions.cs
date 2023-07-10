// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using PC = Microsoft.CodeAnalysis.LanguageServer.ProtocolConversions;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal static class ProtocolConversions
{
    public static LinePosition PositionToLinePosition(LSP.Position position) => PC.PositionToLinePosition(position);
    public static LinePositionSpan RangeToLinePositionSpan(LSP.Range range) => PC.RangeToLinePositionSpan(range);
    public static TextSpan RangeToTextSpan(LSP.Range range, SourceText text) => PC.RangeToTextSpan(range, text);
    public static LSP.Position LinePositionToPosition(LinePosition linePosition) => PC.LinePositionToPosition(linePosition);
    public static LSP.Range LinePositionToRange(LinePositionSpan linePositionSpan) => PC.LinePositionToRange(linePositionSpan);
    public static LSP.Range TextSpanToRange(TextSpan textSpan, SourceText text) => PC.TextSpanToRange(textSpan, text);
}
