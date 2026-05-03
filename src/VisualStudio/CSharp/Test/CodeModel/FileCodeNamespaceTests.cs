// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel;

[Trait(Traits.Feature, Traits.Features.CodeModel)]
public sealed class FileCodeNamespaceTests : AbstractFileCodeElementTests
{
    public FileCodeNamespaceTests()
        : base("""
            using System;

            namespace Goo
            {
                public class Alpha
                {
                }

                public class Beta
                {
                }

                namespace Bar
                {
                }
            }

            namespace A.B
            {
                public class Alpha
                {
                }

                public class Beta
                {
                }
            }
            """)
    {
    }

    private CodeNamespace GetCodeNamespace(params object[] path)
    {
        return (CodeNamespace)GetCodeElement(path);
    }

    [WpfFact]
    public void Children()
    {
        var testObject = GetCodeNamespace("Goo");

        Assert.Equal(3, testObject.Children.Count);
    }

    [WpfFact]
    public void Members()
    {
        var testObject = GetCodeNamespace("Goo");

        Assert.Equal(3, testObject.Members.Count);
    }

    [WpfFact]
    public void Parent()
    {
        var outer = GetCodeNamespace("Goo");
        var inner = outer.Members.Item("Bar") as CodeNamespace;

        Assert.Equal(outer.Name, ((CodeNamespace)inner.Parent).Name);
    }

    [WpfFact]
    public void Kind()
    {
        var testObject = GetCodeNamespace("Goo");

        Assert.Equal(vsCMElement.vsCMElementNamespace, testObject.Kind);
    }

    [WpfFact]
    public void Name()
    {
        var testObject = GetCodeNamespace(2);

        Assert.Equal("Goo", testObject.Name);
    }

    [WpfFact]
    public void Name_Dotted()
    {
        var testObject = GetCodeNamespace(3);

        Assert.Equal("A.B", testObject.Name);
    }

    [WpfFact]
    public void GetStartPoint_Attributes()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
    }

    [WpfFact]
    public void GetStartPoint_AttributesWithDelimiter()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
    }

    [WpfFact]
    public void GetStartPoint_Body()
    {
        var testObject = GetCodeNamespace("A.B");

        var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

        Assert.Equal(20, startPoint.Line);
        Assert.Equal(1, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetStartPoint_BodyWithDelimiter()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
    }

    [WpfFact]
    public void GetStartPoint_Header()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeader));
    }

    [WpfFact]
    public void GetStartPoint_HeaderWithAttributes()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
    }

    [WpfFact]
    public void GetStartPoint_Name()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
    }

    [WpfFact]
    public void GetStartPoint_Navigate()
    {
        var testObject = GetCodeNamespace("A.B");

        var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

        Assert.Equal(18, startPoint.Line);
        Assert.Equal(11, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetStartPoint_Whole()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
    }

    [WpfFact]
    public void GetStartPoint_WholeWithAttributes()
    {
        var testObject = GetCodeNamespace("A.B");
        var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

        Assert.Equal(18, startPoint.Line);
        Assert.Equal(1, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_Attributes()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
    }

    [WpfFact]
    public void GetEndPoint_AttributesWithDelimiter()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
    }

    [WpfFact]
    public void GetEndPoint_Body()
    {
        var testObject = GetCodeNamespace("A.B");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

        Assert.Equal(27, endPoint.Line);
        Assert.Equal(1, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_BodyWithDelimiter()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
    }

    [WpfFact]
    public void GetEndPoint_Header()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
    }

    [WpfFact]
    public void GetEndPoint_HeaderWithAttributes()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
    }

    [WpfFact]
    public void GetEndPoint_Name()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
    }

    [WpfFact]
    public void GetEndPoint_Navigate()
    {
        var testObject = GetCodeNamespace("A.B");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

        Assert.Equal(18, endPoint.Line);
        Assert.Equal(14, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_Whole()
    {
        var testObject = GetCodeNamespace("A.B");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
    }

    [WpfFact]
    public void GetEndPoint_WholeWithAttributes()
    {
        var testObject = GetCodeNamespace("A.B");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

        Assert.Equal(27, endPoint.Line);
        Assert.Equal(2, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void StartPoint()
    {
        var testObject = GetCodeNamespace("A.B");

        var startPoint = testObject.StartPoint;

        Assert.Equal(18, startPoint.Line);
        Assert.Equal(1, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void EndPoint()
    {
        var testObject = GetCodeNamespace("A.B");

        var endPoint = testObject.EndPoint;

        Assert.Equal(27, endPoint.Line);
        Assert.Equal(2, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void Language()
    {
        var testObject = GetCodeNamespace("A.B");

        Assert.Equal(CodeModelLanguageConstants.vsCMLanguageCSharp, testObject.Language);
    }
}
