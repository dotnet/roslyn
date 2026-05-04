// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessarySuppressions;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)]
[WorkItem("https://github.com/dotnet/roslyn/issues/44176")]
public sealed class RemoveUnnecessaryNullableWarningSuppressionsFixAllTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpRemoveUnnecessaryNullableWarningSuppressionsDiagnosticAnalyzer(),
            new CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider());

    [Fact]
    public Task TestFixAllWithoutConflict()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document Name="C1.cs">
            #nullable enable
            class Program1
            {
                static void Main()
                {
                    string s = Goo.GetString(){|FixAllInSolution:!|};
                    System.Console.WriteLine(s);
                }

                static void D()
                {
                    string y = ""!;
                    System.Console.WriteLine(y);
                }
            }
                    </Document>
                    <Document Name="D1.cs">
            #nullable enable
            class Goo
            {
                public static string GetString() => "";
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document Name="C1.cs">
            #nullable enable
            class Program1
            {
                static void Main()
                {
                    string s = Goo.GetString();
                    System.Console.WriteLine(s);
                }
            
                static void D()
                {
                    string y = "";
                    System.Console.WriteLine(y);
                }
            }
                    </Document>
                    <Document Name="D1.cs">
            #nullable enable
            class Goo
            {
                public static string GetString() => "";
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestFixAllWithConflict()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="C1.cs">
            #nullable enable
            class Program1
            {
                static void Main()
                {
                    // In this test, we will not remove this guy because in our linked file we will see Goo.GetString
                    // return a `string?` and will want to preserve that.  But we will remove the `!` in the method
                    // below.
                    string s = Goo.GetString(){|FixAllInSolution:!|};
                    System.Console.WriteLine(s);
                }

                static void D()
                {
                    string y = ""!;
                    System.Console.WriteLine(y);
                }
            }
                    </Document>
                    <Document Name="D1.cs">
            #nullable enable
            class Goo
            {
                public static string GetString() => "";
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document IsLinkFile="true" LinkAssemblyName="Assembly1" LinkFilePath="C1.cs"/>
            
                    <Document Name="D2.cs">
            #nullable enable
            class Goo
            {
                // In this project GetString does return a nullable string.
                public static string? GetString() => "";
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="C1.cs">
            #nullable enable
            class Program1
            {
                static void Main()
                {
                    // In this test, we will not remove this guy because in our linked file we will see Goo.GetString
                    // return a `string?` and will want to preserve that.  But we will remove the `!` in the method
                    // below.
                    string s = Goo.GetString()!;
                    System.Console.WriteLine(s);
                }
            
                static void D()
                {
                    string y = "";
                    System.Console.WriteLine(y);
                }
            }
                    </Document>
                    <Document Name="D1.cs">
            #nullable enable
            class Goo
            {
                public static string GetString() => "";
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document IsLinkFile="true" LinkAssemblyName="Assembly1" LinkFilePath="C1.cs"/>
            
                    <Document Name="D2.cs">
            #nullable enable
            class Goo
            {
                // In this project GetString does return a nullable string.
                public static string? GetString() => "";
            }
                    </Document>
                </Project>
            </Workspace>
            """);
}
