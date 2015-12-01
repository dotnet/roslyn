// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeVariableTests : AbstractFileCodeElementTests
    {
        public FileCodeVariableTests()
            : base(@"using System;

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
}")
        {
        }

        private async Task<CodeVariable> GetCodeVariableAsync(params object[] path)
        {
            return (CodeVariable)await GetCodeElementAsync(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Access_Public()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intA");

            Assert.Equal(vsCMAccess.vsCMAccessPublic, testObject.Access);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Access_Protected()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intB");

            Assert.Equal(vsCMAccess.vsCMAccessProtected, testObject.Access);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Access_Private()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.Equal(vsCMAccess.vsCMAccessPrivate, testObject.Access);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Attributes_Count()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.Equal(1, testObject.Attributes.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Children_Count()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.Equal(1, testObject.Children.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Comment()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intA");

            Assert.Equal("This is a comment.\r\n", testObject.Comment);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task DocComment()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intB");

            string expected = "<doc>\r\n<summary>\r\nThis is a summary.\r\n</summary>\r\n</doc>";

            Assert.Equal(expected, testObject.DocComment);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task InitExpressions_NoExpression()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intB");

            Assert.Equal(null, testObject.InitExpression);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task InitExpression()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.Equal("4", testObject.InitExpression);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task InitExpression_FixedBuffer()
        {
            CodeVariable testObject = await GetCodeVariableAsync("DevDivBugs70194", "buffer");

            Assert.Equal(null, testObject.InitExpression);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsConstant_True()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "FORTYTWO");

            Assert.True(testObject.IsConstant);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsConstant_False()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.False(testObject.IsConstant);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsShared_True()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "FORTYTWO");

            Assert.True(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsShared_False()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.False(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Kind()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.Equal(vsCMElement.vsCMElementVariable, testObject.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Parent()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            CodeClass testObjectParent = testObject.Parent as CodeClass;

            Assert.Equal("A", testObjectParent.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Type()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            Assert.Equal("System.Int32", testObject.Type.AsFullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Attributes()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_AttributesWithDelimiter()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(13, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Body()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBody));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_BodyWithDelimiter()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Header()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_HeaderWithAttributes()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Name()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Navigate()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(14, startPoint.Line);
            Assert.Equal(17, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Whole()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_WholeWithAttributes()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(13, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Attributes()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_AttributesWithDelimiter()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(13, endPoint.Line);
            Assert.Equal(19, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBody));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_BodyWithDelimiter()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Header()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_HeaderWithAttributes()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Name()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Navigate()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(14, endPoint.Line);
            Assert.Equal(21, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Whole()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_WholeWithAttributes()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(14, endPoint.Line);
            Assert.Equal(26, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task StartPoint()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint startPoint = testObject.StartPoint;

            Assert.Equal(13, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task EndPoint()
        {
            CodeVariable testObject = await GetCodeVariableAsync("A", "intC");

            TextPoint endPoint = testObject.EndPoint;

            Assert.Equal(14, endPoint.Line);
            Assert.Equal(26, endPoint.LineCharOffset);
        }
    }
}
