// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
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

    protected override void ReportSymbol(Symbol symbol)
    {
        EntityHandle handle;
        PEModule module;

        switch (symbol)
        {
            case PENamedTypeSymbol namedType:
                handle = namedType.Handle;
                module = ((PEModuleSymbol)namedType.ContainingModule).Module;
                break;

            case PEFieldSymbol field:
                handle = field.Handle;
                module = ((PEModuleSymbol)field.ContainingModule).Module;
                break;

            case PEPropertySymbol property:
                handle = property.Handle;
                module = ((PEModuleSymbol)property.ContainingModule).Module;
                break;

            default:
                base.ReportSymbol(symbol);
                return;
        }

        var attribute = module.GetAttributeHandle(handle, AttributeDescription.RequiredMemberAttribute);

        if (attribute.IsNil)
        {
            return;
        }

        ReportContainingSymbols(symbol);
        _builder.Append(GetIndentString(symbol));
        _builder.Append("[RequiredMember] ");
        _builder.AppendLine(symbol.ToDisplayString(DisplayFormat));
        _reported.Add(symbol);

        // If attributes aren't filtered out, this will print extra data and cause an error in test assertion.
        base.ReportSymbol(symbol);
    }

    protected override CSharpAttributeData? GetTargetAttribute(ImmutableArray<CSharpAttributeData> attributes)
        => GetAttribute(attributes, "System.Runtime.CompilerServices", "RequiredMemberAttribute");

    protected override bool TypeRequiresAttribute(TypeSymbol? type)
    {
        return false;
    }
}
