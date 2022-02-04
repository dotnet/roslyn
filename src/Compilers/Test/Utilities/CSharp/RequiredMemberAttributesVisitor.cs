// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities;

internal class RequiredMemberAttributesVisitor : TestAttributesVisitor
{
    internal static string GetString(PEModuleSymbol module)
    {
        var builder = new StringBuilder();
        var visitor = new RequiredMemberAttributesVisitor(builder);
        visitor.Visit(module);
        return builder.ToString();
    }

    private RequiredMemberAttributesVisitor(StringBuilder builder) : base(builder)
    {
    }

    protected override SymbolDisplayFormat DisplayFormat => SymbolDisplayFormat.TestFormat;

    protected override CSharpAttributeData? GetTargetAttribute(ImmutableArray<CSharpAttributeData> attributes)
        => GetAttribute(attributes, "System.Runtime.CompilerServices", "RequiredMemberAttribute");

    protected override bool TypeRequiresAttribute(TypeSymbol? type)
    {
        return false;
    }
}
