// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack;
using Microsoft.AnalyzerPowerPack.CSharp.Design;
using Microsoft.AnalyzerPowerPack.VisualBasic.Design;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    public partial class CA1024Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicCA1024DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpCA1024DiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1024NoDiagnosticCases()
        {
            VerifyCSharp(@"
using System;
using System.Collections;

public class GenericType<T>
{
}

public class Base
{
    public virtual int GetSomething()
    {
        return 0;
    }

    public virtual int GetOverloadedMethod()
    {
        return 1;
    }

    public virtual int GetOverloadedMethod(int i)
    {
        return i;
    }
}

public class Class1 : Base
{
    private string fileName = """";

    // 1) Returns void
    public void GetWronglyNamedMethod()
    {
    }

    // 2) Not a method
    public string LogFile
    {
        get { return fileName; }
    }

    // 3) Returns an array type
    public int[] GetValues()
    {
        return null;
    }

    // 4) Has parameters
    public int[] GetMethodWithParameters(int p)
    {
        return new int[] { p };
    }

    // 5a) Name doesn't start with a 'Get'
    public int SomeMethod()
    {
        return 0;
    }

    // 5b) First compound word is not 'Get'
    public int GetterMethod()
    {
        return 0;
    }

    // 6) Generic method
    public object GetGenericMethod<T>()
    {
        return new GenericType<T>();
    }

    // 7) Override
    public override int GetSomething()
    {
        return 1;
    }

    // 8) Method with overloads
    public override int GetOverloadedMethod()
    {
        return 1;
    }

    public override int GetOverloadedMethod(int i)
    {
        return i;
    }

    // 9) Methods with special name
    public override int GetHashCode()
    {
        return 0;
    }

    public IEnumerator GetEnumerator()
    {
        return null;
    }

    // 10) Method with invocation expressions
    public int GetSomethingWithInvocation()
    {
        Console.WriteLine(this);
        return 0;
    }

    // 11) Method named 'Get'
    public string Get()
    {
        return fileName;
    }

    // 12) Private method
    private string GetSomethingPrivate()
    {
        return fileName;
    }

    // 13) Internal method
    internal string GetSomethingInternal()
    {
        return fileName;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1024DiagnosticCases()
        {
            VerifyCSharp(@"
public class Class
{
    private string fileName = ""data.txt"";

    public string GetFileName()
    {
        return fileName;
    }

    public string Get_FileName2()
    {
        return fileName;
    }

    public string Get123()
    {
        return fileName;
    }

    protected string GetFileNameProtected()
    {
        return fileName;
    }
}
",
            GetCA1024CSharpResultAt(6, 19, "GetFileName"),
            GetCA1024CSharpResultAt(11, 19, "Get_FileName2"),
            GetCA1024CSharpResultAt(16, 19, "Get123"),
            GetCA1024CSharpResultAt(21, 22, "GetFileNameProtected"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1024DiagnosticCasesWithScope()
        {
            VerifyCSharp(@"
public class Class
{
    private string fileName = """";

    public string GetFileName()
    {
        return fileName;
    }

    [|public string Get_FileName2()
    {
        return fileName;
    }

    public string Get123()
    {
        return fileName;
    }|]

    protected string GetFileNameProtected()
    {
        return fileName;
    }
}
",
            GetCA1024CSharpResultAt(11, 19, "Get_FileName2"),
            GetCA1024CSharpResultAt(16, 19, "Get123"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1024NoDiagnosticCases()
        {
            VerifyBasic(@"
Public Class Base
	Public Overridable Function GetSomething() As Integer
		Return 0
	End Function
End Class

Public Class Class1
	Inherits Base
	Private fileName As String

	' 1) Returns void
	Public Sub GetWronglyNamedMethod()
	End Sub

	' 2) Not a method
	Public ReadOnly Property LogFile() As String
		Get
			Return fileName
		End Get
	End Property

	' 3) Returns an array type
	Public Function GetValues() As Integer()
		Return Nothing
	End Function

	' 4) Has parameters
	Public Function GetMethodWithParameters(p As Integer) As Integer()
		Return New Integer() {p}
	End Function

	' 5a) Name doesn't start with a 'Get'
	Public Function SomeMethod() As Integer
		Return 0
	End Function

	' 5b) First compound word is not 'Get'
	Public Function GetterMethod() As Integer
		Return 0
	End Function

	' 6) Generic method
	Public Function GetGenericMethod(Of T)() As Object
		Return New GenericType(Of T)()
	End Function

	' 7) Override
	Public Overrides Function GetSomething() As Integer
		Return 1
	End Function

	' 8) Method with overloads
	Public Overrides Function GetOverloadedMethod() As Integer
		Return 1
	End Function

	Public Overrides Function GetOverloadedMethod(i As Integer) As Integer
		Return i
	End Function

	' 9) Methods with special name
	Public Overloads Function GetHashCode() As Integer
		Return 0
	End Function

	Public Function GetEnumerator() As IEnumerator
		Return Nothing
	End Function

	' 10) Method with invocation expressions
	Public Function GetSomethingWithInvocation() As Integer
		Console.WriteLine(Me)
		Return 0
	End Function

	' 11) Method named 'Get'
	Public Function Get() As String
		Return fileName
	End Function

    ' 12) Private method
	Private Function GetSomethingPrivate() As String
		Return fileName
	End Function

    ' 13) Friend method
	Friend Function GetSomethingInternal() As String
		Return fileName
	End Function
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1024DiagnosticCases()
        {
            VerifyBasic(@"
Public Class Class1
	Private fileName As String

	Public Function GetFileName() As String
		Return filename
	End Function

	Public Function Get_FileName2() As String
		Return filename
	End Function

	Public Function Get123() As String
		Return filename
	End Function

    Protected Function GetFileNameProtected() As String
		Return filename
	End Function
End Class
",
            GetCA1024BasicResultAt(5, 18, "GetFileName"),
            GetCA1024BasicResultAt(9, 18, "Get_FileName2"),
            GetCA1024BasicResultAt(13, 18, "Get123"),
            GetCA1024BasicResultAt(17, 24, "GetFileNameProtected"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1024DiagnosticCaseswithScope()
        {
            VerifyBasic(@"
Public Class Class1
	Private fileName As String

	Public Function GetFileName() As String
		Return filename
	End Function

	[|Public Function Get_FileName2() As String
		Return filename
	End Function

	Public Function Get123() As String
		Return filename
	End Function|]

    Protected Function GetFileNameProtected() As String
		Return filename
	End Function
End Class
",
            GetCA1024BasicResultAt(9, 18, "Get_FileName2"),
            GetCA1024BasicResultAt(13, 18, "Get123"));
        }

        internal static string CA1024Name = "CA1024";

        private static DiagnosticResult GetCA1024CSharpResultAt(int line, int column, string methodName)
        {
            return GetCSharpResultAt(line, column, CA1024Name, string.Format(AnalyzerPowerPackRulesResources.ChangeToAPropertyIfAppropriate, methodName));
        }

        private static DiagnosticResult GetCA1024BasicResultAt(int line, int column, string methodName)
        {
            return GetBasicResultAt(line, column, CA1024Name, string.Format(AnalyzerPowerPackRulesResources.ChangeToAPropertyIfAppropriate, methodName));
        }
    }
}
