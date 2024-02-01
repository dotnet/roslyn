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

        private CodeClass GetCodeClass(EditorTestWorkspace workspace, params object[] path)
        {
            return (CodeClass)GetCodeElement(workspace, path);
        }

        [WpfFact]
        public void IsAbstract()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var cc = GetCodeClass(workspace, "Goo");

            Assert.True(cc.IsAbstract);
        }

        [WpfFact]
        public void Bases()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var cc = GetCodeClass(workspace, "Goo");

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
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var cc = GetCodeClass(workspace, "Goo");

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
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var cc = GetCodeClass(workspace, "Goo");

            Assert.Equal(vsCMElement.vsCMElementClass, cc.Kind);
        }

        [WpfFact]
        public void GetStartPoint_Attributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_Body()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(10, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_BodyWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetStartPoint_Header()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartHeader);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_HeaderWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetStartPoint_Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetStartPoint_Navigate()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(14, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_Whole()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetStartPoint_WholeWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Attributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, endPoint.Line);
            Assert.Equal(15, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Body()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_BodyWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetEndPoint_Header()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        public void GetEndPoint_HeaderWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetEndPoint_Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetEndPoint_Navigate()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, endPoint.Line);
            Assert.Equal(17, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Whole()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetEndPoint_WholeWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void StartPoint()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var startPoint = testObject.StartPoint;

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void EndPoint()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var endPoint = testObject.EndPoint;

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void Accessor()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeClass(workspace, "Bar");

            var l = from p in testObject.Members.OfType<CodeProperty>() where vsCMAccess.vsCMAccessPublic == p.Access && p.Getter != null && !p.Getter.IsShared && vsCMAccess.vsCMAccessPublic == p.Getter.Access select p;
            var z = l.ToList<CodeProperty>();
            Assert.Equal(2, z.Count);
        }
    }
}
