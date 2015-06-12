// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack;
using Microsoft.AnalyzerPowerPack.Naming;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    public class CA1715Test : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA1715DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA1715DiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestInterfaceNamesCSharp()
        {
            VerifyCSharp(@"
public interface Controller
{
    void SomeMethod();
}

public interface 日本語
{
    void SomeMethod();
}

public interface _Controller
{
    void SomeMethod();
}

public interface _日本語
{
    void SomeMethod();
}

public interface Internet
{
    void SomeMethod();
}

public interface Iinternet
{
    void SomeMethod();
}

public class Class1
{
    public interface Controller
    {
        void SomeMethod();
    }
}

public interface IAmAnInterface
{
    void SomeMethod();
}
",
                GetCA1715CSharpResultAt(2, 18, CA1715InterfaceMessage),
                GetCA1715CSharpResultAt(7, 18, CA1715InterfaceMessage),
                GetCA1715CSharpResultAt(12, 18, CA1715InterfaceMessage),
                GetCA1715CSharpResultAt(17, 18, CA1715InterfaceMessage),
                GetCA1715CSharpResultAt(22, 18, CA1715InterfaceMessage),
                GetCA1715CSharpResultAt(27, 18, CA1715InterfaceMessage),
                GetCA1715CSharpResultAt(34, 22, CA1715InterfaceMessage));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestTypeParameterNamesCSharp()
        {
            VerifyCSharp(@"
using System;

public class IInterface<V>
{
}

public class IAnotherInterface<本語>
{
}

public delegate void Callback<V>();

public class Class2<V>
{
}

public class Class2<T, V>
{
}

public class Class3<Type>
{
}

public class Class3<T, Type>
{
}

public class Base<Key, Value>
{
}

public class Derived<Key, Value> : Base<Key, Value>
{
}

public class Class4<Type1>
{
    public void AnotherMethod<Type2>()
    {
        Console.WriteLine(typeof(Type2).ToString());
    }

    public void Method<Type2>(Type2 type)
    {
        Console.WriteLine(type);
    }

    public void Method<K, V>(K key, V value)
    {
        Console.WriteLine(key.ToString() + value.ToString());
    }
}

public class Class5<_Type1>
{
    public void Method<_K, _V>(_K key, _V value)
    {
        Console.WriteLine(key.ToString() + value.ToString());
    }
}

public class Class6<TTypeParameter>
{
}
",
                GetCA1715CSharpResultAt(4, 25, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(8, 32, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(12, 31, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(14, 21, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(18, 24, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(22, 21, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(26, 24, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(30, 19, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(30, 24, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(34, 22, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(34, 27, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(38, 21, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(40, 31, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(45, 24, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(50, 24, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(50, 27, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(56, 21, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(58, 24, CA1715TypeParameterMessage),
                GetCA1715CSharpResultAt(58, 28, CA1715TypeParameterMessage));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestInterfaceNamesBasic()
        {
            VerifyBasic(@"
Public Interface Controller
    Sub SomeMethod()
End Interface

Public Interface 日本語
    Sub SomeMethod()
End Interface

Public Interface _Controller
    Sub SomeMethod()
End Interface

Public Interface _日本語
    Sub SomeMethod()
End Interface

Public Interface Internet
    Sub SomeMethod()
End Interface

Public Interface Iinternet
    Sub SomeMethod()
End Interface

Public Class Class1
    Public Interface Controller
        Sub SomeMethod()
    End Interface
End Class

Public Interface IAmAnInterface
    Sub SomeMethod()
End Interface
",
                GetCA1715BasicResultAt(2, 18, CA1715InterfaceMessage),
                GetCA1715BasicResultAt(6, 18, CA1715InterfaceMessage),
                GetCA1715BasicResultAt(10, 18, CA1715InterfaceMessage),
                GetCA1715BasicResultAt(14, 18, CA1715InterfaceMessage),
                GetCA1715BasicResultAt(18, 18, CA1715InterfaceMessage),
                GetCA1715BasicResultAt(22, 18, CA1715InterfaceMessage),
                GetCA1715BasicResultAt(27, 22, CA1715InterfaceMessage));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestTypeParameterNamesBasic()
        {
            VerifyBasic(@"
Import System

Public Class IInterface(Of V)
End Class

Public Class IAnotherInterface(Of 本語)
End Class

Public Delegate Sub Callback(Of V)()

Public Class Class2(Of V)
End Class

Public Class Class2(Of T, V)
End Class

Public Class Class3(Of Type)
End Class

Public Class Class3(Of T, Type)
End Class

Public Class Base(Of Key, Value)
End Class

Public Class Derived(Of Key, Value)
    Inherits Base(Of Key, Value)
End Class

Public Class Class4(Of Type1)
    Public Sub AnotherMethod(Of Type2)()
        Console.WriteLine(GetType(Type2).ToString())
    End Sub

    Public Sub Method(Of Type2)(type As Type2)
        Console.WriteLine(type)
    End Sub

    Public Sub Method(Of K, V)(key As K, value As V)
        Console.WriteLine(key.ToString() + value.ToString())
    End Sub
End Class

Public Class Class5(Of _Type1)
    Public Sub Method(Of _K, _V)(key As _K, value As _V)
        Console.WriteLine(key.ToString() + value.ToString())
    End Sub
End Class

Public Class Class6(Of TTypeParameter)
End Class
",
                GetCA1715BasicResultAt(4, 28, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(7, 35, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(10, 33, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(12, 24, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(15, 27, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(18, 24, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(21, 27, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(24, 22, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(24, 27, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(27, 25, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(27, 30, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(31, 24, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(32, 33, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(36, 26, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(40, 26, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(40, 29, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(45, 24, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(46, 26, CA1715TypeParameterMessage),
                GetCA1715BasicResultAt(46, 30, CA1715TypeParameterMessage));
        }

        internal static string CA1715InterfaceMessage = AnalyzerPowerPackRulesResources.InterfaceNamesShouldStartWithI;
        internal static string CA1715TypeParameterMessage = AnalyzerPowerPackRulesResources.TypeParameterNamesShouldStartWithT;

        private static DiagnosticResult GetCA1715CSharpResultAt(int line, int column, string message)
        {
            return GetCSharpResultAt(line, column, CA1715DiagnosticAnalyzer.RuleId, message);
        }

        private static DiagnosticResult GetCA1715BasicResultAt(int line, int column, string message)
        {
            return GetBasicResultAt(line, column, CA1715DiagnosticAnalyzer.RuleId, message);
        }
    }
}
