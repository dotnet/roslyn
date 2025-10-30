// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNamespace;

public sealed class ConvertNamespaceRefactoringFixAllTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ConvertNamespaceCodeRefactoringProvider();

    private OptionsCollection PreferBlockScopedNamespace
        => this.Option(CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Warning);

    private OptionsCollection PreferFileScopedNamespace
        => this.Option(CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Warning);

    [Fact]
    public Task TestConvertToFileScope_FixAllInProject()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace {|FixAllInProject:|}N1
            {
            }
                    </Document>
                    <Document>
            namespace N2
            {
                class C { }
            }
                    </Document>
                    <Document>
            namespace N3.N4
            {
                class C2 { }
            }
                    </Document>
                    <Document>
            namespace N5;
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1;
                    </Document>
                    <Document>
            namespace N2;

            class C { }
                </Document>
                    <Document>
            namespace N3.N4;

            class C2 { }
                </Document>
                    <Document>
            namespace N5;
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: PreferBlockScopedNamespace));

    [Fact]
    public Task TestConvertToFileScope_FixAllInSolution()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace {|FixAllInSolution:|}N1
            {
            }
                    </Document>
                    <Document>
            namespace N2
            {
                class C { }
            }
                    </Document>
                    <Document>
            namespace N3.N4
            {
                class C2 { }
            }
                    </Document>
                    <Document>
            namespace N5;
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1;
                    </Document>
                    <Document>
            namespace N2;

            class C { }
                </Document>
                    <Document>
            namespace N3.N4;

            class C2 { }
                </Document>
                    <Document>
            namespace N5;
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6;
                    </Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: PreferBlockScopedNamespace));

    [Theory]
    [InlineData("FixAllInDocument")]
    [InlineData("FixAllInContainingType")]
    [InlineData("FixAllInContainingMember")]
    public Task TestConvertToFileScope_UnsupportedFixAllScopes(string fixAllScope)
        => TestMissingInRegularAndScriptAsync($$"""
            namespace {|{{fixAllScope}}:|}N1
            {
            }
            """, new TestParameters(options: PreferBlockScopedNamespace));

    [Fact]
    public Task TestConvertToBlockScope_FixAllInProject()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace {|FixAllInProject:|}N1;
                    </Document>
                    <Document>
            namespace N2;

            class C { }
                    </Document>
                    <Document>
            namespace N3.N4;

            class C2 { }
                    </Document>
                    <Document>
            namespace N5
            {
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6;
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1
            {
            }        </Document>
                    <Document>
            namespace N2
            {
                class C { }

            }        </Document>
                    <Document>
            namespace N3.N4
            {
                class C2 { }

            }        </Document>
                    <Document>
            namespace N5
            {
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6;
                    </Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: PreferFileScopedNamespace));

    [Fact]
    public Task TestConvertToBlockScope_FixAllInSolution()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace {|FixAllInSolution:|}N1;
                    </Document>
                    <Document>
            namespace N2;

            class C { }
                    </Document>
                    <Document>
            namespace N3.N4;

            class C2 { }
                    </Document>
                    <Document>
            namespace N5
            {
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6;
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1
            {
            }        </Document>
                    <Document>
            namespace N2
            {
                class C { }

            }        </Document>
                    <Document>
            namespace N3.N4
            {
                class C2 { }

            }        </Document>
                    <Document>
            namespace N5
            {
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace N6
            {
            }        </Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: PreferFileScopedNamespace));

    [Theory]
    [InlineData("FixAllInDocument")]
    [InlineData("FixAllInContainingType")]
    [InlineData("FixAllInContainingMember")]
    public Task TestConvertToBlockScope_UnsupportedFixAllScopes(string fixAllScope)
        => TestMissingInRegularAndScriptAsync($$"""
            namespace {|{{fixAllScope}}:|}N1;
            """, new TestParameters(options: PreferFileScopedNamespace));
}
