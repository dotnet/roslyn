// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

        private async Task<CodeFunction> GetCodeFunctionAsync(params object[] path)
        {
            return (CodeFunction)await GetCodeElementAsync(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task CanOverride_False()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            Assert.False(testObject.CanOverride);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task CanOverride_True()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodB");

            Assert.True(testObject.CanOverride);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task FullName()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodD");

            Assert.Equal("A.MethodD", testObject.FullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task FunctionKind_Function()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            Assert.Equal(vsCMFunction.vsCMFunctionFunction, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task FunctionKind_Constructor()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", 1);

            Assert.Equal(vsCMFunction.vsCMFunctionConstructor, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task FunctionKind_Finalizer()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", 2);

            Assert.Equal(vsCMFunction.vsCMFunctionDestructor, testObject.FunctionKind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsOverloaded_True()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodE");

            Assert.True(testObject.IsOverloaded);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsOverloaded_False()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            Assert.False(testObject.IsOverloaded);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsShared_False()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            Assert.False(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsShared_True()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodC");

            Assert.True(testObject.IsShared);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Kind()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            Assert.Equal(vsCMElement.vsCMElementFunction, testObject.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Name()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodC");

            Assert.Equal("MethodC", testObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Parameters_Count()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodD");

            Assert.Equal(3, testObject.Parameters.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Parent()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            Assert.NotNull(testObject.Parent);
            Assert.True(testObject.Parent is CodeClass, testObject.Parent.GetType().ToString());
            Assert.Equal("A", ((CodeClass)testObject.Parent).FullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Type()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            Assert.Equal("System.Int32", testObject.Type.AsFullName);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Comment()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("C", "MethodB");

            string expected = "This is a short comment.\r\n";

            Assert.Equal(expected, testObject.Comment);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task DocComment()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("C", "MethodA");

            string expected = "<doc>\r\n<summary>\r\nA short summary.\r\n</summary>\r\n<param name=\"intA\">A parameter.</param>\r\n<returns>An int.</returns>\r\n</doc>";

            Assert.Equal(expected, testObject.DocComment);
        }

        [ConditionalFact(typeof(x86), Skip = "636860")]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Overloads_Count()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodE");

            Assert.Equal(2, testObject.Overloads.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Attributes()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_AttributesWithDelimiter()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Body()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_BodyWithDelimiter()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Header()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartHeader);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_HeaderWithAttributes()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Name()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Navigate()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(9, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_NavigateWithBlankLine()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodWithBlankLine");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(48, startPoint.Line);
            Assert.Equal(9, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Whole()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_WholeWithAttributes()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Attributes()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_AttributesWithDelimiter()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_BodyWithDelimiter()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Header()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_HeaderWithAttributes()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Name()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Navigate()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Whole()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_WholeWithAttributes()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(6, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task StartPoint()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint startPoint = testObject.StartPoint;

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(5, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task EndPoint()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("A", "MethodA");

            TextPoint endPoint = testObject.EndPoint;

            Assert.Equal(21, endPoint.Line);
            Assert.Equal(6, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task DynamicReturnType()
        {
            CodeVariable testObject = (CodeVariable)await GetCodeElementAsync("C", "DynamicField");

            CodeTypeRef returnType = testObject.Type;
            Assert.Equal(returnType.AsFullName, "dynamic");
            Assert.Equal(returnType.AsString, "dynamic");
            Assert.Equal(returnType.CodeType.FullName, "System.Object");
            Assert.Equal(returnType.TypeKind, vsCMTypeRef.vsCMTypeRefOther);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task DynamicParameter()
        {
            CodeFunction testObject = await GetCodeFunctionAsync("C", "DynamicMethod");

            CodeTypeRef returnType = ((CodeParameter)testObject.Parameters.Item(1)).Type;
            Assert.Equal(returnType.AsFullName, "dynamic");
            Assert.Equal(returnType.AsString, "dynamic");
            Assert.Equal(returnType.CodeType.FullName, "System.Object");
            Assert.Equal(returnType.TypeKind, vsCMTypeRef.vsCMTypeRefOther);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(530496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530496")]
        public async Task TestCodeElementFromPoint()
        {
            var text = await (await GetCurrentDocumentAsync()).GetTextAsync();
            var tree = await (await GetCurrentDocumentAsync()).GetSyntaxTreeAsync();
            var position = text.ToString().IndexOf("DynamicMethod", StringComparison.Ordinal);
            var virtualTreePoint = new VirtualTreePoint(tree, text, position);
            var textPoint = new MockTextPoint(virtualTreePoint, 4);
            var scope = vsCMElement.vsCMElementFunction;
            var element = (await GetCodeModelAsync()).CodeElementFromPoint(textPoint, scope);
            Assert.Equal("DynamicMethod", element.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(726710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726710")]
        public async Task TestCodeElementFromPointBetweenMembers()
        {
            var text = await (await GetCurrentDocumentAsync()).GetTextAsync();
            var tree = await (await GetCurrentDocumentAsync()).GetSyntaxTreeAsync();
            var position = text.ToString().IndexOf("protected virtual string MethodB", StringComparison.Ordinal) - 1;
            var virtualTreePoint = new VirtualTreePoint(tree, text, position);
            var textPoint = new MockTextPoint(virtualTreePoint, 4);

            await Assert.ThrowsAsync<COMException>(async () =>
                (await GetCodeModelAsync()).CodeElementFromPoint(textPoint, vsCMElement.vsCMElementFunction));

            var element = (await GetCodeModelAsync()).CodeElementFromPoint(textPoint, vsCMElement.vsCMElementClass);
            Assert.Equal("A", element.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Operator()
        {
            CodeFunction functionObject = await GetCodeFunctionAsync("A", 3);
            Assert.Equal("operator +", functionObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        [WorkItem(924179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924179")]
        public async Task ConversionOperator()
        {
            CodeClass classObject = (CodeClass)await GetCodeElementAsync("Ref");
            var element = classObject.Members.Item(1);
            Assert.Equal("implicit operator Ref<T>", element.Name);
        }
    }
}
