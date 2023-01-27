﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities.BracePairs;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BracePairs
{
    public sealed class CSharpBracePairsTests : AbstractBracePairsTests
    {
        protected override TestWorkspace CreateWorkspace(string input)
            => TestWorkspace.CreateCSharp(input);

        [Fact]
        public async Task Test1()
        {
            await Test("""
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
    }
}
