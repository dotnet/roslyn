// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Test.Utilities.Rename;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Rename
{
    public class CSharpRenameRewriterTests : RenameRewriterTests
    {
        [Fact]
        public async Task TestRenameMultipleSymbolsInSingleDocument()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""C#"" CommonReferences=""true"">
                           <Document FilePath=""test.cs"">
public class {|Rename1:Apple|}
{

    public {|classRef:Apple|}()
    {
        {|propertyRef1:Orange|} = 10;
        {|methodRef:Goo|}({|propertyRef2:Orange|});
    }

    public void {|Rename2:Goo|}(int x)
    {
    }

    public int {|Rename3:Orange|}
    {
        get;
        set;
    }
}
                           </Document>
                       </Project>
            </Workspace>
");

            var renameOption = new SymbolRenameOptions();
            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test.cs",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("Goo2", renameOption) },
                    { "Rename3", ("Orange2", renameOption) },
                }, cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "Rename1", "Apple2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "classRef", "Apple2", cancellationToken);

            await verifier.VerifyAsync(documentFilePath: "test.cs", "Rename2", "Goo2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "methodRef", "Goo2", cancellationToken);

            await verifier.VerifyAsync(documentFilePath: "test.cs", "Rename3", "Orange2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "propertyRef", "Orange2", cancellationToken);
        }

        [Fact]
        public async Task TestRenameInCommentsAndStrings()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""C#"" CommonReferences=""true"">
                           <Document FilePath=""test1.cs"">
<![CDATA[
/// <summary>
/// <see cref=""Apple""/> Apple is not Lemon. Lemon is not banana
/// </summary>
/// <Lemon>
/// </Lemon>
public class {|Rename1:Apple|}
{
    // banana is not Apple
    public void {|Rename2:Lemon|}(int {|Rename3:banana|})
    {
        string Apple = ""Apple, Lemon and banana are fruit"";
        string Lemon = $""Apple, Lemon and {banana} are fruit"";
    }
}]]>
                           </Document>
                    </Project>
                </Workspace>
");

            var renameOption = new SymbolRenameOptions() { RenameInComments = true, RenameInStrings = true };

            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test1.cs",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("Lemon2", renameOption) },
                    { "Rename3", ("banana2", renameOption) },
                }, cancellationToken);
            await verifier.VerifyDocumentAsync("test1.cs",
@"
/// <summary>
/// <see cref=""Apple2""/> Apple2 is not Lemon2. Lemon2 is not banana2
/// </summary>
/// <Lemon2>
/// </Lemon2>
public class Apple2
{
    // banana2 is not Apple2
    public void Lemon2(int banana2)
    {
        string Apple = ""Apple2, Lemon2 and banana2 are fruit"";
        string Lemon = $""Apple2, Lemon2 and {banana2} are fruit"";
    }
}", cancellationToken);
        }
    }
}
