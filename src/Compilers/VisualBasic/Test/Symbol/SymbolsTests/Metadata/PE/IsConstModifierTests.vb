' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata

    Public Class IsConstModifierTests
        Inherits BasicTestBase

        <Fact>
        Public Sub NonVirtualReadOnlySignaturesAreRead()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    public void M(ref readonly int x)
    {
        System.Console.WriteLine(x);
    }
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim x = 5
        Dim obj = New TestRef()
        obj.M(x)
    End Sub
End Class
    </file>
                </compilation>

            CompileAndVerify(source, additionalRefs:={reference}, expectedOutput:="5")
        End Sub

        <Fact>
        Public Sub ReadOnlySignaturesAreNotSupported_Methods_Parameters_Virtual()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    public virtual void M(ref readonly int x)
    {
        System.Console.WriteLine(x);
    }
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim x = 5
        Dim obj = New TestRef()
        obj.M(x)
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
        obj.M(x)
            ~
</expected>)
        End Sub

        <Fact>
        Public Sub ReadOnlySignaturesAreNotSupported_Methods_ReturnTypes_Virtual()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    private int value = 0;
    public virtual ref readonly int M()
    {
        return ref value;
    }
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim obj = New TestRef()
        obj.M()
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
        obj.M()
            ~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub ReadOnlySignaturesAreNotSupported_Properties_Virtual()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    private int value = 0;
    public virtual ref readonly int P => ref value;
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim obj = New TestRef()
        Dim value = obj.P
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30643: Property 'P' is of an unsupported type.
        Dim value = obj.P
                        ~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub ReadOnlySignaturesAreNotSupported_Indexers_ReturnTypes_Virtual()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    private int value = 0;
    public virtual ref readonly int this[int p] => ref value;
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim obj = New TestRef()
        Dim value = obj(0)
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30643: Property 'TestRef.Item(p As )' is of an unsupported type.
        Dim value = obj(0)
                    ~~~
                                                </expected>)
        End Sub

        <Fact>
        Public Sub ReadOnlySignaturesAreNotSupported_Indexers_Parameters_Virtual()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    public virtual int this[ref readonly int p] => 0;
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim p = 0
        Dim obj = New TestRef()
        Dim value = obj(p)
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC30643: Property 'TestRef.Item(p As )' is of an unsupported type.
        Dim value = obj(p)
                    ~~~
                                                </expected>)
        End Sub
    End Class

End Namespace