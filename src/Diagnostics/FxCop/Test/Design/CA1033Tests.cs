// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CA1033Tests : DiagnosticAnalyzerTestBase
    {
        #region Verifiers

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpInterfaceMethodsShouldBeCallableByChildTypesAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicInterfaceMethodsShouldBeCallableByChildTypesAnalyzer();
        }

        private static DiagnosticResult CSharpResult(int line, int column, string className, string methodName)
        {
            return GetCSharpResultAt(line, column, CSharpInterfaceMethodsShouldBeCallableByChildTypesAnalyzer.Rule, className, methodName);
        }

        private static DiagnosticResult BasicResult(int line, int column, string className, string methodName)
        {
            return GetBasicResultAt(line, column, BasicInterfaceMethodsShouldBeCallableByChildTypesAnalyzer.Rule, className, methodName);
        }

        #endregion

        #region CSharp 

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1033SimpleDiagnosticCasesCSharp()
        {
            VerifyCSharp(@"
using System;

public interface IGeneral
{
    object DoSomething();
    void DoNothing();
    void JustThrow();

    int this[int item] { get; }
    string Name { get; }
}

public class ImplementsGeneral  : IGeneral
{
    // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
    object IGeneral.DoSomething() { return null; }

    void IGeneral.DoNothing() { }
    void IGeneral.JustThrow() { throw new Exception(); }

    int IGeneral.this[int item]
    {
        // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        get
        {
            Console.WriteLine(this);
            return item;
        }
    }

    string IGeneral.Name
    {
        // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        get
        {
            Console.WriteLine(this);
            return ""name"";
        }
    }
}

public class ImplementsGeneralThree : IGeneral
{
    public ImplementsGeneralThree()
    {
        DoSomething();
        int i = this[0];
        i = i + 1;
        string name = Name;
        Console.WriteLine(name);
    }

    // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
    object IGeneral.DoSomething() { return null; }

    void IGeneral.DoNothing() { }
    void IGeneral.JustThrow() { throw new Exception(); }

    int IGeneral.this[int item]
    {
        // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        get
        {
            Console.WriteLine(this);
            return item;
        }
    }

    string IGeneral.Name
    {
        // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        get
        {
            Console.WriteLine(this);
            return ""name"";
        }
    }

    // private, this is no good
    int DoSomething() { Console.WriteLine(this); return 0; }
    internal string Name
    {
        get
        {
            Console.WriteLine(this);
            return ""name"";
        }
    }
    int this[int item]
    {
        get
        {
            Console.WriteLine(this);
            return item;
        }
    }
}
",
            CSharpResult(17, 21, "ImplementsGeneral", "IGeneral.DoSomething"),
            CSharpResult(25, 9, "ImplementsGeneral", "IGeneral.get_Item"),
            CSharpResult(35, 9, "ImplementsGeneral", "IGeneral.get_Name"),
            CSharpResult(55, 21, "ImplementsGeneralThree", "IGeneral.DoSomething"),
            CSharpResult(63, 9, "ImplementsGeneralThree", "IGeneral.get_Item"),
            CSharpResult(73, 9, "ImplementsGeneralThree", "IGeneral.get_Name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1033NestedDiagnosticCasesCSharp()
        {
            VerifyCSharp(@"
using System;

public class NestedExplicitInterfaceImplementation
{
    public interface INestedGeneral
    {
        object DoSomething();
        void DoNothing();
        void JustThrow();
        int this[int item] { get; }
        string Name { get; }
        event EventHandler TheEvent;
    }

    public class ImplementsNestedGeneral : INestedGeneral
    {
        // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        object INestedGeneral.DoSomething() { return null; }

        void INestedGeneral.DoNothing() { }
        void INestedGeneral.JustThrow() { throw new Exception(); }

        int INestedGeneral.this[int item]
        {
            // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            get
            {
                Console.WriteLine(this);
                return item;
            }
        }

        string INestedGeneral.Name
        {
            // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            get
            {
                Console.WriteLine(this);
                return ""name"";
            }
        }

        event EventHandler INestedGeneral.TheEvent
        {
            // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            add
            { Console.WriteLine(this); }
            // [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            remove
            { Console.WriteLine(this); }
        }
    }
}
",
            CSharpResult(19, 31, "ImplementsNestedGeneral", "NestedExplicitInterfaceImplementation.INestedGeneral.DoSomething"),
            CSharpResult(27, 13, "ImplementsNestedGeneral", "NestedExplicitInterfaceImplementation.INestedGeneral.get_Item"),
            CSharpResult(37, 13, "ImplementsNestedGeneral", "NestedExplicitInterfaceImplementation.INestedGeneral.get_Name"),
            CSharpResult(47, 13, "ImplementsNestedGeneral", "NestedExplicitInterfaceImplementation.INestedGeneral.add_TheEvent"),
            CSharpResult(50, 13, "ImplementsNestedGeneral", "NestedExplicitInterfaceImplementation.INestedGeneral.remove_TheEvent"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1033NoDiagnosticCasesCSharp()
        {
            VerifyCSharp(@"
using System;

public interface IGeneral
{
    object DoSomething();
    void DoNothing();
    void JustThrow();

    int this[int item] { get; }
    string Name { get; }
}

public class ImplementsGeneral  : IGeneral
{
    object IGeneral.DoSomething() { DoSomething(x); }
    public object DoSomething(bool x) { return x; }

    void IGeneral.DoNothing() { }
    void IGeneral.JustThrow() { throw new Exception(); }

    int IGeneral.this[int item]
    {
        get
        {
        }
    }

    string IGeneral.Name
    {
        get
        {
        }
    }
}

public class ImplementsGeneralThree : IGeneral
{
    public ImplementsGeneralThree()
    {
        DoSomething();
        int i = this[0];
        i = i + 1;
        string name = Name;
        Console.WriteLine(name);
    }

    object IGeneral.DoSomething() { DoSomething(x); }
    public object DoSomething(bool x) { return x; }

    void IGeneral.DoNothing() { }
    void IGeneral.JustThrow() { throw new Exception(); }

    int IGeneral.this[int item]
    {
        get
        {
            Console.WriteLine(this);
            return item;
        }
    }

    string IGeneral.Name
    {
        get
        {
            Console.WriteLine(this);
            return ""name"";
        }
    }

    public string Name
    {
        get
        {
            Console.WriteLine(this);
            return ""name"";
        }
    }
    public int this[int item]
    {
        get
        {
            Console.WriteLine(this);
            return item;
        }
    }
}

public class NestedExplicitInterfaceImplementation
{
    public interface INestedGeneral
    {
        object DoSomething();
        void DoNothing();
        void JustThrow();
        int this[int item] { get; }
        string Name { get; }
        event EventHandler TheEvent;
    }

    public class ImplementsNestedGeneral : INestedGeneral
    {
        object IGeneral.DoSomething() { DoSomething(x); }
        public object DoSomething(bool x) { return x; }
    
        void INestedGeneral.DoNothing() { }
        void INestedGeneral.JustThrow() { throw new Exception(); }

        int INestedGeneral.this[int item]
        {
            get
            {
            }
        }

        string INestedGeneral.Name
        {
            get
            {
            }
        }

        event EventHandler INestedGeneral.TheEvent
        {
            add
            { }
            remove
            { }
        }
    }
}
");
        }

        #endregion

        #region VisualBasic

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1033SimpleDiagnosticCasesBasic()
        {
            VerifyBasic(@"
Imports System

Public Interface IGeneral
    Function DoSomething() As Object
    Sub DoNothing()
    Sub JustThrow()

    Default ReadOnly Property Item(item__1 As Integer) As Integer
    ReadOnly Property Name() As String
End Interface

Public Class ImplementsGeneral
    Implements IGeneral

    ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
    Private Function IGeneral_DoSomething() As Object Implements IGeneral.DoSomething
        Return Nothing
    End Function

    Private Sub IGeneral_DoNothing() Implements IGeneral.DoNothing
    End Sub

    Private Sub IGeneral_JustThrow() Implements IGeneral.JustThrow
        Throw New Exception()
    End Sub

    Private ReadOnly Property IGeneral_Item(item As Integer) As Integer Implements IGeneral.Item
        ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        Get
            Console.WriteLine(Me)
            Return item
        End Get
    End Property

    Private ReadOnly Property IGeneral_Name() As String Implements IGeneral.Name
        ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        Get
            Console.WriteLine(Me)
            Return ""name""
        End Get
    End Property
End Class

Public Class ImplementsGeneralThree
    Implements IGeneral


    Public Sub New()
        DoSomething()
    End Sub

    ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
    Private Function IGeneral_DoSomething() As Object Implements IGeneral.DoSomething
        Return Nothing
    End Function

    Private Sub IGeneral_DoNothing() Implements IGeneral.DoNothing
    End Sub
    Private Sub IGeneral_JustThrow() Implements IGeneral.JustThrow
        Throw New Exception()
    End Sub

    Private ReadOnly Property IGeneral_Item(item As Integer) As Integer Implements IGeneral.Item
        ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        Get
            Console.WriteLine(Me)
            Return item
        End Get
    End Property

    Private ReadOnly Property IGeneral_Name() As String Implements IGeneral.Name
        ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        Get
            Console.WriteLine(Me)
            Return ""name""
        End Get
    End Property

    ' private, this is no good
    Private Function DoSomething() As Integer
        Console.WriteLine(Me)
        Return 0
    End Function
    Friend ReadOnly Property Name() As String
        Get
            Console.WriteLine(Me)
            Return ""name""
        End Get
    End Property

    Private ReadOnly Property Item(item__1 As Integer) As Integer
        Get
            Console.WriteLine(Me)
            Return item__1
        End Get
    End Property
End Class
",
            BasicResult(17, 22, "ImplementsGeneral", "IGeneral_DoSomething"),
            BasicResult(30, 9, "ImplementsGeneral", "get_IGeneral_Item"),
            BasicResult(38, 9, "ImplementsGeneral", "get_IGeneral_Name"),
            BasicResult(54, 22, "ImplementsGeneralThree", "IGeneral_DoSomething"),
            BasicResult(66, 9, "ImplementsGeneralThree", "get_IGeneral_Item"),
            BasicResult(74, 9, "ImplementsGeneralThree", "get_IGeneral_Name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1033NestedDiagnosticCasesBasic()
        {
            VerifyBasic(@"
Imports System

Public Class NestedExplicitInterfaceImplementation
    Public Interface INestedGeneral
        Function DoSomething() As Object
        Sub DoNothing()
        Sub JustThrow()
        Default ReadOnly Property Item(item__1 As Integer) As Integer
        ReadOnly Property Name() As String
        Event TheEvent As EventHandler
    End Interface

    Public Class ImplementsNestedGeneral
        Implements INestedGeneral
        ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
        Private Function INestedGeneral_DoSomething() As Object Implements INestedGeneral.DoSomething
            Return Nothing
        End Function

        Private Sub INestedGeneral_DoNothing() Implements INestedGeneral.DoNothing
        End Sub
        Private Sub INestedGeneral_JustThrow() Implements INestedGeneral.JustThrow
            Throw New Exception()
        End Sub

        Private ReadOnly Property INestedGeneral_Item(item As Integer) As Integer Implements INestedGeneral.Item
            ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            Get
                Console.WriteLine(Me)
                Return item
            End Get
        End Property

        Private ReadOnly Property INestedGeneral_Name() As String Implements INestedGeneral.Name
            ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            Get
                Console.WriteLine(Me)
                Return ""name""
            End Get
        End Property

        Private Custom Event TheEvent As EventHandler Implements INestedGeneral.TheEvent
            ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            AddHandler(ByVal value As EventHandler)
                Console.WriteLine(Me)
            End AddHandler

            ' [ExpectedWarning(""InterfaceMethodsShouldBeCallableByChildTypes"", ""DesignRules"")]
            RemoveHandler(ByVal value As EventHandler)
                Console.WriteLine(Me)
            End RemoveHandler

            RaiseEvent()
            End RaiseEvent
        End Event
    End Class
End Class
",
            BasicResult(17, 26, "ImplementsNestedGeneral", "INestedGeneral_DoSomething"),
            BasicResult(29, 13, "ImplementsNestedGeneral", "get_INestedGeneral_Item"),
            BasicResult(37, 13, "ImplementsNestedGeneral", "get_INestedGeneral_Name"),
            BasicResult(45, 13, "ImplementsNestedGeneral", "add_TheEvent"),
            BasicResult(50, 13, "ImplementsNestedGeneral", "remove_TheEvent"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1033NoDiagnosticCasesBasic()
        {
            VerifyBasic(@"
Imports System

Public Interface IGeneral
    Function DoSomething() As Object
    Sub DoNothing()
    Sub JustThrow()

    Default ReadOnly Property Item(item__1 As Integer) As Integer
    ReadOnly Property Name() As String
End Interface

Public Class ImplementsGeneral
    Implements IGeneral

    Private Function IGeneral_DoSomething() As Object Implements IGeneral.DoSomething
        Return Nothing
    End Function

    Public Function DoSomething() As Integer
        Console.WriteLine(Me)
        Return 0
    End Function

    Private Sub IGeneral_DoNothing() Implements IGeneral.DoNothing
    End Sub

    Private Sub IGeneral_JustThrow() Implements IGeneral.JustThrow
        Throw New Exception()
    End Sub

    Private ReadOnly Property IGeneral_Item(item As Integer) As Integer Implements IGeneral.Item
        Get
        End Get
    End Property

    Private ReadOnly Property IGeneral_Name() As String Implements IGeneral.Name
        Get
        End Get
    End Property
End Class

Public Class ImplementsGeneralThree
    Implements IGeneral


    Public Sub New()
        DoSomething()
    End Sub

    Private Function IGeneral_DoSomething() As Object Implements IGeneral.DoSomething
        Return Nothing
    End Function

    Private Sub IGeneral_DoNothing() Implements IGeneral.DoNothing
    End Sub
    Private Sub IGeneral_JustThrow() Implements IGeneral.JustThrow
        Throw New Exception()
    End Sub

    Private ReadOnly Property IGeneral_Item(item As Integer) As Integer Implements IGeneral.Item
        Get
        End Get
    End Property

    Private ReadOnly Property IGeneral_Name() As String Implements IGeneral.Name
        Get
            Console.WriteLine(Me)
            Return ""name""
        End Get
    End Property

    Public Function DoSomething() As Integer
        Console.WriteLine(Me)
        Return 0
    End Function

    Public ReadOnly Property Name() As String
        Get
            Console.WriteLine(Me)
            Return ""name""
        End Get
    End Property

    Public ReadOnly Property Item(item__1 As Integer) As Integer
        Get
            Console.WriteLine(Me)
            Return item__1
        End Get
    End Property
End Class

Public Class NestedExplicitInterfaceImplementation
    Public Interface INestedGeneral
        Function DoSomething() As Object
        Sub DoNothing()
        Sub JustThrow()
        Default ReadOnly Property Item(item__1 As Integer) As Integer
        ReadOnly Property Name() As String
        Event TheEvent As EventHandler
    End Interface

    Public Class ImplementsNestedGeneral
        Implements INestedGeneral
        
        Private Function INestedGeneral_DoSomething() As Object Implements INestedGeneral.DoSomething
            Return Nothing
        End Function

        Public Function DoSomething() As Integer
            Console.WriteLine(Me)
            Return 0
        End Function
    
        Private Sub INestedGeneral_DoNothing() Implements INestedGeneral.DoNothing
        End Sub
        Private Sub INestedGeneral_JustThrow() Implements INestedGeneral.JustThrow
            Throw New Exception()
        End Sub

        Private ReadOnly Property INestedGeneral_Item(item As Integer) As Integer Implements INestedGeneral.Item
            Get
            End Get
        End Property

        Private ReadOnly Property INestedGeneral_Name() As String Implements INestedGeneral.Name
            Get
            End Get
        End Property

        Private Custom Event TheEvent As EventHandler Implements INestedGeneral.TheEvent
            AddHandler(ByVal value As EventHandler)
            End AddHandler

            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler

            RaiseEvent()
            End RaiseEvent
        End Event
    End Class
End Class
");
        }

        #endregion 
    }
}
