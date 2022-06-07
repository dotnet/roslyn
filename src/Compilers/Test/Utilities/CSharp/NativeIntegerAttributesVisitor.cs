// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Returns a string with all symbols containing NativeIntegerAttributes.
    /// </summary>
    internal sealed class NativeIntegerAttributesVisitor : TestAttributesVisitor
    {
        internal static string GetString(PEModuleSymbol module)
        {
            var builder = new StringBuilder();
            var visitor = new NativeIntegerAttributesVisitor(builder);
            visitor.Visit(module);
            return builder.ToString();
        }

        private NativeIntegerAttributesVisitor(StringBuilder builder) : base(builder)
        {
        }

        protected override SymbolDisplayFormat DisplayFormat => SymbolDisplayFormat.TestFormatWithConstraints.
            WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeRef |
                SymbolDisplayMemberOptions.IncludeExplicitInterface).
            WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UseNativeIntegerUnderlyingType);

        protected override bool TypeRequiresAttribute(TypeSymbol? type) => type?.ContainsNativeIntegerWrapperType() == true;

        protected override CSharpAttributeData? GetTargetAttribute(ImmutableArray<CSharpAttributeData> attributes) =>
            GetAttribute(attributes, "System.Runtime.CompilerServices", "NativeIntegerAttribute");
    }
}
