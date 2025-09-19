// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression;

public abstract partial class CSharpSuppressionTests : AbstractSuppressionDiagnosticTest_NoEditor
{
    #region "Fix all occurrences tests"

    #region "Pragma disable tests"

    public abstract partial class CSharpPragmaWarningDisableSuppressionTests : CSharpSuppressionTests
    {
        [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
        public sealed partial class UserInfoDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
        {
            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public Task TestFixAllInDocument()
                => TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInDocument:class Class1|}
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                    class Class3 { }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class1
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class2
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                    }
                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class3 { }
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """);

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public Task TestFixAllInProject()
                => TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInProject:class Class1|}
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class1
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class2
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                    }
                            </Document>
                            <Document>
                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class3
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """);

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public Task TestFixAllInSolution()
                => TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInSolution:class Class1|}
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class1
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class2
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                    }
                            </Document>
                            <Document>
                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class3
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class1
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    class Class2
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """);

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public Task TestFixAllInContainingMember()
                => TestMissingInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInContainingMember:class Class1|}
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                    class Class3 { }
                            </Document>
                        </Project>
                    </Workspace>
                    """);

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public Task TestFixAllInContainingType()
                => TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInContainingType:partial class Class1|}
                    {
                        int Method1()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                        int Method2()
                        {
                            int x = 0;
                        }
                    }
                            </Document>
                            <Document>
                    partial class Class1
                    {
                        int Method3()
                        {
                            int x = 0;
                        }
                    }

                    class Class4
                    {
                        int Method4()
                        {
                            int x = 0;
                        }
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    partial class Class1
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                        int Method1()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                        int Method2()
                        {
                            int x = 0;
                        }
                    }
                            </Document>
                            <Document>
                    #pragma warning disable InfoDiagnostic // InfoDiagnostic Title
                    partial class Class1
                    #pragma warning restore InfoDiagnostic // InfoDiagnostic Title
                    {
                        int Method3()
                        {
                            int x = 0;
                        }
                    }

                    class Class4
                    {
                        int Method4()
                        {
                            int x = 0;
                        }
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """);
        }
    }

    #endregion

    #region "SuppressMessageAttribute tests"

    public abstract partial class CSharpGlobalSuppressMessageSuppressionTests : CSharpSuppressionTests
    {
        public sealed partial class UserInfoDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
        {
            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInDocument()
            {
                var addedGlobalSuppressions =
                    $"""
                    // This file is used by Code Analysis to maintain SuppressMessage
                    // attributes that are applied to this project.
                    // Project-level suppressions either have no target or are given
                    // a specific target and scoped to a namespace, type, member, etc.

                    using System.Diagnostics.CodeAnalysis;

                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "member", Target = "~M:Class1.Method~System.Int32")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class1")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class2")]

                    """
.Replace("<", "&lt;").Replace(">", "&gt;");

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                            <Document FilePath="GlobalSuppressions.cs">
                    """ + addedGlobalSuppressions +
    """
    </Document>
        </Project>
        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
            <Document>
    class Class1
    {
        int Method()
        {
            int x = 0;
        }
    }

    class Class2
    {
    }
            </Document>
        </Project>
    </Workspace>
    """;

                await TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInDocument:class Class1|}
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, expected, index: 1);
            }

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInProject()
            {
                var addedGlobalSuppressions =
                    $"""
                    // This file is used by Code Analysis to maintain SuppressMessage
                    // attributes that are applied to this project.
                    // Project-level suppressions either have no target or are given
                    // a specific target and scoped to a namespace, type, member, etc.

                    using System.Diagnostics.CodeAnalysis;

                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "member", Target = "~M:Class1.Method~System.Int32")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class1")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class2")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class3")]

                    """
.Replace("<", "&lt;").Replace(">", "&gt;");

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                            <Document FilePath="GlobalSuppressions.cs">
                    """ + addedGlobalSuppressions +
    """
    </Document>
        </Project>
        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
            <Document>
    class Class1
    {
        int Method()
        {
            int x = 0;
        }
    }

    class Class2
    {
    }
            </Document>
        </Project>
    </Workspace>
    """;

                await TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInProject:class Class1|}
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, expected, index: 1);
            }

            [Fact(Skip = "TODO: File a GitHubIssue for test framework unable to handle multiple projects in solution with same file name.")]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInSolution()
            {
                var addedGlobalSuppressionsProject1 =
                    $"""
                    // This file is used by Code Analysis to maintain SuppressMessage
                    // attributes that are applied to this project.
                    // Project-level suppressions either have no target or are given
                    // a specific target and scoped to a namespace, type, member, etc.

                    using System.Diagnostics.CodeAnalysis;

                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "member", Target = "~M:Class1.Method~System.Int32")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class1")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class2")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class3")]
                    """.Replace("<", "&lt;").Replace(">", "&gt;");

                var addedGlobalSuppressionsProject2 =
                    $"""
                    // This file is used by Code Analysis to maintain SuppressMessage
                    // attributes that are applied to this project.
                    // Project-level suppressions either have no target or are given
                    // a specific target and scoped to a namespace, type, member, etc.

                    using System.Diagnostics.CodeAnalysis;

                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "member", Target = "~M:Class1.Method~System.Int32")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class1")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class2")]
                    """.Replace("<", "&lt;").Replace(">", "&gt;");

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                            <Document FilePath="GlobalSuppressions.cs">
                    """ + addedGlobalSuppressionsProject1 +
    """
    </Document>
        </Project>
        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
            <Document>
    class Class1
    {
        int Method()
        {
            int x = 0;
        }
    }

    class Class2
    {
    }
            </Document>
            <Document FilePath="GlobalSuppressions.cs">
    """ + addedGlobalSuppressionsProject2 +
    """
    </Document>
        </Project>
    </Workspace>
    """;

                await TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInSolution:class Class1|}
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                            <Document>
                    class Class1
                    {
                        int Method()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, expected);
            }

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public Task TestFixAllInContainingMember()
                => TestMissingInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInContainingMember:class Class1|}
                    {
                        int Method1()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """);

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInContainingType()
            {
                var addedGlobalSuppressions =
                    $"""
                    // This file is used by Code Analysis to maintain SuppressMessage
                    // attributes that are applied to this project.
                    // Project-level suppressions either have no target or are given
                    // a specific target and scoped to a namespace, type, member, etc.

                    using System.Diagnostics.CodeAnalysis;

                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "member", Target = "~M:Class1.Method1~System.Int32")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "member", Target = "~M:Class1.Method2~System.Int32")]
                    [assembly: SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification = "{FeaturesResources.Pending}", Scope = "type", Target = "~T:Class1")]

                    """
.Replace("<", "&lt;").Replace(">", "&gt;");

                var expected = """
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    partial class Class1
                    {
                        int Method1()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    partial class Class1
                    {
                        int Method2()
                        {
                            int x = 0;
                        }
                    }

                    class Class3
                    {
                    }
                            </Document>
                            <Document FilePath="GlobalSuppressions.cs">
                    """ + addedGlobalSuppressions +
    """
    </Document>
        </Project>
    </Workspace>
    """;

                await TestInRegularAndScriptAsync("""
                    <Workspace>
                        <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                            <Document>
                    using System;

                    {|FixAllInContainingType:partial class Class1|}
                    {
                        int Method1()
                        {
                            int x = 0;
                        }
                    }

                    class Class2
                    {
                    }
                            </Document>
                            <Document>
                    partial class Class1
                    {
                        int Method2()
                        {
                            int x = 0;
                        }
                    }

                    class Class3
                    {
                    }
                            </Document>
                        </Project>
                    </Workspace>
                    """, expected, index: 1);
            }
        }
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
    public sealed partial class CSharpDiagnosticWithoutLocationSuppressionTests : CSharpSuppressionTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var addedGlobalSuppressions =
                $"""
                // This file is used by Code Analysis to maintain SuppressMessage
                // attributes that are applied to this project.
                // Project-level suppressions either have no target or are given
                // a specific target and scoped to a namespace, type, member, etc.

                using System.Diagnostics.CodeAnalysis;

                [assembly: SuppressMessage("NoLocationDiagnostic", "NoLocationDiagnostic:NoLocationDiagnostic", Justification = "{FeaturesResources.Pending}")]

                """
.Replace("<", "&lt;").Replace(">", "&gt;");

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
                using System;

                class Class1
                {
                    int Method()
                    {
                        int x = 0;
                    }
                }

                class Class2
                {
                }
                        </Document>
                        <Document>
                class Class3
                {
                }
                        </Document>
                        <Document FilePath="GlobalSuppressions.cs">
                """ + addedGlobalSuppressions +
"""
</Document>
    </Project>
    <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
    </Project>
</Workspace>
""";

            await TestInRegularAndScriptAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>{|FixAllInProject:|}
                using System;

                class Class1
                {
                    int Method()
                    {
                        int x = 0;
                    }
                }

                class Class2
                {
                }
                        </Document>
                        <Document>
                class Class3
                {
                }
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                        <Document>
                class Class1
                {
                    int Method()
                    {
                        int x = 0;
                    }
                }

                class Class2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """, expected);
        }
    }

    #endregion

    #endregion
}
