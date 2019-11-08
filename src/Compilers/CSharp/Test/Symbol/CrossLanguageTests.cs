// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CrossLanguageTests : CSharpTestBase
    {
        [Fact]
        public void CanBeReferencedByName()
        {
            var vbText = @"
Public Interface I
    Property P(x As Integer)
End Interface
";
            var vbcomp = VisualBasicCompilation.Create(
                "Test",
                new[] { VisualBasicSyntaxTree.ParseText(vbText) },
                new[] { MscorlibRef_v4_0_30316_17626 },
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var ref1 = vbcomp.EmitToImageReference(embedInteropTypes: true);

            var text = @"class C : I {}";
            var tree = Parse(text);
            var comp = CreateEmptyCompilation(new[] { tree }, new[] { ref1 });

            var t = comp.GetTypeByMetadataName("I");
            Assert.Empty(t.GetMembersUnordered().Where(x => x is { Kind: SymbolKind.Method, CanBeReferencedByName: false }));
            Assert.False(t.GetMembersUnordered().Where(x => x.Kind == SymbolKind.Property).First().CanBeReferencedByName); //there's only one.
        }
    }
}
