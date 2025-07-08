// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryImports;

public sealed class RemoveUnnecessaryImportsTests_FixAllTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public RemoveUnnecessaryImportsTests_FixAllTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryImportsCodeFixProvider());

    #region "Fix all occurrences tests"

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInDocument()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            {|FixAllInDocument:using System;
            using System.Collections.Generic;|}

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInProject()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            {|FixAllInProject:using System;
            using System.Collections.Generic;|}

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInProjectSkipsGeneratedCode()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            {|FixAllInProject:using System;
            using System.Collections.Generic;|}

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document FilePath="Document.g.cs">
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document FilePath="Document.g.cs">
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInSolution()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            {|FixAllInSolution:using System;
            using System.Collections.Generic;|}

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingMember_NotApplicable()
        => TestMissingInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            {|FixAllInContainingMember:using System;
            using System.Collections.Generic;|}

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingType_NotApplicable()
        => TestMissingInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            {|FixAllInContainingType:using System;
            using System.Collections.Generic;|}

            class Program
            {
                public Int32 x;
            }
                    </Document>
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program2
            {
                public Int32 x;
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            using System.Collections.Generic;

            class Program3
            {
                public Int32 x;
            }
                    </Document>
                </Project>
            </Workspace>
            """);
    #endregion
}
