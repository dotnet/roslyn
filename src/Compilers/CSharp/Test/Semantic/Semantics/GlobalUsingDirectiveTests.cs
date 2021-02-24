// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class GlobalUsingDirectiveTests : CompilingTestBase
    {
        [Fact]
        public void MixingUsings_01()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

global using ns1;
using ns2;
global using ns3;
using ns4;

namespace ns1 {}
namespace ns2 {}
namespace ns3 {}
namespace ns4 {}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (6,1): error CS9002: A global using directive must precede all non-global using directives.
                // global using ns3;
                Diagnostic(ErrorCode.ERR_GlobalUsingOutOfOrder, "global").WithLocation(6, 1)
                );
        }

        [Fact]
        public void MixingUsings_02()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

global using ns1;
global using ns3;

namespace ns1 {}
namespace ns3 {}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void MixingUsings_03()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

global using ns1;
global using ns3;

using ns4;
using ns2;

namespace ns1 {}
namespace ns2 {}
namespace ns3 {}
namespace ns4 {}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void InNamespace_01()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

namespace ns
{
    global using ns1;
    using ns2;
    global using ns3;
    using ns4;

    namespace ns1 {}
    namespace ns2 {}
    namespace ns3 {}
    namespace ns4 {}
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (6,5): error CS9001: A global using directive cannot be used in a namespace declaration.
                //     global using ns1;
                Diagnostic(ErrorCode.ERR_GlobalUsingInNamespace, "global").WithLocation(6, 5)
                );
        }

        [Fact]
        public void InNamespace_02()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

namespace ns.ns.ns
{
    global using ns1;
    using ns2;
    global using ns3;
    using ns4;

    namespace ns1 {}
    namespace ns2 {}
    namespace ns3 {}
    namespace ns4 {}
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (6,5): error CS9001: A global using directive cannot be used in a namespace declaration.
                //     global using ns1;
                Diagnostic(ErrorCode.ERR_GlobalUsingInNamespace, "global").WithLocation(6, 5)
                );
        }
    }
}

