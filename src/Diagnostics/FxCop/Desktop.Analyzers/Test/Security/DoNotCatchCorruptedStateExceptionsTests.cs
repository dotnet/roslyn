// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;                 

namespace Desktop.Analyzers.UnitTests
{
    public partial class DoNotCatchCorruptedStateExceptionsTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicDoNotCatchCorruptedStateExceptionsAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpDoNotCatchCorruptedStateExceptionsAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithSecurityCriticalAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;

            namespace TestNamespace
            {
                class TestClass
                {
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }");

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security

            Namespace TestNamespace
                Class TestClass
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                        End Try
                    End Sub
                End Class
            End Namespace
            ");

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security

            Namespace TestNamespace
                Class TestClass
                    <SecurityCritical> _
                    Public Shared Function TestMethod() as Boolean
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                        End Try
                        Return True
                    End Sub
                End Class
            End Namespace
            ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithHpcseAttribute()
        {
            // Note this is a change from FxCop's previous behavior since we no longer consider SystemCritical.

            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(17, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                        End Try
                    End Sub
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(11, 25, "Public Shared Sub TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    Public Shared Function TestMethod() As Double
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                        End Try
                        Return 0
                    End Function
                End Class
            End Namespace
            ",
           GetCA2153BasicResultAt(11, 25, "Public Shared Function TestMethod() As Double", "System.Exception")
           );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchRethrowExceptionInMethodWithHpcseAndSecurityCriticalAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {
                            throw;
                        }
                    }
                }
            }");

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                            Throw
                        End Try
                    End Sub
                End Class
            End Namespace
            ");

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Function TestMethod() As Double
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                            Throw
                        End Try
                        Return 0
                    End Sub
                End Class
            End Namespace
            ");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithHpcseAndSecurityCriticalAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(19, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                        End Try
                    End Sub
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Sub TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    < SecurityCritical > _
                    Public Shared Function TestMethod() As Double
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                        End Try
                        Return 0
                    End Function
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Function TestMethod() As Double", "System.Exception")
            );

            VerifyBasic(@"
            Imports System
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    < SecurityCritical > _
                    Public Shared Function TestMethod() As Double
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As Exception
                        End Try
                        Return 0
                    End Function
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(14, 25, "Public Shared Function TestMethod() As Double", "System.Exception")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchInMethodWithHpcseAndSecurityCriticalAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch 
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(19, 25, "TestNamespace.TestClass.TestMethod()", "object")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch 
                        End Try
                    End Sub
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Sub TestMethod()", "Object")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    < SecurityCritical > _
                    Public Shared Function TestMethod() As Double
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch 
                        End Try
                        Return 0
                    End Function
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Function TestMethod() As Double", "Object")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchsystemExceptionInMethodWithHpcseAndSecurityCriticalAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (SystemException e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(19, 25, "TestNamespace.TestClass.TestMethod()", "System.SystemException")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e as System.SystemException
                        End Try
                    End Sub
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Sub TestMethod()", "System.SystemException")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    < SecurityCritical > _
                    Public Shared Function TestMethod() As Double
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e as System.SystemException
                        End Try
                        Return 0
                    End Function
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Function TestMethod() As Double", "System.SystemException")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithHpcseAndSecurityCriticalClassScopeEverythingAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                [SecurityCritical(SecurityCriticalScope.Everything)]
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(19, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                <SecurityCritical(SecurityCriticalScope.Everything)> _
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                        End Try
                    End Sub
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Sub TestMethod()", "System.Exception")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithHpcseAndSecurityCriticalClassAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                [SecurityCritical]
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(19, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithHpcseAndSecurityCriticalClassScopeExcplicitAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                [SecurityCritical(SecurityCriticalScope.Explicit)]
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(19, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithHpcseAndSecurityCriticalL1Attributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            [assembly:SecurityCritical(SecurityCriticalScope.Everything)]
            [assembly:SecurityRules(SecurityRuleSet.Level1)]
            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(20, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInMethodWithHpcseAndSecurityCriticalL2Attributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            [assembly:SecurityCritical(SecurityCriticalScope.Everything)]
            [assembly:SecurityRules(SecurityRuleSet.Level2)]
            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream = new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(20, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInNestedClassMethodWithOuterHpcseAndSecurityCriticalScopeEverythingAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                [SecurityCritical(SecurityCriticalScope.Everything)]
                class TestClass
                {
                    class NestedClass
                    {
                        [HandleProcessCorruptedStateExceptions] 
                        public static void TestMethod()
                        {
                            try 
                            {
                                FileStream fileStream = new FileStream(""name"", FileMode.Create);
                            }
                            catch (Exception e)
                            {}
                        }
                    }
                }
            }",
            GetCA2153CSharpResultAt(21, 29, "TestNamespace.TestClass.NestedClass.TestMethod()", "System.Exception")
            );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInNestedClassMethodWithInnerHpcseAndSecurityCriticalScopeEverythingAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [SecurityCritical(SecurityCriticalScope.Everything)]
                    class NestedClass
                    {
                        [HandleProcessCorruptedStateExceptions] 
                        public static void TestMethod()
                        {
                            try 
                            {
                                FileStream fileStream = new FileStream(""name"", FileMode.Create);
                            }
                            catch (Exception e)
                            {}
                        }
                    }
                }
            }",
            GetCA2153CSharpResultAt(21, 29, "TestNamespace.TestClass.NestedClass.TestMethod()", "System.Exception")
            );
        }

  
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInNestedClassMethodwithInnerHpcseAndOuterSecurityCriticalAttributes()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [SecurityCritical(SecurityCriticalScope.Everything)]
                    class NestedClass
                    {
                        [HandleProcessCorruptedStateExceptions] 
                        public static void TestMethod()
                        {
                            try 
                            {
                                FileStream fileStream = new FileStream(""name"", FileMode.Create);
                            }
                            catch (Exception e)
                            {}
                        }
                    }
                }
            }",
            GetCA2153CSharpResultAt(21, 29, "TestNamespace.TestClass.NestedClass.TestMethod()", "System.Exception")
            );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInGetAccessorWithHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {      
                    public string SaveNewFile3
                    {
                        [HandleProcessCorruptedStateExceptions]
                        get
                        {
                            try
                            {
                                AccessViolation();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(""CATCH"");
                            }
                            return ""asdf"";
                        }
                    }
                    private static void AccessViolation(){}
                }
            }",
            GetCA2153CSharpResultAt(20, 29, "TestNamespace.TestClass.SaveNewFile3.get", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    private x As Integer
                    Public Property X() As Integer
                        <HandleProcessCorruptedStateExceptions> _
                        <SecurityCritical> _
                        Get
                            Try
                                Dim fileStream As New FileStream(""name"", FileMode.Create)
                            Catch e As System.Exception
                            End Try
                            Return x
                        End Get
                    End Property
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(14, 29, "Public Property Get X() As Integer", "System.Exception")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchInGetAccessorWithHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {      
                    public string SaveNewFile3
                    {
                        [HandleProcessCorruptedStateExceptions]
                        get
                        {
                            try
                            {
                                AccessViolation();
                            }
                            catch 
                            {
                                Console.WriteLine(""CATCH"");
                            }
                            return ""asdf"";
                        }
                    }
                    private static void AccessViolation(){}
                }
            }",
            GetCA2153CSharpResultAt(20, 29, "TestNamespace.TestClass.SaveNewFile3.get", "object")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchSystemExceptionInGetAccessorWithHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {      
                    public string SaveNewFile3
                    {
                        [HandleProcessCorruptedStateExceptions]
                        get
                        {
                            try
                            {
                                AccessViolation();
                            }
                            catch (SystemException ex)
                            {
                                Console.WriteLine(""CATCH"");
                            }
                            return ""asdf"";
                        }
                    }
                    private static void AccessViolation(){}
                }
            }",
            GetCA2153CSharpResultAt(20, 29, "TestNamespace.TestClass.SaveNewFile3.get", "System.SystemException")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchInSetAccessorWithHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {      
                    public string SaveNewFile3
                    {
                        [HandleProcessCorruptedStateExceptions]
                        set
                        {
                            try
                            {
                                AccessViolation();
                            }
                            catch 
                            {
                                Console.WriteLine(""CATCH"");
                            }
                            return ""asdf"";
                        }
                    }
                    private static void AccessViolation(){}
            }",
            GetCA2153CSharpResultAt(20, 29, "TestNamespace.TestClass.SaveNewFile3.set", "object")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInSetAccessorWithHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {      
                    private string file;
                    public string SaveNewFile3
                    {
                        [HandleProcessCorruptedStateExceptions]
                        set
                        {
                            try
                            {
                                AccessViolation();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(""CATCH"");
                            }
                            file = value;
                        }
                    } 
                    private static void AccessViolation(){}
                }
            }",
            GetCA2153CSharpResultAt(21, 29, "TestNamespace.TestClass.SaveNewFile3.set", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    private x As Integer
                    Public Property X() As Integer
                        <HandleProcessCorruptedStateExceptions> _
                        <SecurityCritical> _
                        Set
                            Try
                                Dim fileStream As New FileStream(""name"", FileMode.Create)
                            Catch e As System.Exception
                            End Try
                            Return x
                        End Get
                    End Property
                End Class
            End Namespace
            ",
           GetCA2153BasicResultAt(14, 29, "Public Property Set X(Value As Integer)", "System.Exception")
           );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchIOExceptionInMethodHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions]
                    public static void TestMethod()
                    { 
                        try
                        {
                            FileStream fs = new FileStream(""fileName"", FileMode.Create);
                        }
                        catch (IOException ex)
                        {
                            throw ex;
                        }
                        catch
                        {
                            throw;
                        }
                        finally { }
                    }
                }"
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchIOExceptionSwallowOtherExceptionInMethodHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions]
                    public static void TestMethod()
                    { 
                        try
                        {
                            FileStream fs = new FileStream(""fileName"", FileMode.Create);
                        }
                        catch (IOException ex)
                        {
                            throw ex;
                        }
                        catch (IOException ex)
                        {
                            throw ex;
                        }
                        catch {}
                        finally { }
                        }
                    }
                }
            }",
            GetCA2153CSharpResultAt(26, 25, "TestNamespace.TestClass.TestMethod()", "object")
            );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestSwallowAccessViolationExceptionInMethodHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {   
                    [HandleProcessCorruptedStateExceptions]
                    public static void SaveNewFile7(string fileName)
                    {
                        try
                        {
                            unsafe
                            {
                                byte b = *(byte*)(8762765876); // some code that causes access violation
                            }
                        }
                        catch (AccessViolationException ex)
                        {
                            // the AV is ignored here
                        }
                        finally {}
                    }
                }
            }");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestSwallowAccessViolationExceptionThenSwallowOtherExceptionInMethodHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {   
                    [HandleProcessCorruptedStateExceptions]
                    public static void SaveNewFile7(string fileName)
                    {
                        try
                        {
                            unsafe
                            {
                                byte b = *(byte*)(8762765876); // some code that causes access violation
                            }
                        }
                        catch (AccessViolationException ex)
                        {
                            // the AV is ignored here
                        }
                        catch {}
                        finally {}
                    }
                }
            }",
            GetCA2153CSharpResultAt(25, 25, "TestNamespace.TestClass.SaveNewFile7(string)", "object")
            );
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionThrowNotImplementedExceptionInMethodHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        try 
                        {
                            FileStream fileStream= new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception e)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }",
            GetCA2153CSharpResultAt(19, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                            Throw New NotImplementedException()
                        End Try
                    End Sub
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Sub TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Function TestMethod() As Double
                        Try
                            Dim fileStream As New FileStream(""name"", FileMode.Create)
                        Catch e As System.Exception
                            Throw New NotImplementedException()
                        End Try
                    Return 0
                    End Function
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(13, 25, "Public Shared Function TestMethod() As Double", "System.Exception")
            );
        }



        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchExceptionInnerCatchThrowIOExceptionInMethodHpcseAttribute()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [HandleProcessCorruptedStateExceptions] 
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        FileStream fileStream= null;
                        try
                        {
                            fileStream= new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                FileStream  anotherFileStream = new FileStream(""newName"", FileMode.Create);
                            }
                            catch (IOException)
                            {
                                throw;
                            }
                        }
                    }
                }
            }",
            GetCA2153CSharpResultAt(20, 25, "TestNamespace.TestClass.TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Dim fileStream As FileStream = Nothing
                        Try
                            fileStream= New FileStream(""name"", FileMode.Create)
                        Catch outterException As System.Exception
                            Try
                                Dim anotherFileStream = New FileStream(""newName"", FileMode.Create)
                            Catch innerException As IOException
                                Throw
                            End Try
                        End Try
                    End Sub
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(14, 25, "Public Shared Sub TestMethod()", "System.Exception")
            );

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <HandleProcessCorruptedStateExceptions> _
                    <SecurityCritical> _
                    Public Shared Function TestMethod() As Double
                        Dim fileStream As FileStream = Nothing
                        Try
                            fileStream= New FileStream(""name"", FileMode.Create)
                        Catch outterException As System.Exception
                            Try
                                Dim anotherFileStream = New FileStream(""newName"", FileMode.Create)
                            Catch innerException As IOException
                                Throw
                            End Try
                        End Try
                        Return 0
                    End Function
                End Class
            End Namespace
            ",
            GetCA2153BasicResultAt(14, 25, "Public Shared Function TestMethod() As Double", "System.Exception")
            );
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2153TestCatchGeneralException()
        {
            VerifyCSharp(@"
            using System;
            using System.IO;
            using System.Security;
            using System.Runtime.ExceptionServices;

            namespace TestNamespace
            {
                class TestClass
                {
                    [SecurityCritical]
                    public static void TestMethod()
                    {
                        FileStream fileStream= null;
                        try
                        {
                            fileStream= new FileStream(""name"", FileMode.Create);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }");

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <SecurityCritical> _
                    Public Shared Sub TestMethod()
                        Dim fileStream As FileStream = Nothing
                        Try
                            fileStream= New FileStream(""name"", FileMode.Create)
                        Catch outterException As System.Exception
                        End Try
                    End Sub
                End Class
            End Namespace
            ");

            VerifyBasic(@"
            Imports System.IO
            Imports System.Security
            Imports System.Runtime.ExceptionServices

            Namespace TestNamespace
                Class TestClass
                    <SecurityCritical> _
                    Public Shared Function TestMethod() As Double
                        Dim fileStream As FileStream = Nothing
                        Try
                            fileStream= New FileStream(""name"", FileMode.Create)
                        Catch outterException As System.Exception
                        End Try
                        Return 0
                    End Function
                End Class
            End Namespace
            ");
        }

        private const string CA2153RuleName = "CA2153";

        private DiagnosticResult GetCA2153CSharpResultAt(int line, int column, string signature, string typeName)
        {
            return GetCSharpResultAt(line, column, CA2153RuleName, string.Format(DesktopAnalyzersResources.DoNotCatchCorruptedStateExceptionsMessage, signature, typeName));
        }

        private DiagnosticResult GetCA2153BasicResultAt(int line, int column, string signature, string typeName)
        {
            return GetBasicResultAt(line, column, CA2153RuleName, string.Format(DesktopAnalyzersResources.DoNotCatchCorruptedStateExceptionsMessage, signature, typeName));
        }
    }
}
