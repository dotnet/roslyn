// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.BracePairs;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BracePairs;

public sealed class CSharpBracePairsTests : AbstractBracePairsTests
{
    protected override EditorTestWorkspace CreateWorkspace(string input)
        => EditorTestWorkspace.CreateCSharp(input);

    [Fact]
    public Task Test1()
        => Test("""
            public class C
            {|a:{|}
                void M{|b:(|}int i{|b:)|}
                {|c:{|}
                {|c:}|}

                {|d:[|}Attr{|d:]|}
                void M2{|e:(|}List{|f:<|}int{|f:>|} i{|e:)|}
                {|g:{|}
                {|g:}|}
            {|a:}|}
            """);
}
