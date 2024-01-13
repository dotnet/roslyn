// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    [Trait(Traits.Feature, Traits.Features.CodeModel)]
    public class FileCodeClassTests : AbstractFileCodeElementTests
    {
        public FileCodeClassTests()
            : base(@"using System;

public abstract class Goo : IDisposable, ICloneable
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

    public string WindowsUserID => ""Domain""; 
}")
        {
        }

        private CodeClass GetCodeClass(params object[] path)
        {
            return (CodeClass)GetCodeElement(path);
        }

        [WpfFact]
        public void IsAbstract()
        {
            var cc = GetCodeClass("Goo");

            Assert.True(cc.IsAbstract);
        }

        [WpfFact]
        public void Bases()
        {
            var cc = GetCodeClass("Goo");

            var bases = cc.Bases;

            Assert.Equal(1, bases.Count);
            Assert.Equal(1, bases.Cast<CodeElement>().Count());

            Assert.NotNull(bases.Parent);

            var parentClass = bases.Parent as CodeClass;
            Assert.NotNull(parentClass);
            Assert.Equal("Goo", parentClass.FullName);

            Assert.True(bases.Item("object") is CodeClass);
        }

        [WpfFact]
        public void ImplementedInterfaces()
        {
            var cc = GetCodeClass("Goo");

            var interfaces = cc.ImplementedInterfaces;

            Assert.Equal(2, interfaces.Count);
            Assert.Equal(2, interfaces.Cast<CodeElement>().Count());

            Assert.NotNull(interfaces.Parent);

            var parentClass = interfaces.Parent as CodeClass;
            Assert.NotNull(parentClass);
            Assert.Equal("Goo", parentClass.FullName);

            Assert.True(interfaces.Item("System.IDisposable") is CodeInterface);
            Assert.True(interfaces.Item("ICloneable") is CodeInterface);
        }

        [WpfFact]
        public void KindTest()
        {
            var cc = GetCodeClass("Goo");

            Assert.Equal(vsCMElement.vsCMElementClass, cc.Kind);
        }

        [WpfFact]
        public void GetStartPoint_Attributes()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            var testObject = GetCodeClass("Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_Body()
        {
            var testObject = GetCodeClass("Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(10, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_BodyWithDelimiter()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetStartPoint_Header()
        {
            var testObject = GetCodeClass("Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartHeader);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_HeaderWithAttributes()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetStartPoint_Name()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetStartPoint_Navigate()
        {
            var testObject = GetCodeClass("Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(14, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_Whole()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetStartPoint_WholeWithAttributes()
        {
            var testObject = GetCodeClass("Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Attributes()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            var testObject = GetCodeClass("Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, endPoint.Line);
            Assert.Equal(15, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Body()
        {
            var testObject = GetCodeClass("Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_BodyWithDelimiter()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetEndPoint_Header()
        {
            var testObject = GetCodeClass("Bar");

            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        public void GetEndPoint_HeaderWithAttributes()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetEndPoint_Name()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetEndPoint_Navigate()
        {
            var testObject = GetCodeClass("Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, endPoint.Line);
            Assert.Equal(17, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Whole()
        {
            var testObject = GetCodeClass("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetEndPoint_WholeWithAttributes()
        {
            var testObject = GetCodeClass("Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void StartPoint()
        {
            var testObject = GetCodeClass("Bar");

            var startPoint = testObject.StartPoint;

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void EndPoint()
        {
            var testObject = GetCodeClass("Bar");

            var endPoint = testObject.EndPoint;

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void Accessor()
        {
            var testObject = GetCodeClass("Bar");

            var l = from p in testObject.Members.OfType<CodeProperty>() where vsCMAccess.vsCMAccessPublic == p.Access && p.Getter != null && !p.Getter.IsShared && vsCMAccess.vsCMAccessPublic == p.Getter.Access select p;
            var z = l.ToList<CodeProperty>();
            Assert.Equal(2, z.Count);
        }
    }
}
