// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeClassTests : AbstractFileCodeElementTests
    {
        public static async Task<FileCodeClassTests> CreateAsync()
        {
            var pair = await CreateWorkspaceAndFileCodeModelAsync(@"using System;

public abstract class Foo : IDisposable, ICloneable
{
}

[Serializable]
public class Bar
{
    int a;

    public int A
    {
        get
        {
            return a;
        }
    }
}");
            return new FileCodeClassTests(pair);
        }

        public FileCodeClassTests(Tuple<TestWorkspace, EnvDTE.FileCodeModel> pair)
            : base(pair)
        {
        }

        private CodeClass GetCodeClass(params object[] path)
        {
            return (CodeClass)GetCodeElement(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsAbstract()
        {
            CodeClass cc = GetCodeClass("Foo");

            Assert.True(cc.IsAbstract);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Bases()
        {
            CodeClass cc = GetCodeClass("Foo");

            var bases = cc.Bases;

            Assert.Equal(bases.Count, 1);
            Assert.Equal(bases.Cast<CodeElement>().Count(), 1);

            Assert.NotNull(bases.Parent);

            var parentClass = bases.Parent as CodeClass;
            Assert.NotNull(parentClass);
            Assert.Equal(parentClass.FullName, "Foo");

            Assert.True(bases.Item("object") is CodeClass);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void ImplementedInterfaces()
        {
            CodeClass cc = GetCodeClass("Foo");

            var interfaces = cc.ImplementedInterfaces;

            Assert.Equal(interfaces.Count, 2);
            Assert.Equal(interfaces.Cast<CodeElement>().Count(), 2);

            Assert.NotNull(interfaces.Parent);

            var parentClass = interfaces.Parent as CodeClass;
            Assert.NotNull(parentClass);
            Assert.Equal(parentClass.FullName, "Foo");

            Assert.True(interfaces.Item("System.IDisposable") is CodeInterface);
            Assert.True(interfaces.Item("ICloneable") is CodeInterface);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void KindTest()
        {
            CodeClass cc = GetCodeClass("Foo");

            Assert.Equal(vsCMElement.vsCMElementClass, cc.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Attributes()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Body()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(10, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_BodyWithDelimiter()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Header()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartHeader);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_HeaderWithAttributes()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Name()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Navigate()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(14, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Whole()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_WholeWithAttributes()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Attributes()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, endPoint.Line);
            Assert.Equal(15, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(19, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_BodyWithDelimiter()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Header()
        {
            CodeClass testObject = GetCodeClass("Bar");

            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_HeaderWithAttributes()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Name()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Navigate()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, endPoint.Line);
            Assert.Equal(17, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Whole()
        {
            CodeClass testObject = GetCodeClass("Bar");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_WholeWithAttributes()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(19, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void StartPoint()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint startPoint = testObject.StartPoint;

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void EndPoint()
        {
            CodeClass testObject = GetCodeClass("Bar");

            TextPoint endPoint = testObject.EndPoint;

            Assert.Equal(19, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }
    }
}
