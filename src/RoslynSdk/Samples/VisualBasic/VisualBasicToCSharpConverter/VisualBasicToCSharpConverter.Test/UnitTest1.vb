Option Strict Off

Imports Microsoft.CodeAnalysis
Imports VisualBasicToCSharpConverter
Imports Xunit
Imports CS = Microsoft.CodeAnalysis.CSharp
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace VisualBasicToCSharpConverter.UnitTests.Converting

    Public Class VisualBasicToCSharpConverterTests

        ' File-level or project-level code snippets would be nice :).
        '<Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestTemplate()

            AssertConversion(
<String>

</String>,
<String>

</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertSimpleTypes()

            AssertConversion(
<String>
Class C

End Class

Interface I

End Interface

Structure S

End Structure

Delegate Sub D1()

Delegate Sub D2(p1 As T1)

Delegate Function D3(p1 As T1, ByRef p2 As T2) As ReturnType

Enum E
    A
    B
    C
End Enum
</String>,
<String>
class C
{
}

interface I
{
}

struct S
{
}

delegate void D1();

delegate void D2(T1 p1);

delegate ReturnType D3(T1 p1, ref T2 p2);

enum E
{
    A, 
    B, 
    C
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertLoadedSimpleTypes()

            AssertConversion(
<String>
' This is a comment.
&lt;System.Serializable()&gt;
Friend MustInherit Class C(Of T As {New, IDisposable, Foo}, U As T)
    Inherits Object
    Implements IDisposable, IA, IB
    Implements IC, ID

End Class

Public Delegate Sub Action()
Private Delegate Sub Action(Of In T)(arg0 As T)
Delegate Function Func(Of Out T)() As T

' Trivia.
&lt;Global.System.Flags&gt;
Public Enum E As UShort
    ' Trivia.
    A = 0
    B = 1 >> 0 ' Trivia.
    C = 1 >> 1
    &lt;DisplayName("B and C")&gt;
    D = B And C
End Enum
</String>,
<String>
// This is a comment.
[System.Serializable()]
internal abstract class C&lt;T, U&gt; : object, IDisposable, IA, IB, IC, ID where T : IDisposable, Foo, new() where U : T
{
}

public delegate void Action();
private delegate void Action&lt;in T&gt;(T arg0);
delegate T Func&lt;out T&gt;();

// Trivia.
[global::System.Flags]
public enum E : ushort
{
    // Trivia.
    A = 0,
    B = 1 >> 0 // Trivia.
,
    C = 1 >> 1,
    [DisplayName("B and C")]
    D = B &amp; C
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertFieldsAndLocalVariables()
            'Array modifiers aren't supported yet.
            'Private F5(), F6?(), F7 As T3

            AssertConversion(
<String>
Class C

    Private F1 As T1, F2, F3 As T2, F4?, F5 As T3

    Sub M()
        Dim l1 As T1, l2, l3 As T2, l4?, l5 As T3
    End Sub

End Class
</String>,
<String>
class C
{
    private T1 F1;
    private T2 F2;
    private T2 F3;
    private T3? F4;
    private T3 F5;

    void M()
    {
        T1 l1;
        T2 l2;
        T2 l3;
        T3? l4;
        T3 l5;
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertTypeCharactersAndVariableModifiers()

            AssertConversion(
<String>
Class C
    Sub M()
        Dim i%, o, s$
        Dim arr%(), arr2%?()
        Dim arr3() As Integer, arr4 As Integer?()
    End Sub
End Class
</String>,
<String>
class C
{
    void M()
    {
        int i;
        dynamic o;
        string s;
        int[] arr;
        int?[] arr2;
        int[] arr3;
        int?[] arr4;
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertAsNewAndInitializers()

            AssertConversion(
<String>
Class C
    Sub M()
        Dim obj As New C
        Dim a, b, c As New C("Hello")
        Dim d = New C() With {.Text = "Hello"}
        Dim e As New C() With {.Text = "Goodbye"}
        Dim f = New List(Of Integer) From { 1, 2, 3 }
        Dim g As New List(Of Integer) From { 1, 2, 3 }
    End Sub
End Class
</String>,
<String>
class C
{
    void M()
    {
        var obj = new C();
        var a = new C("Hello");
        var b = new C("Hello");
        var c = new C("Hello");
        var d = new C() { Text = "Hello" };
        var e = new C() { Text = "Goodbye" };
        var f = new List&lt;int&gt;() { 1, 2, 3 };
        var g = new List&lt;int&gt;() { 1, 2, 3 };
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertInterfaceMembers()

            AssertConversion(
<String>
Interface IA
    Inherits IB, IC
    Inherits ID

    Sub M()

    Function N() As String

    ReadOnly Property P1 As Char

    Property P2 As Object

End Interface
</String>,
<String>
interface IA : IB, IC, ID
{

    void M();

    string N();

    char P1 { get; }

    object P2 { get; set; }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertArrayDeclarations()

            AssertConversion(
<String>
Class C
    Sub M()
        Dim a() As Integer
        Dim b(1024 - 1) As Byte
        Dim c(1023) As Byte
        Dim d(0 To -1) As Object
        Dim e = New String() {}
        Dim f = New String(10, 10) {}
        Dim g(5)(,) As Double
        Dim h = New Single(0 To 6, 0 To 8)(,,)(,)() {}
        Dim importantDates = new Date() {Date.MinValue, Date.Now, Date.MaxValue}
        Dim nulls() As Object = {Nothing, Nothing, Nothing}
    End Sub
End Class
</String>,
<String>
class C
{
    void M()
    {
        int[] a;
        byte[] b = new byte[1024];
        byte[] c = new byte[1024];
        object[] d = new object[0];
        var e = new string[] {};
        var f = new string[11, 11];
        double[][,] g = new double[6][,];
        var h = new float[7, 9][,,][,][];
        var importantDates = new global::System.DateTime[] {global::System.DateTime.MinValue, global::System.DateTime.Now, global::System.DateTime.MaxValue};
        object[] nulls = {null, null, null};
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertAbstractMembers()

            AssertConversion(
<String>
MustInherit Class C
    MustOverride Function M1() As Integer

    Public MustOverride Readonly Property P1 As Decimal

    Protected Overridable Sub M2()

    End Sub
End Class
</String>,
<String>
abstract class C
{

    abstract int M1();

    public abstract decimal P1 { get; }

    protected virtual void M2()
    {
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertCompilationUnit()

            AssertConversion(
<String>
Imports System
Imports System.Collections.Generics
Imports System.Windows, System.Windows.Forms

&lt;Assembly: A(), Module: B()&gt;
&lt;Assembly: C&gt;
</String>,
<String>
using System;
using System.Collections.Generics;
using System.Windows;
using System.Windows.Forms;

[assembly: A()]
[module: B()]
[assembly: C]
</String>
            )

        End Sub

        <Fact>
        Sub TestConvertMembers()

            AssertConversion(
<String>
Class C

    Sub New(p1 As T1)
        Me.New()
    End Sub

    ' Trivia.
    Property P1 As T1

    Protected Property P2 As T2
        Get
            Return Nothing
        End Get
        Set(value As T2)

        End Set
    End Property
    
    ' Trivia.
    Private ReadOnly Property P3 As T3
        Get
            Return Nothing
        End Get
    End Property

    Sub M1()
        MyBase.M1()
    End Sub

    Sub M2(ByRef p1 As T1, Optional p2 As T2 = 1)

    End Sub

    Function M3(Of T As Structure)() As Date

    End Function

    Public Event Click As EventHandler

    Public Shared Operator +(a As C, b As C) As C
        Return "Empty"
    End Operator

    Public Shared Narrowing Operator CType(value As String) As C

    End Operator
End Class
</String>,
<String>
class C
{
    C(T1 p1) : this()
    {
    }    

    // Trivia.
    T1 P1 { get; set; }

    protected T2 P2
    {
        get
        {
            return null;
        }
        set
        {
        }
    }

    // Trivia.
    private T3 P3
    {
        get
        {
            return null;
        }
    }

    void M1()
    {
        base.M1();
    }

    void M2(ref T1 p1, T2 p2 = 1)
    {
    }

    global::System.DateTime M3&lt;T&gt;() where T : struct
    {
    }

    public event EventHandler Click;

    public static C operator +(C a, C b)
    {
        return "Empty";
    }

    public static explicit operator C(string value)
    {
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertNamespace()

            ' TODO: Test RootNamespace.
            AssertConversion(
<String>
Namespace A
    Class C

    End Class

    Namespace B
        Namespace D.E.F

        End Namespace
    End Namespace
End Namespace

Namespace A.B.D

End Namespace

Namespace Global.G

End Namespace
</String>,
<String>
namespace A
{
    class C
    {
    }

    namespace B
    {
        namespace D.E.F
        {
        }
    }
}

namespace A.B.D
{
}

namespace G
{
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertTrySyncUsing()

            AssertConversion(
<String>
Class C
    Sub M()
        ' Try-Catch-All.
        Try
            Connection.Open()
        Catch
            Throw
        End Try

        ' Try-Finally.
        Try
            Connection.Open()

            Connection.Close()
        Finally
            If Connection IsNot Nothing Then Connection.Close()
        End Try

        ' Try-Catch.
        Try
            Socket.Send(Data)
        Catch ex As InvalidCastException
            WriteLine(ex)
        Catch ex As SocketException
            Throw New Exception(ex)
        Catch ex As Exception
            WriteLine(ex)
        End Try

        ' Try-Catch-Finally.
        Try
            Throw New Exception()
        Catch ex As Exception

        Finally
            WriteLine("Done!")
        End Try

        SyncLock resource

        End SyncLock

        Using resource

        End Using

        Using connection As New SqlConnection(ConnectionString)

        End Using

        Using resource = GetResource()

        End Using

        Using connection = CreateConnection(), 
              command = connection.CreateCommand(), 
              reader = command.ExecuteReader()

        End Using
    End Sub
End Class
</String>,
<String>
class C
{
    void M()
    {
        // Try-Catch-All.
        try
        {
            Connection.Open();
        }
        catch
        {
            throw;
        }

        // Try-Finally.
        try
        {
            Connection.Open();

            Connection.Close();
        }
        finally
        {
            if (Connection != null) { Connection.Close(); }
        }

        // Try-Catch.
        try
        {
            Socket.Send(Data);
        }
        catch (InvalidCastException ex)
        {
            WriteLine(ex);
        }
        catch (SocketException ex)
        {
            throw new Exception(ex);
        }
        catch (Exception ex)
        {
            WriteLine(ex);
        }

        // Try-Catch-Finally.
        try
        {
            throw new Exception();
        }
        catch (Exception ex)
        {
        }
        finally
        {
            WriteLine("Done!");
        }

        lock (resource)
        {
        }

        using (resource)
        {
        }

        using (var connection = new SqlConnection(ConnectionString))
        {
        }

        using (var resource = GetResource())
        {
        }

        using (var connection = CreateConnection())
        using (var command = connection.CreateCommand())
        using (var reader = command.ExecuteReader())
        {

        }
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertIf()

            AssertConversion(
<String>
Class C
    Sub M()
        If True Then Return
        If False Then Return : Return : Else Return
        If True

        ElseIf 1 > 2 Then
        
        ElseIf String.IsNullOrEmpty(String.Empty)
            Console.Beep()
        Else
            Return
        End If
    End Sub
End Class
</String>,
<String>
class C
{
    void M()
    {
        if (true)
        {
            return;
        }

        if (false)
        {
            return;
            return;
        }
        else
        {
            return;
        }

        if (true)
        {
        }
        else if (1 > 2)
        {
        }
        else if (string.IsNullOrEmpty(string.Empty))
        {
            Console.Beep();
        }
        else
        {
            return;
        }
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertSelectCase()

            AssertConversion(
<String>
Class C
    Sub M()
        Select Case kind
            Case SyntaxKind.FieldDeclaration, SyntaxKind.LocalDeclaration
                Return
            Case SyntaxKind.UsingBlock
                Visit(node)
            Case Else
                Throw New NotSupportedException()
        End Select
    End Sub
End Class
</String>,
<String>
class C
{
    void M()
    {
        switch (kind)
        {
            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.LocalDeclaration:
                return;
                break;
            case SyntaxKind.UsingBlock:
                Visit(node);
                break;
            default:
                throw new NotSupportedException();
        }
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertCasts()

            AssertConversion(
<String>
Class C
Friend Sub M(obj As Object)
        Dim casts = {CType(obj, Integer).ToString(), DirectCast(obj, String), TryCast(obj, C).M(obj)}

        Dim values = {CByte(obj), CUShort(obj), CUInt(obj), CULng(obj), 
                      CSByte(obj), CShort(obj), CInt(obj), CLng(obj), 
                      CBool(obj), CDate(obj), CObj(obj),
                      CChar(obj), CStr(obj), 
                      CSng(obj), CDbl(obj), CDec(obj)}
    End Sub
End Class
</String>,
<String>
class C
{
    internal void M(object obj)
    {
        var casts = new[] {((int)obj).ToString(), ((string)obj), (obj as C).M(obj)};

        var values = new[] {((byte)obj), ((ushort)obj), ((uint)obj), ((ulong)obj), 
                            ((sbyte)obj), ((short)obj), ((int)obj), ((long)obj), 
                            ((bool)obj), ((global::System.DateTime)obj), ((object)obj),
                            ((char)obj), ((string)obj), 
                            ((float)obj), ((double)obj), ((decimal)obj)};
    }
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertLoops()

            AssertConversion(
<String>
Class C
    Function M() As Integer
        While True
            Console.Beep()
        End While

        Do : Loop

        Do While enumerator.MoveNext()
        Loop

        Do Until stream.EndOfFile
        Loop

        Do
        Loop While Peek() IsNot Nothing

        Do
        Loop Until Peek() = -1

        For Each control In Controls
        Next

        For Each c As Control in Controls
        Next

        For i = 1 To 10
        Next

        For i = 0 To 100 Step 10
        Next

        For i As Integer = 0 To arr.Length - 1
        Next

        For i = arr.Length - 1 To 0 Step -1
        Next

        For i = 1 To Sheets.Count
        Next
    End Function
End Class
</String>,
<String>
class C
{
    int M()
    {
        while (true)
        {
            Console.Beep();
        }

        while (true)
        {
        }

        while (enumerator.MoveNext())
        {
        }

        while (!(stream.EndOfFile))
        {
        }

        do
        {
        }
        while (Peek() != null);

        do
        {
        }
        while (!(Peek() == -1));

        foreach (var control in Controls)
        {
        }

        foreach (Control c in Controls)
        {
        }

        for (var i = 1; i &lt;= 10; i++)
        {
        }

        for (var i = 0; i &lt;= 100; i += 10)
        {
        }

        for (int i = 0; i &lt; arr.Length; i++)
        {
        }

        for (var i = arr.Length - 1; i &gt;= 0; i--)
        {
        }

        for (var i = 1; i &lt;= Sheets.Count; i++)
        {
        }
    }
}
</String>
            )

        End Sub

        <Fact>
        Sub TestConvertLinq()

            AssertConversion(
<String>
Class C
    Sub M()

        Dim q = From item In Items

        Dim q = From item In Items Distinct

        Dim q = From item In Items Where item.IsSelected AndAlso True

        Dim q = From item In Items Where item.IsSelected AndAlso True Select item.ProductId, item.UnitPrice

        Dim q = From item In Items Order By item.UnitPrice

        Dim q = From item In Items Join product in Products On item.ProductId Equals product.Id

    End Sub
End Class
</String>,
<String>
class C
{
    void M()
    {
        var q = from item in Items select item;

        var q = (from item in Items select item).Distinct();

        var q = from item in Items where item.IsSelected &amp;&amp; true select item;

        var q = from item in Items where item.IsSelected &amp;&amp; true select new { item.ProductId, item.UnitPrice };

        var q = from item in Items orderby item.UnitPrice select item;

        var q = from item in Items join product in Products on item.ProductId equals product.Id select new { item, product };
    }
}
</String>
            )

        End Sub

        <Fact>
        Public Sub TestAsyncModifier()

            AssertConversion(
<String>
Async Sub M()
End Sub

Async Function N() As Task
End Function

Async Function O() As Task(Of Integer)
End Function
</String>,
<String>
async void M()
{
}

async Task N()
{
}

async Task&lt;int&gt; O()
{
}
</String>
            )

        End Sub

        <Fact>
        Public Sub TestAwaitExpression()

            AssertConversion(
<String>
Async Sub Button1_Click(sender As Object, e As EventArgs)
    ResultsTextBox.Text = Await httpClient.DownloadStringTaskAsync("http://somewhere.com/")
End Sub
</String>,
<String>
async void Button1_Click(object sender, EventArgs e)
{
    ResultsTextBox.Text = await httpClient.DownloadStringTaskAsync("http://somewhere.com/");
}
</String>
            )

        End Sub

        <Fact>
        Public Sub TestAwaitStatement()

            AssertConversion(
<String>
Async Sub Button1_Click(sender As Object, e As EventArgs)
    Await BeepAsync()
End Sub
</String>,
<String>
async void Button1_Click(object sender, EventArgs e)
{
    await BeepAsync();
}
</String>
            )

        End Sub

        <Fact>
        Public Sub TestAsyncLambdas()

            AssertConversion(
<String>
Sub M()
    Task.Run(Async Function()
             End Function)
    Task.Run(Async Function() Await NAsync())
    Task.Run(Async Sub() Await NAsync())
    Task.Run(Async Sub()
             End Sub)
End Sub
</String>,
<String>
void M()
{
    Task.Run(async () => 
    {
    }

    );
    Task.Run(async () => await NAsync());
    Task.Run(async () => 
    {
        await NAsync();
    }

    );
    Task.Run(async () => 
    {
    }

    );
}
</String>
            )

        End Sub

        <Fact(Skip:="Embedded resources not working with repotoolset")>
        Sub TestConvertUnsupportedDoesntThrow()
            Dim actual = Converter.ConvertTree(VB.SyntaxFactory.ParseSyntaxTree(My.Resources.VBAllInOne))
        End Sub

        Sub AssertConversion(ByVal source As String, ByVal expected As String)

            Dim tree = VB.SyntaxFactory.ParseSyntaxTree(source)

            Normalize(expected)

            Dim actual = Converter.ConvertTree(tree).ToFullString()

            Assert.Equal(expected, actual)

        End Sub

        Private Sub Normalize(ByRef value As String)
            value = CS.SyntaxFactory.ParseCompilationUnit(value).NormalizeWhitespace().ToFullString()
        End Sub
    End Class

End Namespace

