using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class DeterministicTests : EmitMetadataTestBase
    {
        private Guid CompiledGuid(string source, string assemblyName)
        {
            var compilation = CreateCompilation(source, assemblyName: assemblyName, references: new[] { MscorlibRef });
            Guid result = default(Guid);
            base.CompileAndVerify(compilation, emitOptions: EmitOptions.CCI, validator: (a, eo) =>
                {
                    var module = a.Modules[0];
                    result = module.GetModuleVersionIdOrThrow();
                });
            return result;
        }

        [Fact]
        public void Simple()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
            var mvid1 = CompiledGuid(source, "X1");
            var mvid2 = CompiledGuid(source, "X1");
            var mvid3 = CompiledGuid(source, "X2");
            var mvid4 = CompiledGuid(source, "X2");
            Assert.Equal(mvid1, mvid2);
            Assert.Equal(mvid3, mvid4);
            Assert.NotEqual(mvid1, mvid3);
        }
    }
}
