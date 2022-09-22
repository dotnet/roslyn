' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_Conversions
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte)x)", state.GetLineTextFromCaretPosition())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_BetweenDots(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.$$.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte)x).ToString();", state.GetLineTextFromCaretPosition())
                Assert.Equal(".", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_PartiallyWritten_Before(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.$$by.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("CompareTo")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_PartiallyWritten_After(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.by$$.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("(byte)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte)x).ToString();", state.GetLineTextFromCaretPosition())
                Assert.Equal(".", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_NullableType_Dot(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = (int?)0;
        var y = x.$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte?)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte?)x)", state.GetLineTextFromCaretPosition())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_NullableType_Question_BetweenDots(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = (int?)0;
        var y = x?.$$.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte?)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte?)x)?.ToString();", state.GetLineTextFromCaretPosition())
                Assert.Equal(".", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        Private Shared Async Function VerifyCustomCommitProviderAsync(markupBeforeCommit As String, itemToCommit As String, expectedCodeAfterCommit As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                    New XElement("Document", markupBeforeCommit.Replace(vbCrLf, vbLf)))

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem(itemToCommit)
                state.SendTab()
                Await state.AssertNoCompletionSession()

                Dim expected As String = Nothing
                Dim cursorPosition As Integer = 0
                MarkupTestFile.GetPosition(expectedCodeAfterCommit, expected, cursorPosition)

                Assert.Equal(expected, state.SubjectBuffer.CurrentSnapshot.GetText())
                Assert.Equal(cursorPosition, state.TextView.Caret.Position.BufferPosition.Position)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitBuiltInEnumConversionsIsApplied() As Task
            ' built-in enum conversions:
            ' https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-enumeration-conversions
            Await VerifyCustomCommitProviderAsync("
public enum E { One }
public class Program
{
    public static void Main()
    {
        var e = E.One;
        e.$$
    }
}
", "(int)", "
public enum E { One }
public class Program
{
    public static void Main()
    {
        var e = E.One;
        ((int)e)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitBuiltInEnumConversionsAreLifted() As Task
            ' built-in enum conversions:
            ' https//docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-enumeration-conversions
            Await VerifyCustomCommitProviderAsync("
public enum E { One }
public class Program
{
    public static void Main()
    {
        E? e = null;
        e.$$
    }
}
", "(int?)", "
public enum E { One }
public class Program
{
    public static void Main()
    {
        E? e = null;
        ((int?)e)$$
    }
}
")

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitBuiltInNumericConversionsAreLifted() As Task
            ' built-in numeric conversions:
            ' https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/numeric-conversions
            Await VerifyCustomCommitProviderAsync("
public class Program
{
    public static void Main()
    {
        long? l = 0;
        l.$$
    }
}
", "(int?)", "
public class Program
{
    public static void Main()
    {
        long? l = 0;
        ((int?)l)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitBuiltInNumericConversionsAreOffered() As Task
            ' built-in numeric conversions:
            ' https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/numeric-conversions
            Await VerifyCustomCommitProviderAsync("
public class Program
{
    public static void Main()
    {
        long l = 0;
        l.$$
    }
}
", "(int)", "
public class Program
{
    public static void Main()
    {
        long l = 0;
        ((int)l)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitUserDefinedConversionNullForgivingOperatorHandling() As Task
            Await VerifyCustomCommitProviderAsync("
#nullable enable

public class C {
    public static explicit operator int(C c) => 0;
}

public class Program
{
    public static void Main()
    {
        C? c = null;
        var i = c!.$$
    }
}
", "(int)", "
#nullable enable

public class C {
    public static explicit operator int(C c) => 0;
}

public class Program
{
    public static void Main()
    {
        C? c = null;
        var i = ((int)c!)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitConversionOfConditionalAccessOfStructAppliesNullableStruct() As Task
            ' see https://sharplab.io/#gist:08c697b6b9b6384b8ec81cc586e064e6 to run a sample
            ' conversion ((int)c?.S) fails with System.InvalidOperationException: Nullable object must have a value.
            ' conversion ((int?)c?.S) passes (returns an int? with HasValue == false)
            Await VerifyCustomCommitProviderAsync("
public struct S {
    public static explicit operator int(S _) => 0;
}
public class C {
    public S S { get; } = default;
}
public class Program
{
    public static void Main()
    {
        C c = null;
        c?.S.$$
    }
}
", "(int?)", "
public struct S {
    public static explicit operator int(S _) => 0;
}
public class C {
    public S S { get; } = default;
}
public class Program
{
    public static void Main()
    {
        C c = null;
        ((int?)c?.S)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitConversionOfNullableStructToNullableStructIsApplied() As Task
            ' Lifted conversion https://docs.microsoft.com/hu-hu/dotnet/csharp/language-reference/language-specification/conversions#lifted-conversion-operators
            Await VerifyCustomCommitProviderAsync("
public struct S {
    public static explicit operator int(S _) => 0;
}
public class Program
{
    public static void Main()
    {
        S? s = null;
        s.$$
    }
}
", "(int?)", "
public struct S {
    public static explicit operator int(S _) => 0;
}
public class Program
{
    public static void Main()
    {
        S? s = null;
        ((int?)s)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitUserDefinedConversionOfNullableStructAccessViaNullcondionalOffersLiftedConversion() As Task
            Await VerifyCustomCommitProviderAsync("
public struct S {
    public static explicit operator int(S s) => 0;
}
public class Program
{
    public static void Main()
    {
        S? s = null;
        var i = ((S?)s)?.$$
    }
}
", "(int?)", "
public struct S {
    public static explicit operator int(S s) => 0;
}
public class Program
{
    public static void Main()
    {
        S? s = null;
        var i = ((int?)((S?)s))?$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitUserDefinedConversionOfPropertyNamedLikeItsTypeIsHandled() As Task
            Await VerifyCustomCommitProviderAsync("
public struct S {
    public static explicit operator int(S s) => 0;
}
public class C {
    public S S { get; }
}
public class Program
{
    public static void Main()
    {
        var c = new C();
        var i = c.S.$$
    }
}
", "(int)", "
public struct S {
    public static explicit operator int(S s) => 0;
}
public class C {
    public S S { get; }
}
public class Program
{
    public static void Main()
    {
        var c = new C();
        var i = ((int)c.S)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitUserDefinedConversionIsApplied() As Task
            Await VerifyCustomCommitProviderAsync("
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public static void Main()
    {
        var c = new C();
        c.$$
    }
}
", "(float)", "
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public static void Main()
    {
        var c = new C();
        ((float)c)$$
    }
}
")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitUserDefinedConversionToArray() As Task
            Await VerifyCustomCommitProviderAsync(
"
public class C
{
    public static explicit operator int[](C _) => default;
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C();
            c.$$
        }
    }
}
", "(int[])",
"
public class C
{
    public static explicit operator int[](C _) => default;
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C();
            ((int[])c)$$
        }
    }
}
"
            )
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        Public Async Function ExplicitUserDefinedConversionToGenericType() As Task
            Await VerifyCustomCommitProviderAsync(
"
public class C<T>
{
    public static explicit operator D<T>(C<T> _) => default;
}
public class D<T>
{
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C<int>();
            c.$$
        }
    }
}
", "(D<int>)",
"
public class C<T>
{
    public static explicit operator D<T>(C<T> _) => default;
}
public class D<T>
{
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C<int>();
            ((D<int>)c)$$
        }
    }
}
"
            )
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        <CombinatorialData>
        Public Async Function ExplicitConversionOfConditionalAccessFromClassOrStructToClassOrStruct(
            <CombinatorialValues("struct", "class")> fromClassOrStruct As String,
            <CombinatorialValues("struct", "class")> toClassOrStruct As String,
            propertyIsNullable As Boolean,
            conditionalAccess As Boolean) As Task

            If fromClassOrStruct = "class" AndAlso propertyIsNullable Then
                ' This test Is solely about lifting of nullable value types. The CombinatorialData also 
                ' adds cases for nullable reference types: public class From ... public From? From { get; }
                ' We don't want to test NRT cases here.
                Return
            End If

            Dim assertShouldBeNullable =
                fromClassOrStruct = "struct" AndAlso
                toClassOrStruct = "struct" AndAlso
                (propertyIsNullable OrElse conditionalAccess)

            Dim propertyNullableQuestionMark = If(propertyIsNullable, "?", "")
            Dim conditionalAccessQuestionMark = If(conditionalAccess, "?", "")
            Dim shouldBeNullableQuestionMark = If(assertShouldBeNullable, "?", "")
            Await VerifyCustomCommitProviderAsync($"
public {fromClassOrStruct} From {{
    public static explicit operator To(From _) => default;
}}
public {toClassOrStruct} To {{
}}
public class C {{
    public From{propertyNullableQuestionMark} From {{ get; }} = default;
}}
public class Program
{{
    public static void Main()
    {{
        C c = null;
        c{conditionalAccessQuestionMark}.From.$$
    }}
}}
", $"(To{shouldBeNullableQuestionMark})", $"
public {fromClassOrStruct} From {{
    public static explicit operator To(From _) => default;
}}
public {toClassOrStruct} To {{
}}
public class C {{
    public From{propertyNullableQuestionMark} From {{ get; }} = default;
}}
public class Program
{{
    public static void Main()
    {{
        C c = null;
        ((To{shouldBeNullableQuestionMark})c{conditionalAccessQuestionMark}.From)$$
    }}
}}
")
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        <InlineData("bool")>
        <InlineData("byte")>
        <InlineData("sbyte")>
        <InlineData("char")>
        <InlineData("decimal")>
        <InlineData("double")>
        <InlineData("float")>
        <InlineData("int")>
        <InlineData("uint")>
        <InlineData("long")>
        <InlineData("ulong")>
        <InlineData("short")>
        <InlineData("ushort")>
        <InlineData("object")>
        <InlineData("string")>
        <InlineData("dynamic")>
        Public Async Function ExplicitUserDefinedConversionIsAppliedForBuiltinTypeKeywords(builtinType As String) As Task
            Await VerifyCustomCommitProviderAsync($"
namespace N
{{
    public class C
    {{
        public static explicit operator {builtinType}(C _) => default;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            c.{builtinType}$$
        }}
    }}
}}
", $"({builtinType})", $"
namespace N
{{
    public class C
    {{
        public static explicit operator {builtinType}(C _) => default;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            (({builtinType})c)$$
        }}
    }}
}}
")
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        <InlineData("white.$$", "Black", "((Black)white)$$")>
        <InlineData("white.$$;", "Black", "((Black)white)$$;")>
        <InlineData("white.Bl$$", "Black", "((Black)white)$$")>
        <InlineData("white.Bl$$;", "Black", "((Black)white)$$;")>
        <InlineData("white?.Bl$$;", "Black", "((Black)white)?$$;")>
        <InlineData("white.$$Bl;", "Black", "((Black)white)$$Bl;")>
        <InlineData("var f = white.$$;", "Black", "var f = ((Black)white)$$;")>
        <InlineData("white?.$$", "Black", "((Black)white)?$$")>
        <InlineData("white?.$$b", "Black", "((Black)white)?$$b")>
        <InlineData("white?.$$b.c()", "Black", "((Black)white)?$$b.c()")>
        <InlineData("white?.$$b()", "Black", "((Black)white)?$$b()")>
        <InlineData("white.Black?.$$", "White", "((White)white.Black)?$$")>
        <InlineData("white.Black.$$", "White", "((White)white.Black)$$")>
        <InlineData("white?.Black?.$$", "White", "((White)white?.Black)?$$")>
        <InlineData("white?.Black?.fl$$", "White", "((White)white?.Black)?$$")>
        <InlineData("white?.Black.fl$$", "White", "((White)white?.Black)$$")>
        <InlineData("white?.Black.White.Bl$$ack?.White", "Black", "((Black)white?.Black.White)$$?.White")>
        <InlineData("((White)white).$$", "Black", "((Black)((White)white))$$")>
        <InlineData("(true ? white : white).$$", "Black", "((Black)(true ? white : white))$$")>
        Public Async Function ExplicitUserDefinedConversionIsAppliedForDifferentExpressions(expression As String, conversionOffering As String, fixedCode As String) As Task
            Await VerifyCustomCommitProviderAsync($"
namespace N
{{
    public class Black
    {{
        public White White {{ get; }}
        public static explicit operator White(Black _) => new White();
    }}
    public class White
    {{
        public Black Black {{ get; }}
        public static explicit operator Black(White _) => new Black();
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var white = new White();
            {expression}
        }}
    }}
}}
", $"({conversionOffering})", $"
namespace N
{{
    public class Black
    {{
        public White White {{ get; }}
        public static explicit operator White(Black _) => new White();
    }}
    public class White
    {{
        public Black Black {{ get; }}
        public static explicit operator Black(White _) => new Black();
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var white = new White();
            {fixedCode}
        }}
    }}
}}
")
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        <InlineData("/* Leading */c.$$", "/* Leading */((float)c)$$")>
        <InlineData("/* Leading */c.fl$$", "/* Leading */((float)c)$$")>
        <InlineData("c.  $$", "((float)c)  $$")>
        <InlineData("(true ? /* Inline */ c : c).$$", "((float)(true ? /* Inline */ c : c))$$")>
        <InlineData("c.fl$$ /* Trailing */", "((float)c)$$ /* Trailing */")>
        Public Async Function ExplicitUserDefinedConversionTriviaHandling(expression As String, fixedCode As String) As Task
            Await VerifyCustomCommitProviderAsync($"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {expression}
    }}
}}
", "(float)", $"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {fixedCode}
    }}
}}
")
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")>
        <InlineData("abstract")>
        <InlineData("as")>
        <InlineData("base")>
        <InlineData("bool")>
        <InlineData("break")>
        <InlineData("byte")>
        <InlineData("case")>
        <InlineData("catch")>
        <InlineData("char")>
        <InlineData("checked")>
        <InlineData("class")>
        <InlineData("const")>
        <InlineData("continue")>
        <InlineData("decimal")>
        <InlineData("default")>
        <InlineData("delegate")>
        <InlineData("do")>
        <InlineData("double")>
        <InlineData("else")>
        <InlineData("enum")>
        <InlineData("event")>
        <InlineData("explicit")>
        <InlineData("extern")>
        <InlineData("false")>
        <InlineData("finally")>
        <InlineData("fixed")>
        <InlineData("float")>
        <InlineData("for")>
        <InlineData("foreach")>
        <InlineData("goto")>
        <InlineData("if")>
        <InlineData("implicit")>
        <InlineData("in")>
        <InlineData("int")>
        <InlineData("interface")>
        <InlineData("internal")>
        <InlineData("is")>
        <InlineData("lock")>
        <InlineData("long")>
        <InlineData("namespace")>
        <InlineData("new")>
        <InlineData("null")>
        <InlineData("object")>
        <InlineData("operator")>
        <InlineData("out")>
        <InlineData("override")>
        <InlineData("params")>
        <InlineData("private")>
        <InlineData("protected")>
        <InlineData("public")>
        <InlineData("readonly")>
        <InlineData("ref")>
        <InlineData("return")>
        <InlineData("sbyte")>
        <InlineData("sealed")>
        <InlineData("short")>
        <InlineData("sizeof")>
        <InlineData("stackalloc")>
        <InlineData("static")>
        <InlineData("string")>
        <InlineData("struct")>
        <InlineData("switch")>
        <InlineData("this")>
        <InlineData("throw")>
        <InlineData("true")>
        <InlineData("try")>
        <InlineData("typeof")>
        <InlineData("uint")>
        <InlineData("ulong")>
        <InlineData("unchecked")>
        <InlineData("unsafe")>
        <InlineData("ushort")>
        <InlineData("using")>
        <InlineData("virtual")>
        <InlineData("void")>
        <InlineData("volatile")>
        <InlineData("while")>
        <InlineData("add")>
        <InlineData("alias")>
        <InlineData("ascending")>
        <InlineData("async")>
        <InlineData("await")>
        <InlineData("by")>
        <InlineData("descending")>
        <InlineData("dynamic")>
        <InlineData("equals")>
        <InlineData("from")>
        <InlineData("get")>
        <InlineData("global")>
        <InlineData("group")>
        <InlineData("into")>
        <InlineData("join")>
        <InlineData("let")>
        <InlineData("nameof")>
        <InlineData("notnull")>
        <InlineData("on")>
        <InlineData("orderby")>
        <InlineData("partial")>
        <InlineData("remove")>
        <InlineData("select")>
        <InlineData("set")>
        <InlineData("unmanaged")>
        <InlineData("value")>
        <InlineData("var")>
        <InlineData("when")>
        <InlineData("where")>
        <InlineData("yield")>
        public async Function ExplicitUserDefinedConversionIsAppliedForOtherKeywords(keyword As string) As Task
            Await VerifyCustomCommitProviderAsync($"
namespace N
{{
    public class {keyword}Class
    {{
    }}
    public class C
    {{
        public static explicit operator {keyword}Class(C _) => new {keyword}Class;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            c.{keyword}$$
        }}
    }}
}}
", $"({keyword}Class)", $"
namespace N
{{
    public class {keyword}Class
    {{
    }}
    public class C
    {{
        public static explicit operator {keyword}Class(C _) => new {keyword}Class;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            (({keyword}Class)c)$$
        }}
    }}
}}
")
        End Function
    End Class
End Namespace
