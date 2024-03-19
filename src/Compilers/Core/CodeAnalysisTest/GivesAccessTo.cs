// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class GivesAccessTo
    {
        [Fact, WorkItem(26459, "https://github.com/dotnet/roslyn/issues/26459")]
        public void TestGivesAccessTo_CrossLanguageAndCompilation()
        {
            var csharpTree = CSharpTestSource.Parse(@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""VB"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS2"")]
internal class CS
{
}
");
            var csharpTree2 = CSharpTestSource.Parse(@"
internal class CS2
{
}
");
            var vbTree = BasicTestSource.Parse(@"
<assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS"")>
<assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""VB2"")>
Friend Class VB
End Class
");
            var vbTree2 = BasicTestSource.Parse(@"
Friend Class VB2
End Class
");
            var csc = (Compilation)CSharpCompilation.Create("CS", new[] { csharpTree }, new MetadataReference[] { TestBase.MscorlibRef });
            var CS = csc.GlobalNamespace.GetMembers("CS").First() as INamedTypeSymbol;

            var csc2 = (Compilation)CSharpCompilation.Create("CS2", new[] { csharpTree2 }, new MetadataReference[] { TestBase.MscorlibRef });
            var CS2 = csc2.GlobalNamespace.GetMembers("CS2").First() as INamedTypeSymbol;

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
