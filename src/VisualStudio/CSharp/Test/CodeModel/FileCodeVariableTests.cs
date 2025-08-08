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
public sealed class FileCodeVariableTests : AbstractFileCodeElementTests
{
    public FileCodeVariableTests()
        : base("""
            using System;

            public class A
            {
                // This is a comment.
                public int intA;

               /// <summary>
               /// This is a summary.
               /// </summary>
                protected int intB;

                [Serializable]
                private int intC = 4;
                int intD;

                public static const int FORTYTWO = 42;
            }

            unsafe public struct DevDivBugs70194
            {
                fixed char buffer[100];
            }
            """)
    {
    }

    private CodeVariable GetCodeVariable(params object[] path)
    {
        return (CodeVariable)GetCodeElement(path);
    }

    [WpfFact]
    public void Access_Public()
    {
        var testObject = GetCodeVariable("A", "intA");

        Assert.Equal(vsCMAccess.vsCMAccessPublic, testObject.Access);
    }

    [WpfFact]
    public void Access_Protected()
    {
        var testObject = GetCodeVariable("A", "intB");

        Assert.Equal(vsCMAccess.vsCMAccessProtected, testObject.Access);
    }

    [WpfFact]
    public void Access_Private()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.Equal(vsCMAccess.vsCMAccessPrivate, testObject.Access);
    }

    [WpfFact]
    public void Attributes_Count()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.Equal(1, testObject.Attributes.Count);
    }

    [WpfFact]
    public void Children_Count()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.Equal(1, testObject.Children.Count);
    }

    [WpfFact]
    public void Comment()
    {
        var testObject = GetCodeVariable("A", "intA");

        Assert.Equal("This is a comment.\r\n", testObject.Comment);
    }

    [WpfFact]
    public void DocComment()
    {
        var testObject = GetCodeVariable("A", "intB");
        Assert.Equal("<doc>\r\n<summary>\r\nThis is a summary.\r\n</summary>\r\n</doc>", testObject.DocComment);
    }

    [WpfFact]
    public void InitExpressions_NoExpression()
    {
        var testObject = GetCodeVariable("A", "intB");

        Assert.Null(testObject.InitExpression);
    }

    [WpfFact]
    public void InitExpression()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.Equal("4", testObject.InitExpression);
    }

    [WpfFact]
    public void InitExpression_FixedBuffer()
    {
        var testObject = GetCodeVariable("DevDivBugs70194", "buffer");

        Assert.Null(testObject.InitExpression);
    }

    [WpfFact]
    public void IsConstant_True()
    {
        var testObject = GetCodeVariable("A", "FORTYTWO");

        Assert.True(testObject.IsConstant);
    }

    [WpfFact]
    public void IsConstant_False()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.False(testObject.IsConstant);
    }

    [WpfFact]
    public void IsShared_True()
    {
        var testObject = GetCodeVariable("A", "FORTYTWO");

        Assert.True(testObject.IsShared);
    }

    [WpfFact]
    public void IsShared_False()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.False(testObject.IsShared);
    }

    [WpfFact]
    public void Kind()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.Equal(vsCMElement.vsCMElementVariable, testObject.Kind);
    }

    [WpfFact]
    public void Parent()
    {
        var testObject = GetCodeVariable("A", "intC");

        var testObjectParent = testObject.Parent as CodeClass;

        Assert.Equal("A", testObjectParent.Name);
    }

    [WpfFact]
    public void Type()
    {
        var testObject = GetCodeVariable("A", "intC");

        Assert.Equal("System.Int32", testObject.Type.AsFullName);
    }

    [WpfFact]
    public void GetStartPoint_Attributes()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
    }

    [WpfFact]
    public void GetStartPoint_AttributesWithDelimiter()
    {
        var testObject = GetCodeVariable("A", "intC");

        var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

        Assert.Equal(13, startPoint.Line);
        Assert.Equal(5, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetStartPoint_Body()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBody));
    }

    [WpfFact]
    public void GetStartPoint_BodyWithDelimiter()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
    }

    [WpfFact]
    public void GetStartPoint_Header()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeader));
    }

    [WpfFact]
    public void GetStartPoint_HeaderWithAttributes()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
    }

    [WpfFact]
    public void GetStartPoint_Name()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
    }

    [WpfFact]
    public void GetStartPoint_Navigate()
    {
        var testObject = GetCodeVariable("A", "intC");

        var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

        Assert.Equal(14, startPoint.Line);
        Assert.Equal(17, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetStartPoint_Whole()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
    }

    [WpfFact]
    public void GetStartPoint_WholeWithAttributes()
    {
        var testObject = GetCodeVariable("A", "intC");

        var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

        Assert.Equal(13, startPoint.Line);
        Assert.Equal(5, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_Attributes()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
    }

    [WpfFact]
    public void GetEndPoint_AttributesWithDelimiter()
    {
        var testObject = GetCodeVariable("A", "intC");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

        Assert.Equal(13, endPoint.Line);
        Assert.Equal(19, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_Body()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBody));
    }

    [WpfFact]
    public void GetEndPoint_BodyWithDelimiter()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
    }

    [WpfFact]
    public void GetEndPoint_Header()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
    }

    [WpfFact]
    public void GetEndPoint_HeaderWithAttributes()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
    }

    [WpfFact]
    public void GetEndPoint_Name()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
    }

    [WpfFact]
    public void GetEndPoint_Navigate()
    {
        var testObject = GetCodeVariable("A", "intC");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

        Assert.Equal(14, endPoint.Line);
        Assert.Equal(21, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_Whole()
    {
        var testObject = GetCodeVariable("A", "intC");
        Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
    }

    [WpfFact]
    public void GetEndPoint_WholeWithAttributes()
    {
        var testObject = GetCodeVariable("A", "intC");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

        Assert.Equal(14, endPoint.Line);
        Assert.Equal(26, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void StartPoint()
    {
        var testObject = GetCodeVariable("A", "intC");

        var startPoint = testObject.StartPoint;

        Assert.Equal(13, startPoint.Line);
        Assert.Equal(5, startPoint.LineCharOffset);
    }

    [WpfFact]
    public void EndPoint()
    {
        var testObject = GetCodeVariable("A", "intC");

        var endPoint = testObject.EndPoint;

        Assert.Equal(14, endPoint.Line);
        Assert.Equal(26, endPoint.LineCharOffset);
    }
}
