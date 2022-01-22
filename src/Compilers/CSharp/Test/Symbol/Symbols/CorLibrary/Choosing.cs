// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.CorLibrary
{
    public class Choosing : CSharpTestBase
    {
        [Fact]
        public void MultipleMscorlibReferencesInMetadata()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CorLibrary.GuidTest2.exe,
                TestMetadata.Net40.mscorlib
            });

            Assert.Same(assemblies[1], assemblies[0].Modules[0].CorLibrary());
        }

        [Fact, WorkItem(760148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760148")]
        public void Bug760148_1()
        {
            var corLib = CreateEmptyCompilation(@"
namespace System
{
    public class Object
    {
    }
}
", options: TestOptions.ReleaseDll);

            var obj = corLib.GetSpecialType(SpecialType.System_Object);

            Assert.False(obj.IsErrorType());
            Assert.Same(corLib.Assembly, obj.ContainingAssembly);

            var consumer = CreateEmptyCompilation(@"
public class Test
{
}
", new[] { new CSharpCompilationReference(corLib) }, options: TestOptions.ReleaseDll);

            Assert.Same(obj, consumer.GetSpecialType(SpecialType.System_Object));
        }

        [Fact, WorkItem(760148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760148")]
        public void Bug760148_2()
        {
            var corLib = CreateEmptyCompilation(@"
namespace System
{
    class Object
    {
    }
}
", options: TestOptions.ReleaseDll);

            var consumer = CreateEmptyCompilation(@"
public class Test
{
}
", new[] { new CSharpCompilationReference(corLib) }, options: TestOptions.ReleaseDll);

            Assert.True(consumer.GetSpecialType(SpecialType.System_Object).IsErrorType());
        }
    }
}
