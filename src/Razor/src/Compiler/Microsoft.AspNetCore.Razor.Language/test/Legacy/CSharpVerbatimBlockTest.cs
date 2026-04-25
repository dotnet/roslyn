// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpVerbatimBlockTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void VerbatimBlock()
    {
        ParseDocumentTest("@{ foo(); }");
    }

    [Fact]
    public void InnerImplicitExprWithOnlySingleAtOutputsZeroLengthCodeSpan()
    {
        ParseDocumentTest("@{@}");
    }

    [Fact]
    public void InnerImplicitExprDoesNotAcceptDotAfterAt()
    {
        ParseDocumentTest("@{@.}");
    }

    [Fact]
    public void InnerImplicitExprWithOnlySingleAtAcceptsSingleSpaceOrNewlineAtDesignTime()
    {
        ParseDocumentTest("""
            @{
                @
            }
            """, designTime: true);
    }

    [Fact]
    public void InnerImplicitExprDoesNotAcceptTrailingNewlineInRunTimeMode()
    {
        ParseDocumentTest("""
            @{@foo.
            }
            """);
    }

    [Fact]
    public void InnerImplicitExprAcceptsTrailingNewlineInDesignTimeMode()
    {
        ParseDocumentTest("""
            @{@foo.
            }
            """, designTime: true);
    }
}
