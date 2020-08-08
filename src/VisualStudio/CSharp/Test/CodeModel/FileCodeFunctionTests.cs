// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
    public abstract int MethodB(string goo);

    dynamic DynamicField;
    dynamic DynamicMethod(dynamic goo = 5);
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
            var testObject = GetCodeFunction("A", "MethodA");

            Assert.False(testObject.CanOverride);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void CanOverride_True()
        {
            var testObject = GetCodeFunction("A", "MethodB");

            Assert.True(testObject.CanOverride);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FullName()
        {
            var testObject = GetCodeFunction("A", "MethodD");

            Assert.Equal("A.MethodD", testObject.FullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FunctionKind_Function()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            Assert.Equal(vsCMFunction.vsCMFunctionFunction, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FunctionKind_Constructor()
        {
            var testObject = GetCodeFunction("A", 1);

            Assert.Equal(vsCMFunction.vsCMFunctionConstructor, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FunctionKind_Finalizer()
        {
            var testObject = GetCodeFunction("A", 2);

            Assert.Equal(vsCMFunction.vsCMFunctionDestructor, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsOverloaded_True()
        {
            var testObject = GetCodeFunction("A", "MethodE");

            Assert.True(testObject.IsOverloaded);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsOverloaded_False()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            Assert.False(testObject.IsOverloaded);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsShared_False()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            Assert.False(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void IsShared_True()
        {
            var testObject = GetCodeFunction("A", "MethodC");

            Assert.True(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Kind()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            Assert.Equal(vsCMElement.vsCMElementFunction, testObject.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Name()
        {
            var testObject = GetCodeFunction("A", "MethodC");

            Assert.Equal("MethodC", testObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Parameters_Count()
        {
            var testObject = GetCodeFunction("A", "MethodD");

            Assert.Equal(3, testObject.Parameters.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Parent()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            Assert.NotNull(testObject.Parent);
            Assert.True(testObject.Parent is CodeClass, testObject.Parent.GetType().ToString());
            Assert.Equal("A", ((CodeClass)testObject.Parent).FullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Type()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            Assert.Equal("System.Int32", testObject.Type.AsFullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Comment()
        {
            var testObject = GetCodeFunction("C", "MethodB");

            var expected = "This is a short comment.\r\n";

            Assert.Equal(expected, testObject.Comment);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DocComment()
        {
            var testObject = GetCodeFunction("C", "MethodA");

            var expected = "<doc>\r\n<summary>\r\nA short summary.\r\n</summary>\r\n<param name=\"intA\">A parameter.</param>\r\n<returns>An int.</returns>\r\n</doc>";

            Assert.Equal(expected, testObject.DocComment);
        }

        [ConditionalFact(typeof(x86), AlwaysSkip = "636860")]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Overloads_Count()
        {
            var testObject = GetCodeFunction("A", "MethodE");

            Assert.Equal(2, testObject.Overloads.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Attributes()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Body()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_BodyWithDelimiter()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Header()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartHeader);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_HeaderWithAttributes()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Name()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Navigate()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(9, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_NavigateWithBlankLine()
        {
            var testObject = GetCodeFunction("A", "MethodWithBlankLine");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(48, startPoint.Line);
            Assert.Equal(9, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Whole()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_WholeWithAttributes()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Attributes()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_BodyWithDelimiter()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Header()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_HeaderWithAttributes()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Name()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Navigate()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Whole()
        {
            var testObject = GetCodeFunction("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_WholeWithAttributes()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(6, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void StartPoint()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var startPoint = testObject.StartPoint;

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void EndPoint()
        {
            var testObject = GetCodeFunction("A", "MethodA");

            var endPoint = testObject.EndPoint;

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(6, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DynamicReturnType()
        {
            var testObject = (CodeVariable)GetCodeElement("C", "DynamicField");

            var returnType = testObject.Type;
            Assert.Equal("dynamic", returnType.AsFullName);
            Assert.Equal("dynamic", returnType.AsString);
            Assert.Equal("System.Object", returnType.CodeType.FullName);
            Assert.Equal(vsCMTypeRef.vsCMTypeRefOther, returnType.TypeKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void DynamicParameter()
        {
            var testObject = GetCodeFunction("C", "DynamicMethod");

            var returnType = ((CodeParameter)testObject.Parameters.Item(1)).Type;
            Assert.Equal("dynamic", returnType.AsFullName);
            Assert.Equal("dynamic", returnType.AsString);
            Assert.Equal("System.Object", returnType.CodeType.FullName);
            Assert.Equal(vsCMTypeRef.vsCMTypeRefOther, returnType.TypeKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(530496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530496")]
        public async Task TestCodeElementFromPoint()
        {
            var text = await (GetCurrentDocument()).GetTextAsync();
            var tree = await (GetCurrentDocument()).GetSyntaxTreeAsync();
            var position = text.ToString().IndexOf("DynamicMethod", StringComparison.Ordinal);
            var virtualTreePoint = new VirtualTreePoint(tree, text, position);
            var textPoint = new MockTextPoint(virtualTreePoint);
            var scope = vsCMElement.vsCMElementFunction;
            var element = (GetCodeModel()).CodeElementFromPoint(textPoint, scope);
            Assert.Equal("DynamicMethod", element.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(726710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726710")]
        public async Task TestCodeElementFromPointBetweenMembers()
        {
            var text = await (GetCurrentDocument()).GetTextAsync();
            var tree = await (GetCurrentDocument()).GetSyntaxTreeAsync();
            var position = text.ToString().IndexOf("protected virtual string MethodB", StringComparison.Ordinal) - 1;
            var virtualTreePoint = new VirtualTreePoint(tree, text, position);
            var textPoint = new MockTextPoint(virtualTreePoint);

            Assert.Throws<COMException>(() =>
                (GetCodeModel()).CodeElementFromPoint(textPoint, vsCMElement.vsCMElementFunction));

            var element = (GetCodeModel()).CodeElementFromPoint(textPoint, vsCMElement.vsCMElementClass);
            Assert.Equal("A", element.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Operator()
        {
            var functionObject = GetCodeFunction("A", 3);
            Assert.Equal("operator +", functionObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(924179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924179")]
        public void ConversionOperator()
        {
            var classObject = (CodeClass)GetCodeElement("Ref");
            var element = classObject.Members.Item(1);
            Assert.Equal("implicit operator Ref<T>", element.Name);
        }
    }
}
