using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class EndToEndTests: EmitMetadataTestBase
    {
        // This test is a canary attempting to make sure that we don't regress the # of fluent calls that 
        // the compiler can handle.
        [WorkItem(16669, "https://github.com/dotnet/roslyn/issues/16669")]
        [Fact]
        public void OverflowOnFluentCall()
        {

            int numberFluentCalls = 0;

#if DEBUG
            bool isDebug = true;
#else
            bool isDebug = false;
#endif

            switch (IntPtr.Size * 8) {
                case 32 when isDebug:
                    numberFluentCalls = 510;
                    break;
                case 32 when !isDebug:
                    numberFluentCalls = 1600;
                    break;
                case 64 when isDebug:
                    numberFluentCalls = 225;
                    break;
                case 64 when !isDebug:
                    numberFluentCalls = 710;
                    break;
                default:
                    throw new Exception($"unexpected pointer size {IntPtr.Size}");
            }

            
            var builder = new StringBuilder();
            builder.AppendLine(
    @"class C {
    C M(string x) { return this; }
    void M2() {
        new C()
");
            for (int i = 0; i < numberFluentCalls; i++)
            {
                builder.AppendLine(@"            .M(""test"")");
            }
            builder.AppendLine(
               @"            .M(""test"");
    }
}");

            var source = builder.ToString();

            var thread = new System.Threading.Thread(() =>
            {
                var options = new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false);
                var compilation = CreateStandardCompilation(source, options: options);
                compilation.VerifyDiagnostics();
                compilation.EmitToArray();
            }, 0);
            thread.Start();
            thread.Join();
        }
    }
}
