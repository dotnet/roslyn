using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IgnoreAccessCheckToTests : CSharpTestBase
    {
        private const string IACTDeclaration = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
        }
    }
}";

        [Fact]
        public void IACTSuccessThroughIAssembly()
        {
            string s = @"internal class C {}";

            var other = CreateStandardCompilation(s,
                assemblyName: "Paul",
                options: TestOptions.ReleaseDll);

            other.VerifyDiagnostics();

            var requestor = CreateStandardCompilation(
    @"
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(""Paul"")]

public class A
{
    public void M() => new C();
}" + IACTDeclaration,
                new MetadataReference[] { new CSharpCompilationReference(other) },
                options: TestOptions.ReleaseDll,
                assemblyName: "John");

            Assert.True(((IAssemblySymbol)other.Assembly).GivesAccessTo(requestor.Assembly));
            Assert.Empty(requestor.GetDiagnostics());
        }
    }
}
