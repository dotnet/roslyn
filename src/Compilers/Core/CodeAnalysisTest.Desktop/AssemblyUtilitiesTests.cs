using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyUtilitiesTests : TestBase
    {
        [Fact]
        public void ReadMVid()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var assembly = Assembly.Load(File.ReadAllBytes(alphaDll.Path));

            var result = AssemblyUtilities.ReadMvid(alphaDll.Path);

            Assert.Equal(expected: assembly.ManifestModule.ModuleVersionId, actual: result);
        }
    }
}
