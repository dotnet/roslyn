// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeFunctionTests : AbstractFileCodeElementTests
    {
        public FileCodeFunctionTests()
            : base(@"using System;

public class A
{
    public A()
    {
    }

    ~A()
    {
    }

    public static A operator +(A a1, A a2)
    {
        return a1;
    }

    private int MethodA()
    {
        return 1;
    }

    protected virtual string MethodB(int intA)
    {
        return intA.ToString();
    }

    internal static bool MethodC(int intA, bool boolB)
    {
        return boolB;
    }

    public float MethodD(int intA, bool boolB, string stringC)
    {
        return 1.5f;
    }

    void MethodE()
    {
    }

    void MethodE(int intA)
    {
    }
    
    void MethodWithBlankLine()
    {

    }
}

public class B : A
{
    protected override string MethodB(int intA)
    {
        return ""Override!"";
    }
}

public abstract class C
{
    /// <summary>
    /// A short summary.
    /// </summary>
    /// <param name=""intA"">A parameter.</param>
    /// <returns>An int.</returns>
    public abstract int MethodA(int intA);

    // This is a short comment.
    public abstract int MethodB(string foo);

    dynamic DynamicField;
    dynamic DynamicMethod(dynamic foo = 5);
}

public class Entity { }

public class Ref<T> where T : Entity
{
    public static implicit operator Ref<T>(T entity)
    {
        return new Ref<T>(entity);
    }
}

")
        {
        }

        private CodeFunction GetCodeFunction(params object[] path)
        {
            return (CodeFunction)GetCodeElement(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void CanOverride_False()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            Assert.False(testObject.CanOverride);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void CanOverride_True()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodB");

            Assert.True(testObject.CanOverride);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FullName()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodD");

            Assert.Equal("A.MethodD", testObject.FullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FunctionKind_Function()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            Assert.Equal(vsCMFunction.vsCMFunctionFunction, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FunctionKind_Constructor()
        {
            CodeFunction testObject = GetCodeFunction("A", 1);

            Assert.Equal(vsCMFunction.vsCMFunctionConstructor, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FunctionKind_Finalizer()
        {
            CodeFunction testObject = GetCodeFunction("A", 2);

            Assert.Equal(vsCMFunction.vsCMFunctionDestructor, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsOverloaded_True()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodE");

            Assert.True(testObject.IsOverloaded);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsOverloaded_False()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            Assert.False(testObject.IsOverloaded);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsShared_False()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            Assert.False(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsShared_True()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodC");

            Assert.True(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Kind()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            Assert.Equal(vsCMElement.vsCMElementFunction, testObject.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Name()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodC");

            Assert.Equal("MethodC", testObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Parameters_Count()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodD");

            Assert.Equal(3, testObject.Parameters.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Parent()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            Assert.NotNull(testObject.Parent);
            Assert.True(testObject.Parent is CodeClass, testObject.Parent.GetType().ToString());
            Assert.Equal("A", ((CodeClass)testObject.Parent).FullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Type()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            Assert.Equal("System.Int32", testObject.Type.AsFullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Comment()
        {
            CodeFunction testObject = GetCodeFunction("C", "MethodB");

            string expected = "This is a short comment.\r\n";

            Assert.Equal(expected, testObject.Comment);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DocComment()
        {
            CodeFunction testObject = GetCodeFunction("C", "MethodA");

            string expected = "<doc>\r\n<summary>\r\nA short summary.\r\n</summary>\r\n<param name=\"intA\">A parameter.</param>\r\n<returns>An int.</returns>\r\n</doc>";

            Assert.Equal(expected, testObject.DocComment);
        }

        [ConditionalWpfFact(typeof(x86), Skip = "636860")]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Overloads_Count()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodE");

            Assert.Equal(2, testObject.Overloads.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Attributes()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Body()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_BodyWithDelimiter()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Header()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartHeader);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_HeaderWithAttributes()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Name()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Navigate()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(9, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_NavigateWithBlankLine()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodWithBlankLine");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(48, startPoint.Line);
            Assert.Equal(9, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Whole()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_WholeWithAttributes()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Attributes()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_BodyWithDelimiter()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Header()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_HeaderWithAttributes()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Name()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Navigate()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Whole()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_WholeWithAttributes()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(6, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void StartPoint()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint startPoint = testObject.StartPoint;

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void EndPoint()
        {
            CodeFunction testObject = GetCodeFunction("A", "MethodA");

            TextPoint endPoint = testObject.EndPoint;

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(6, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DynamicReturnType()
        {
            CodeVariable testObject = (CodeVariable)GetCodeElement("C", "DynamicField");

            CodeTypeRef returnType = testObject.Type;
            Assert.Equal(returnType.AsFullName, "dynamic");
            Assert.Equal(returnType.AsString, "dynamic");
            Assert.Equal(returnType.CodeType.FullName, "System.Object");
            Assert.Equal(returnType.TypeKind, vsCMTypeRef.vsCMTypeRefOther);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DynamicParameter()
        {
            CodeFunction testObject = GetCodeFunction("C", "DynamicMethod");

            CodeTypeRef returnType = ((CodeParameter)testObject.Parameters.Item(1)).Type;
            Assert.Equal(returnType.AsFullName, "dynamic");
            Assert.Equal(returnType.AsString, "dynamic");
            Assert.Equal(returnType.CodeType.FullName, "System.Object");
            Assert.Equal(returnType.TypeKind, vsCMTypeRef.vsCMTypeRefOther);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(530496)]
        public void TestCodeElementFromPoint()
        {
            var text = CurrentDocument.GetTextAsync().Result;
            var tree = CurrentDocument.GetSyntaxTreeAsync().Result;
            var position = text.ToString().IndexOf("DynamicMethod", StringComparison.Ordinal);
            var virtualTreePoint = new VirtualTreePoint(tree, text, position);
            var textPoint = new MockTextPoint(virtualTreePoint, 4);
            var scope = vsCMElement.vsCMElementFunction;
            var element = CodeModel.CodeElementFromPoint(textPoint, scope);
            Assert.Equal("DynamicMethod", element.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(726710)]
        public void TestCodeElementFromPointBetweenMembers()
        {
            var text = CurrentDocument.GetTextAsync().Result;
            var tree = CurrentDocument.GetSyntaxTreeAsync().Result;
            var position = text.ToString().IndexOf("protected virtual string MethodB", StringComparison.Ordinal) - 1;
            var virtualTreePoint = new VirtualTreePoint(tree, text, position);
            var textPoint = new MockTextPoint(virtualTreePoint, 4);

            Assert.Throws<COMException>(() =>
                CodeModel.CodeElementFromPoint(textPoint, vsCMElement.vsCMElementFunction));

            var element = CodeModel.CodeElementFromPoint(textPoint, vsCMElement.vsCMElementClass);
            Assert.Equal("A", element.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Operator()
        {
            CodeFunction functionObject = GetCodeFunction("A", 3);
            Assert.Equal("operator +", functionObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(924179)]
        public void ConversionOperator()
        {
            CodeClass classObject = (CodeClass)GetCodeElement("Ref");
            var element = classObject.Members.Item(1);
            Assert.Equal("implicit operator Ref<T>", element.Name);
        }
    }
}
