// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class XunitApis
    {
        public const string CSharp = @"
using System;
using System.Threading.Tasks;

namespace Xunit
{
    public class Assert
    {
        public static T Throws<T>(Action testCode)
            where T : Exception
        {
            return null;
        }

        public static T Throws<T>(Func<object> testCode)
            where T : Exception
        {
            return null;
        }

        public static T Throws<T>(Func<Task> testCode) where T : Exception { throw new NotImplementedException(); }

        public static Task<T> ThrowsAsync<T>(Func<Task> testCode)
            where T : Exception
        {
            return null;
        }

        public static T ThrowsAny<T>(Action testCode)
            where T : Exception
        {
            return null;
        }

        public static T ThrowsAny<T>(Func<object> testCode)
            where T : Exception
        {
            return null;
        }

        public static Task<T> ThrowsAnyAsync<T>(Func<Task> testCode)
            where T : Exception
        {
            return null;
        }

        public static Exception Throws(Type exceptionType, Action testCode)
        {
            return null;
        }

        public static Exception Throws(Type exceptionType, Func<object> testCode)
        {
            return null;
        }

        public static Task<Exception> ThrowsAsync(Type exceptionType, Func<Task> testCode)
        {
            return null;
        }

        static Exception Throws(Type exceptionType, Exception exception)
        {
            return null;
        }

        static Exception ThrowsAny(Type exceptionType, Exception exception)
        {
            return null;
        }

        public static T Throws<T>(string paramName, Action testCode)
            where T : ArgumentException
        {
            return null;
        }

        public static T Throws<T>(string paramName, Func<object> testCode)
            where T : ArgumentException
        {
            return null;
        }

        public static T Throws<T>(string paramName, Func<Task> testCode) where T : ArgumentException { throw new NotImplementedException(); }

        public static Task<T> ThrowsAsync<T>(string paramName, Func<Task> testCode)
            where T : ArgumentException
        {
            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TheoryAttribute : FactAttribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactAttribute : Attribute
    {
        public virtual string DisplayName { get; set; }
        public virtual string Skip { get; set; }
    }
}

namespace CustomxUnit 
{
    using Xunit;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class WpfFactAttribute : FactAttribute
    {
    }
}";

        public const string VisualBasic = @"
Imports System
Imports System.Threading.Tasks

Namespace Xunit

    Public Class Assert

        Public Shared Function Throws(Of T As Exception)(ByVal testCode As Action) As T
            Return Nothing
        End Function

        Public Shared Function Throws(Of T As Exception)(ByVal testCode As Func(Of Object)) As T
            Return Nothing
        End Function

        Public Shared Function Throws(Of T As Exception)(ByVal testCode As Func(Of Task)) As T
            Throw New NotImplementedException()
        End Function

        Public Shared Function ThrowsAsync(Of T As Exception)(ByVal testCode As Func(Of Task)) As Task(Of T)
            Return Nothing
        End Function

        Public Shared Function ThrowsAny(Of T As Exception)(ByVal testCode As Action) As T
            Return Nothing
        End Function

        Public Shared Function ThrowsAny(Of T As Exception)(ByVal testCode As Func(Of Object)) As T
            Return Nothing
        End Function

        Public Shared Function ThrowsAnyAsync(Of T As Exception)(ByVal testCode As Func(Of Task)) As Task(Of T)
            Return Nothing
        End Function

        Public Shared Function Throws(ByVal exceptionType As Type, ByVal testCode As Action) As Exception
            Return Nothing
        End Function

        Public Shared Function Throws(ByVal exceptionType As Type, ByVal testCode As Func(Of Object)) As Exception
            Return Nothing
        End Function

        Public Shared Function ThrowsAsync(ByVal exceptionType As Type, ByVal testCode As Func(Of Task)) As Task(Of Exception)
            Return Nothing
        End Function

        Private Shared Function Throws(ByVal exceptionType As Type, ByVal exception As Exception) As Exception
            Return Nothing
        End Function

        Private Shared Function ThrowsAny(ByVal exceptionType As Type, ByVal exception As Exception) As Exception
            Return Nothing
        End Function

        Public Shared Function Throws(Of T As ArgumentException)(ByVal paramName As String, ByVal testCode As Action) As T
            Return Nothing
        End Function

        Public Shared Function Throws(Of T As ArgumentException)(ByVal paramName As String, ByVal testCode As Func(Of Object)) As T
            Return Nothing
        End Function

        Public Shared Function Throws(Of T As ArgumentException)(ByVal paramName As String, ByVal testCode As Func(Of Task)) As T
            Throw New NotImplementedException()
        End Function

        Public Shared Function ThrowsAsync(Of T As ArgumentException)(ByVal paramName As String, ByVal testCode As Func(Of Task)) As Task(Of T)
            Return Nothing
        End Function
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False)>
    Public Class TheoryAttribute
        Inherits FactAttribute
    End Class

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False)>
    Public Class FactAttribute
        Inherits Attribute
        Public Overridable Property DisplayName As String
        Public Overridable Property Skip As String
    End Class
End Namespace

Namespace CustomxUnit

    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False)>
    Public Class WpfFactAttribute
        Inherits Xunit.FactAttribute
    End Class
End Namespace
";
    }
}
