// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UpgradeProject;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UpgradeProject
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
    public sealed class UpgradeProjectAsyncMainTests : AbstractUpgradeProjectTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAsyncMainDiagnosticAnalyzer(), new CSharpUpgradeProjectCodeFixProvider());

        private async Task TestUpgradedTo7_1Async(
            string initialMarkup,
            OutputKind outputKind = OutputKind.ConsoleApplication,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            await TestLanguageVersionUpgradedAsync(
                initialMarkup,
                LanguageVersion.CSharp7_1,
                new CSharpParseOptions(LanguageVersion.CSharp7, kind: sourceCodeKind),
                new CSharpCompilationOptions(outputKind));
        }

        private async Task TestUpgradeMissingAsync(
            string initialMarkup,
            OutputKind outputKind = OutputKind.ConsoleApplication,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            LanguageVersion languageVersion = LanguageVersion.CSharp7)
        {
            await TestMissingAsync(
                initialMarkup,
                new TestParameters(
                    new CSharpParseOptions(languageVersion, kind: sourceCodeKind),
                    new CSharpCompilationOptions(outputKind)));
        }

        [Fact]
        public async Task TestOnTaskMain()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestOnTaskMainArgs()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main(string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainIntArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main(int[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainString()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main(string s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainRefArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main(ref string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainInArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main(in string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainOutArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main(out string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainArgsArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main(string[] s, string[] s2) => null;
}");
        }

        [Fact]
        public async Task TestOnTaskOfIntMain()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main() => null;
}");
        }

        [Fact]
        public async Task TestOnTaskOfIntMainArgs()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main(string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfIntMainIntArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main(int[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfIntMainString()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main(string s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfIntMainRefArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main(ref string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfIntMainInArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main(in string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfIntMainOutArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main(out string[] s) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfIntMainArgsArgs()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<int>|] Main(string[] s, string[] s2) => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfIntArrayMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<int[]>|] Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskOfStringMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task<string>|] Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnRefTaskMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static ref [|System.Threading.Tasks.Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnRefReadonlyTaskMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static ref readonly [|System.Threading.Tasks.Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnUnresolvedTaskMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestOnAsyncTaskMain()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static async [|System.Threading.Tasks.Task|] Main() { }
}");
        }

        [Fact]
        public async Task TestNotOnVoidMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|void|] Main() { }
}");
        }

        [Fact]
        public async Task TestNotOnAsyncVoidMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static async [|void|] Main() { }
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainLocalFunction()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static void M()
    {
        [|System.Threading.Tasks.Task|] Main() => null;
    }
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainProperty()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainPropertyGetter()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main
    {
        get => null;
    }
}");
        }

        [Fact]
        public async Task TestNotOnTaskWrongName1()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskWrongName2()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main2() => null;
}");
        }

        [Fact]
        public async Task TestNotOnGenericTaskMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main<T>() => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainInGenericClass()
        {
            await TestUpgradeMissingAsync(
@"class C<T>
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainInClassInGenericClass()
        {
            await TestUpgradeMissingAsync(
@"class B<T>
{
    class C
    {
        static [|System.Threading.Tasks.Task|] Main() => null;
    }
}");
        }

        [Fact]
        public async Task TestOnExternTaskMain()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static extern [|System.Threading.Tasks.Task|] Main();
}");
        }

        [Fact]
        public async Task TestNotOnNonStaticTaskMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    [|System.Threading.Tasks.Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnNonStaticVirtualTaskMain()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    virtual [|System.Threading.Tasks.Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestOnTaskMainInStruct()
        {
            await TestUpgradedTo7_1Async(
@"struct S
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainInScript()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task TestNotOnTaskMainAsGlobalMemberInScript()
        {
            await TestUpgradeMissingAsync(
@"
static [|System.Threading.Tasks.Task|] Main() => null;
", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task TestOnTaskMainWithOutputKind_WindowsApplication()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", outputKind: OutputKind.WindowsApplication);
        }

        [Fact]
        public async Task TestOnTaskMainWithOutputKind_WindowsRuntimeApplication()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", outputKind: OutputKind.WindowsRuntimeApplication);
        }

        [Fact]
        public async Task TestNotOnTaskMainWithOutputKind_DynamicallyLinkedLibrary()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", outputKind: OutputKind.DynamicallyLinkedLibrary);
        }

        [Fact]
        public async Task TestNotOnTaskMainWithOutputKind_NetModule()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", outputKind: OutputKind.NetModule);
        }

        [Fact]
        public async Task TestNotOnTaskMainWithOutputKind_WindowsRuntimeMetadata()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", outputKind: OutputKind.WindowsRuntimeMetadata);
        }

        [Fact]
        public async Task TestNotOnTaskMainWhenAnotherEntryPointExists1()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}
class D
{
    static void Main(string[] args) { }
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainWhenAnotherEntryPointExists2()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}
class D
{
    static int Main() => 0;
}");
        }

        [Fact]
        public async Task TestOnTaskMainWhenAnotherNonEntryPointExists()
        {
            await TestUpgradedTo7_1Async(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}
class D
{
    static System.Threading.Tasks.Task Main() => null;
}");
        }

        [Fact]
        public async Task TestNotOnTaskMainWithLanguageVersion_7_1()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", languageVersion: LanguageVersion.CSharp7_1);
        }

        [Fact]
        public async Task TestNotOnTaskMainWithLanguageVersion_Latest()
        {
            await TestUpgradeMissingAsync(
@"class C
{
    static [|System.Threading.Tasks.Task|] Main() => null;
}", languageVersion: LanguageVersion.Latest);
        }
    }
}
