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
#if DEBUG
            int numberFluentCalls = 290;
#else
            int numberFluentCalls = 590;
#endif
            string MakeCode()
            {
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
                return builder.ToString();
            }
            var source = MakeCode();

            var thread = new System.Threading.Thread(() =>
            {
                var compilation = CreateCompilationWithMscorlib(source);
                compilation.GetDiagnostics();
                compilation.EmitToArray();
            }, 0);
            thread.Start();
            thread.Join();
        }
    }
}
