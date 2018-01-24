// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class MSTestExpectedExceptionAttribute
    {
        public const string CSharp = @"
using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ExpectedExceptionAttribute : Attribute
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
End Namespace
";
    }
}
