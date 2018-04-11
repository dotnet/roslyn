// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class NUnitApis
    {
        public const string CSharp = @"
using System;
using System.Threading.Tasks;

namespace NUnit.Framework
{
    public interface IResolveConstraint { }
    public delegate void TestDelegate();
    public delegate Task AsyncTestDelegate();

    public class Assert
    {
        public static Exception Throws(IResolveConstraint expression, TestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception Throws(IResolveConstraint expression, TestDelegate code)
        {
            return null;
        }

        public static Exception Throws(Type expectedExceptionType, TestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception Throws(Type expectedExceptionType, TestDelegate code)
        {
            return null;
        }

        public static TActual Throws<TActual>(TestDelegate code, string message, params object[] args) where TActual : Exception
        {
            return null;
        }

        public static TActual Throws<TActual>(TestDelegate code) where TActual : Exception
        {
            return null;
        }

        public static Exception Catch(TestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception Catch(TestDelegate code)
        {
            return null;
        }

        public static Exception Catch(Type expectedExceptionType, TestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception Catch(Type expectedExceptionType, TestDelegate code)
        {
            return null;
        }

        public static TActual Catch<TActual>(TestDelegate code, string message, params object[] args) where TActual : System.Exception
        {
            return null;
        }

        public static TActual Catch<TActual>(TestDelegate code) where TActual : System.Exception
        {
            return null;
        }

        public static void DoesNotThrow(TestDelegate code, string message, params object[] args)
        {
        }

        public static void DoesNotThrow(TestDelegate code)
        {
        }

        public static Exception ThrowsAsync(IResolveConstraint expression, AsyncTestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception ThrowsAsync(IResolveConstraint expression, AsyncTestDelegate code)
        {
            return null;
        }

        public static Exception ThrowsAsync(Type expectedExceptionType, AsyncTestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception ThrowsAsync(Type expectedExceptionType, AsyncTestDelegate code)
        {
            return null;
        }

        public static TActual ThrowsAsync<TActual>(AsyncTestDelegate code, string message, params object[] args) where TActual : Exception
        {
            return null;
        }

        public static TActual ThrowsAsync<TActual>(AsyncTestDelegate code) where TActual : Exception
        {
            return null;
        }

        public static Exception CatchAsync(AsyncTestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception CatchAsync(AsyncTestDelegate code)
        {
            return null;
        }

        public static Exception CatchAsync(Type expectedExceptionType, AsyncTestDelegate code, string message, params object[] args)
        {
            return null;
        }

        public static Exception CatchAsync(Type expectedExceptionType, AsyncTestDelegate code)
        {
            return null;
        }

        public static TActual CatchAsync<TActual>(AsyncTestDelegate code, string message, params object[] args) where TActual : Exception
        {
            return null;
        }

        public static TActual CatchAsync<TActual>(AsyncTestDelegate code) where TActual : Exception
        {
            return null;
        }

        public static void DoesNotThrowAsync(AsyncTestDelegate code, string message, params object[] args)
        {
        }

        public static void DoesNotThrowAsync(AsyncTestDelegate code)
        {
        }
    }

	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
	public class OneTimeSetUpAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
	public class OneTimeTearDownAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited=true)]
    public class SetUpAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
	public class TearDownAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public class TestAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited=false)]
    public class TestCaseAttribute : Attribute {
        public TestCaseAttribute(params object[] arguments)
        {
        }
        public TestCaseAttribute(object arg)
        {
        }
        public TestCaseAttribute(object arg1, object arg2)
        {
        }
        public TestCaseAttribute(object arg1, object arg2, object arg3)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class TestCaseSourceAttribute : Attribute {
        public TestCaseSourceAttribute(string sourceName)
        {
        }
        public TestCaseSourceAttribute(Type sourceType, string sourceName, object[] methodParams)
        {
        }
        public TestCaseSourceAttribute(Type sourceType, string sourceName)
        {
        }
        public TestCaseSourceAttribute(string sourceName, object[] methodParams)
        {
        }
        public TestCaseSourceAttribute(Type sourceType)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited=true)]
    public class TheoryAttribute : Attribute {}
}
";

        public const string VisualBasic = @"

Imports System
Imports System.Threading.Tasks

Namespace NUnit.Framework

    Public Interface IResolveConstraint
    End Interface

    Public Delegate Sub TestDelegate()

    Public Delegate Function AsyncTestDelegate() As Task

    Public Class Assert

        Public Shared Function Throws(ByVal expression As IResolveConstraint, ByVal code As TestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function Throws(ByVal expression As IResolveConstraint, ByVal code As TestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function Throws(ByVal expectedExceptionType As Type, ByVal code As TestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function Throws(ByVal expectedExceptionType As Type, ByVal code As TestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function Throws(Of TActual As Exception)(ByVal code As TestDelegate, ByVal message As String, ParamArray args As Object()) As TActual
            Return Nothing
        End Function

        Public Shared Function Throws(Of TActual As Exception)(ByVal code As TestDelegate) As TActual
            Return Nothing
        End Function

        Public Shared Function [Catch](ByVal code As TestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function [Catch](ByVal code As TestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function [Catch](ByVal expectedExceptionType As Type, ByVal code As TestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function [Catch](ByVal expectedExceptionType As Type, ByVal code As TestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function [Catch](Of TActual As System.Exception)(ByVal code As TestDelegate, ByVal message As String, ParamArray args As Object()) As TActual
            Return Nothing
        End Function

        Public Shared Function [Catch](Of TActual As System.Exception)(ByVal code As TestDelegate) As TActual
            Return Nothing
        End Function

        Public Shared Sub DoesNotThrow(ByVal code As TestDelegate, ByVal message As String, ParamArray args As Object())
        End Sub

        Public Shared Sub DoesNotThrow(ByVal code As TestDelegate)
        End Sub

        Public Shared Function ThrowsAsync(ByVal expression As IResolveConstraint, ByVal code As AsyncTestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function ThrowsAsync(ByVal expression As IResolveConstraint, ByVal code As AsyncTestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function ThrowsAsync(ByVal expectedExceptionType As Type, ByVal code As AsyncTestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function ThrowsAsync(ByVal expectedExceptionType As Type, ByVal code As AsyncTestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function ThrowsAsync(Of TActual As Exception)(ByVal code As AsyncTestDelegate, ByVal message As String, ParamArray args As Object()) As TActual
            Return Nothing
        End Function

        Public Shared Function ThrowsAsync(Of TActual As Exception)(ByVal code As AsyncTestDelegate) As TActual
            Return Nothing
        End Function

        Public Shared Function CatchAsync(ByVal code As AsyncTestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function CatchAsync(ByVal code As AsyncTestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function CatchAsync(ByVal expectedExceptionType As Type, ByVal code As AsyncTestDelegate, ByVal message As String, ParamArray args As Object()) As Exception
            Return Nothing
        End Function

        Public Shared Function CatchAsync(ByVal expectedExceptionType As Type, ByVal code As AsyncTestDelegate) As Exception
            Return Nothing
        End Function

        Public Shared Function CatchAsync(Of TActual As Exception)(ByVal code As AsyncTestDelegate, ByVal message As String, ParamArray args As Object()) As TActual
            Return Nothing
        End Function

        Public Shared Function CatchAsync(Of TActual As Exception)(ByVal code As AsyncTestDelegate) As TActual
            Return Nothing
        End Function

        Public Shared Sub DoesNotThrowAsync(ByVal code As AsyncTestDelegate, ByVal message As String, ParamArray args As Object())
        End Sub

        Public Shared Sub DoesNotThrowAsync(ByVal code As AsyncTestDelegate)
        End Sub
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)>
    Public Class OneTimeSetUpAttribute
        Inherits Attribute
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)>
    Public Class OneTimeTearDownAttribute
        Inherits Attribute
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)>
    Public Class SetUpAttribute
        Inherits Attribute
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)>
    Public Class TearDownAttribute
        Inherits Attribute
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)>
    Public Class TestAttribute
        Inherits Attribute
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=True, Inherited:=False)>
    Public Class TestCaseAttribute
        Inherits Attribute

        Public Sub New(ParamArray arguments As Object())
        End Sub

        Public Sub New(ByVal arg As Object)
        End Sub

        Public Sub New(ByVal arg1 As Object, ByVal arg2 As Object)
        End Sub

        Public Sub New(ByVal arg1 As Object, ByVal arg2 As Object, ByVal arg3 As Object)
        End Sub
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=True, Inherited:=False)>
    Public Class TestCaseSourceAttribute
        Inherits Attribute

        Public Sub New(ByVal sourceName As String)
        End Sub

        Public Sub New(ByVal sourceType As Type, ByVal sourceName As String, ByVal methodParams As Object())
        End Sub

        Public Sub New(ByVal sourceType As Type, ByVal sourceName As String)
        End Sub

        Public Sub New(ByVal sourceName As String, ByVal methodParams As Object())
        End Sub

        Public Sub New(ByVal sourceType As Type)
        End Sub
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)>
    Public Class TheoryAttribute
        Inherits Attribute
    End Class
End Namespace
";
    }
}
