// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class MSTestAttributes
    {
        public const string CSharp = @"
using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ExpectedExceptionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class TestMethodAttribute : Attribute
    {
        public TestMethodAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class TestInitializeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class TestCleanupAttribute : Attribute
    {
    }
}
";

        public const string VisualBasic = @"
Imports System

Namespace Microsoft.VisualStudio.TestTools.UnitTesting
    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False, Inherited:=True)>
    Public NotInheritable Class ExpectedExceptionAttribute
        Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple := False)>
    Public NotInheritable Class TestMethodAttribute
	    Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple := False)>
    Public NotInheritable Class TestInitializeAttribute
	    Inherits Attribute
    End Class

    <AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple := False)>
    Public NotInheritable Class TestCleanupAttribute
	    Inherits Attribute
    End Class
End Namespace
";
    }
}
