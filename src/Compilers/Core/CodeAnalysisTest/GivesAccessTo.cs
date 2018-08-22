// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class GivesAccessTo
    {
        [Fact, WorkItem(26459, "https://github.com/dotnet/roslyn/issues/26459")]
        public void TestGivesAccessTo_CrossLanguageAndCompilation()
        {
            var csharpTree = CSharpSyntaxTree.ParseText(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""VB"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS2"")]
internal class CS
{
}
");
            var csharpTree2 = CSharpSyntaxTree.ParseText(@"
internal class CS2
{
}
");
            var vbTree = VisualBasicSyntaxTree.ParseText(@"
<assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS"")>
<assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""VB2"")>
Friend Class VB
End Class
");
            var vbTree2 = VisualBasicSyntaxTree.ParseText(@"
Friend Class VB2
End Class
");
            var csc = CSharpCompilation.Create("CS", new[] { csharpTree }, new MetadataReference[] { TestBase.MscorlibRef });
            var CS = csc.GlobalNamespace.GetMembers("CS")[0] as INamedTypeSymbol;

            var csc2 = CSharpCompilation.Create("CS2", new[] { csharpTree2 }, new MetadataReference[] { TestBase.MscorlibRef });
            var CS2 = csc2.GlobalNamespace.GetMembers("CS2")[0] as INamedTypeSymbol;

            var vbc = VisualBasicCompilation.Create("VB", new[] { vbTree }, new MetadataReference[] { TestBase.MscorlibRef });
            var VB = vbc.GlobalNamespace.GetMembers("VB")[0] as INamedTypeSymbol;

            var vbc2 = VisualBasicCompilation.Create("VB2", new[] { vbTree2 }, new MetadataReference[] { TestBase.MscorlibRef });
            var VB2 = vbc2.GlobalNamespace.GetMembers("VB2")[0] as INamedTypeSymbol;

            Assert.True(CS.ContainingAssembly.GivesAccessTo(CS2.ContainingAssembly));
            Assert.True(CS.ContainingAssembly.GivesAccessTo(VB.ContainingAssembly));
            Assert.False(CS.ContainingAssembly.GivesAccessTo(VB2.ContainingAssembly));

            Assert.True(VB.ContainingAssembly.GivesAccessTo(VB2.ContainingAssembly));
            Assert.True(VB.ContainingAssembly.GivesAccessTo(CS.ContainingAssembly));
            Assert.False(VB.ContainingAssembly.GivesAccessTo(CS2.ContainingAssembly));
        }
    }
}
