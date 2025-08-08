// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class TupleConstructionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(TupleConstructionSignatureHelpProvider);

    [Fact]
    public Task InvocationAfterOpenParen()
        => TestAsync("""
            class C
            {
                (int, int) y = [|($$
            |]}
            """, [new("(int, int)", currentParameterIndex: 0, parameterDocumentation: "")], usePreviousCharAsTrigger: true);

    [Fact]
    public Task InvocationWithNullableReferenceTypes()
        => TestAsync("""
            class C
            {
                (string?, string) y = [|($$
            |]}
            """, [new("(string?, string)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/655607")]
    public Task TestMissingTupleElement()
        => TestAsync("""
            class C
            {
                void M()
                {
                    (a, ) = [|($$
            |]  }
            }
            """, [new("(object a, object)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task InvocationAfterOpenParen2()
        => TestAsync("""
            class C
            {
                (int, int) y = [|($$)|]
            }
            """, [new("(int, int)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task InvocationAfterComma1()
        => TestAsync("""
            class C
            {
                (int, int) y = [|(1,$$
            |]}
            """, [new("(int, int)", currentParameterIndex: 1, parameterDocumentation: "")], usePreviousCharAsTrigger: true);

    [Fact]
    public Task InvocationAfterComma2()
        => TestAsync("""
            class C
            {
                (int, int) y = [|(1,$$)|]
            }
            """, [new("(int, int)", currentParameterIndex: 1)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task ParameterIndexWithNameTyped()
        => TestAsync("""
            class C
            {
                (int a, int b) y = [|(b: $$
            |]}
            """, [
            // currentParameterIndex only considers the position in the argument list 
            // and not names, hence passing 0 even though the controller will highlight
            // "int b" in the actual display
            new("(int a, int b)", currentParameterIndex: 0)]);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14277")]
    public Task NestedTuple()
        => TestAsync("""
            class C
            {
                (int a, (int b, int c)) y = [|(1, ($$
            |]}
            """, [new("(int b, int c)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task NestedTupleWhenNotInferred()
        => TestAsync("""
            class C
            {
                (int, object) y = [|(1, ($$
            |]}
            """, [new("(int, object)", currentParameterIndex: 1)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task NestedTupleWhenNotInferred2()
        => TestAsync("""
            class C
            {
                (int, object) y = [|(1, (2,$$
            |]}
            """, [new("(int, object)", currentParameterIndex: 1)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task NestedTupleWhenNotInferred3()
        => TestAsync("""
            class C
            {
                (int, object) y = [|(1, ($$
            |]}
            """, [new("(int, object)", currentParameterIndex: 1)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task NestedTupleWhenNotInferred4()
        => TestAsync("""
            class C
            {
                (object, object) y = [|(($$
            |]}
            """, [new("(object, object)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact]
    public Task MultipleOverloads()
        => TestAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    Do1([|($$)|])
                }

                static void Do1((int, int) i) { }
                static void Do1((string, string) s) { }
            }
            """, [
            new("(int, int)", currentParameterIndex: 0),
            new("(string, string)", currentParameterIndex: 0)], usePreviousCharAsTrigger: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14793")]
    public Task DoNotCrashInLinkedFile()
        => VerifyItemWithReferenceWorkerAsync(
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                void bar()
                {
                }
            #endif
                void goo()
                {
                    (int, string) x = ($$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [new($"(int, string)", currentParameterIndex: 0)], hideAdvancedMembers: false);
}
