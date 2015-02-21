// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public partial class TypesThatOwnDisposableFieldsShouldBeDisposableAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new TypesThatOwnDisposableFieldsShouldBeDisposableAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TypesThatOwnDisposableFieldsShouldBeDisposableAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001CSharpTestWithNoDisposableType()
        {
            VerifyCSharp(@"
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001CSharpTestWithNoDisposeMethod()
        {
            VerifyCSharp(@"
using System.IO;

    // This class violates the rule.
    public class NoDisposeClass
    {
        FileStream newFile;

        public NoDisposeClass()
        {
            newFile = new FileStream(""data.txt"", FileMode.Append);
        }
    }
",
            GetCA1001CSharpResultAt(5, 18, "NoDisposeClass"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001CSharpTestWithNoDisposeMethodInScope()
        {
            VerifyCSharp(@"
using System.IO;

    // This class violates the rule.
    [|public class NoDisposeClass
    {
        FileStream newFile;

        public NoDisposeClass()
        {
            newFile = new FileStream(""data.txt"", FileMode.Append);
        }
    }|]
",
            GetCA1001CSharpResultAt(5, 18, "NoDisposeClass"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001CSharpScopedTestWithNoDisposeMethodOutOfScope()
        {
            VerifyCSharp(@"
using System;
using System.IO;

// This class violates the rule.
public class NoDisposeClass
{
    FileStream newFile;

    public NoDisposeClass()
    {
        newFile = new FileStream(""data.txt"", FileMode.Append);
    }
}
   
[|public class Foo
{
}
|]
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001CSharpTestWithADisposeMethod()
        {
            VerifyCSharp(@"
using System;
using System.IO;

// This class satisfies the rule.
public class HasDisposeMethod : IDisposable
{
    FileStream newFile;

    public HasDisposeMethod()
    {
        newFile = new FileStream(""data.txt"", FileMode.Append);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // dispose managed resources
            newFile.Close();
        }
        // free native resources
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001BasicTestWithNoDisposableType()
        {
            VerifyBasic(@"
Module Module1

    Sub Main()

    End Sub

End Module
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001BasicTestWithNoDisposeMethod()
        {
            VerifyBasic(@"
   Imports System
   Imports System.IO

   ' This class violates the rule. 
   Public Class NoDisposeMethod

      Dim newFile As FileStream

      Sub New()
         newFile = New FileStream()
      End Sub

   End Class
",
            GetCA1001BasicResultAt(6, 17, "NoDisposeMethod"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001BasicTestWithNoDisposeMethodInScope()
        {
            VerifyBasic(@"
   Imports System.IO

   ' This class violates the rule. 
   [|Public Class NoDisposeMethod

      Dim newFile As FileStream

      Sub New()
         newFile = New FileStream("""", FileMode.Append)
      End Sub

   End Class|]
",
            GetCA1001BasicResultAt(5, 17, "NoDisposeMethod"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001BasicTestWithNoDisposeMethodOutOfScope()
        {
            VerifyBasic(@"
   Imports System.IO

   ' This class violates the rule. 
   Public Class NoDisposeMethod

      Dim newFile As FileStream

      Sub New()
         newFile = New FileStream()
      End Sub

   End Class

   [|
   Public Class Foo
   End Class
   |]
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1001BasicTestWithADisposeMethod()
        {
            VerifyBasic(@"
   Imports System
   Imports System.IO

   ' This class satisfies the rule. 
   Public Class HasDisposeMethod 
      Implements IDisposable

      Dim newFile As FileStream

      Sub New()
         newFile = New FileStream()
      End Sub

      Overloads Protected Overridable Sub Dispose(disposing As Boolean)

         If disposing Then
            ' dispose managed resources
            newFile.Close()
         End If

         ' free native resources 

      End Sub 'Dispose


      Overloads Public Sub Dispose() Implements IDisposable.Dispose

         Dispose(True)
         GC.SuppressFinalize(Me)

      End Sub 'Dispose

   End Class
");
        }

        internal static string CA1001Name = "CA1001";
        internal static string CA1001Message = "Type '{0}' owns disposable fields but is not disposable";

        private static DiagnosticResult GetCA1001CSharpResultAt(int line, int column, string objectName)
        {
            return GetCSharpResultAt(line, column, CA1001Name, string.Format(CA1001Message, objectName));
        }

        private static DiagnosticResult GetCA1001BasicResultAt(int line, int column, string objectName)
        {
            return GetBasicResultAt(line, column, CA1001Name, string.Format(CA1001Message, objectName));
        }
    }
}
