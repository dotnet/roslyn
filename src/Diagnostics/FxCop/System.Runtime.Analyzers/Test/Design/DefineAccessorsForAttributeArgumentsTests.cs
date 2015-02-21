// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public partial class DefineAccessorsForAttributeArgumentsTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicDefineAccessorsForAttributeArgumentsAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpDefineAccessorsForAttributeArgumentsAnalyzer();
        }

        #region No Diagnostic Tests

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_NoDiagnostic_GeneralTest()
        {
            VerifyCSharp(@"
using System;

[AttributeUsage(AttributeTargets.All)]
public sealed class ArgWithGetterTestAttribute : Attribute
{
    private string m_name;

    public ArgWithGetterTestAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class PositionalArgWithSetterTestAttribute : Attribute
{
    private string m_name;
    private char m_firstInitial;

    public PositionalArgWithSetterTestAttribute(string name)
    {
        m_name = name;
        m_firstInitial = name[0];
    }

    public string Name
    {
        get { return m_name; }
    }

    public char FirstInitial
    {
        get { return m_firstInitial; }
        set { m_firstInitial = value; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_NoDiagnostic_GeneralTest()
        {
            VerifyBasic(@"
Imports System

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class ArgWithGetterTestAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PositionalArgWithSetterTestAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstInitial As Char

	Public Sub New(name As String)
		m_name = name
		m_firstInitial = name(0)
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public Property FirstInitial() As Char
		Get
			Return m_firstInitial
		End Get
		Set
			m_firstInitial = value
		End Set
	End Property
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_NoDiagnostic_GetterVisibilityTest()
        {
            VerifyCSharp(@"
using System;

[AttributeUsage(AttributeTargets.All)]
public class PublicGetterAttribute : Attribute
{
    private string m_name;

    public PublicGetterAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class MultipleConstructor1Attribute : Attribute   //Good
{
    private string m_name;
    private int m_data;

    public MultipleConstructor1Attribute(string name)
    {
        m_name = name;
    }

    private MultipleConstructor1Attribute(string name, int data)
    {
        m_name = name;
        Data = data;
    }
    private MultipleConstructor1Attribute(int data)
    {
        Data = data;
    }

    public string Name
    {
        get { return m_name; }
    }

    private int Data
    {
        get { return m_data; }
        set { m_data = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class MultipleConstructor2Attribute : Attribute   //Good
{
    private string m_name;
    private int m_data;

    public MultipleConstructor2Attribute(string name)
    {
        m_name = name;
    }

    private MultipleConstructor2Attribute(string name, int data)
    {
        m_name = name;
        Data = data;
    }
    private MultipleConstructor2Attribute(int data)
    {
        Data = data;
    }

    public string Name
    {
        get { return m_name; }
    }

    internal int Data
    {
        get { return m_data; }
        set { m_data = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class MultipleConstructor3Attribute : Attribute   //Good
{
    private string m_name;
    private int m_data;

    public MultipleConstructor3Attribute(string name)
    {
        m_name = name;
    }

    internal MultipleConstructor3Attribute(string name, int data)
    {
        m_name = name;
        Data = data;
    }
    private MultipleConstructor3Attribute(int data)
    {
        Data = data;
    }

    public string Name
    {
        get { return m_name; }
    }

    private int Data
    {
        get { return m_data; }
        set { m_data = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class MultipleConstructor4Attribute : Attribute   //Good
{
    private string m_name;
    private int m_data;

    public MultipleConstructor4Attribute(string name)
    {
        m_name = name;
    }

    internal MultipleConstructor4Attribute(string name, int data)
    {
        m_name = name;
        Data = data;
    }
    private MultipleConstructor4Attribute(int data)
    {
        Data = data;
    }
    public string Name
    {
        get { return m_name; }
    }

    public int Data
    {
        get { return m_data; }
        set { m_data = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class MultipleConstructor5Attribute : Attribute   //Good
{
    private string m_name;
    private int m_data;

    public MultipleConstructor5Attribute(string name)
    {
        m_name = name;
    }

    public MultipleConstructor5Attribute(string name, int data)
    {
        m_name = name;
        m_data = data;
    }
    public MultipleConstructor5Attribute(int data)
    {
        m_data = data;
    }
    public string Name
    {
        get { return m_name; }
    }

    public int Data
    {
        get { return m_data; }
    }
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class PublicGetterInternalAttribute : Attribute   //Good
{
    private string m_name;

    public PublicGetterInternalAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        get { return m_name; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_NoDiagnostic_GetterVisibilityTest()
        {
            VerifyBasic(@"
Imports System

<AttributeUsage(AttributeTargets.All)> _
Public Class PublicGetterAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class MultipleConstructor1Attribute
	Inherits Attribute
	'Good
	Private m_name As String
	Private m_data As Integer

	Public Sub New(name As String)
		m_name = name
	End Sub

	Private Sub New(name As String, data__1 As Integer)
		m_name = name
		Data = data__1
	End Sub
	Private Sub New(data__1 As Integer)
		Data = data__1
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Private Property Data() As Integer
		Get
			Return m_data
		End Get
		Set
			m_data = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class MultipleConstructor2Attribute
	Inherits Attribute
	'Good
	Private m_name As String
	Private m_data As Integer

	Public Sub New(name As String)
		m_name = name
	End Sub

	Private Sub New(name As String, data__1 As Integer)
		m_name = name
		Data = data__1
	End Sub
	Private Sub New(data__1 As Integer)
		Data = data__1
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Friend Property Data() As Integer
		Get
			Return m_data
		End Get
		Set
			m_data = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class MultipleConstructor3Attribute
	Inherits Attribute
	'Good
	Private m_name As String
	Private m_data As Integer

	Public Sub New(name As String)
		m_name = name
	End Sub

	Friend Sub New(name As String, data__1 As Integer)
		m_name = name
		Data = data__1
	End Sub
	Private Sub New(data__1 As Integer)
		Data = data__1
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Private Property Data() As Integer
		Get
			Return m_data
		End Get
		Set
			m_data = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class MultipleConstructor4Attribute
	Inherits Attribute
	'Good
	Private m_name As String
	Private m_data As Integer

	Public Sub New(name As String)
		m_name = name
	End Sub

	Friend Sub New(name As String, data__1 As Integer)
		m_name = name
		Data = data__1
	End Sub
	Private Sub New(data__1 As Integer)
		Data = data__1
	End Sub
	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public Property Data() As Integer
		Get
			Return m_data
		End Get
		Set
			m_data = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class MultipleConstructor5Attribute
	Inherits Attribute
	'Good
	Private m_name As String
	Private m_data As Integer

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public Sub New(name As String, data As Integer)
		m_name = name
		m_data = data
	End Sub
	Public Sub New(data As Integer)
		m_data = data
	End Sub
	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public ReadOnly Property Data() As Integer
		Get
			Return m_data
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Friend NotInheritable Class PublicGetterInternalAttribute
	Inherits Attribute
	'Good
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_NoDiagnostic_SetterVisibilityTest()
        {
            VerifyCSharp(@"
using System;

[AttributeUsage(AttributeTargets.All)]
public class ProtectedSetterAttribute : Attribute
{
    private string m_name;

    public ProtectedSetterAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        get { return m_name; }
        protected set { m_name = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ProtectedInternalSetterAttribute : Attribute
{
    private string m_name;

    public ProtectedInternalSetterAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        get { return m_name; }
        protected internal set { m_name = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class PrivateSetterAttribute : Attribute   //Good
{
    private string m_name;

    public PrivateSetterAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        get { return m_name; }
        private set { m_name = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class InternalSetterAttribute : Attribute
{
    private string m_name;

    public InternalSetterAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        get { return m_name; }
        internal set { m_name = value; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_NoDiagnostic_SetterVisibilityTest()
        {
            VerifyBasic(@"
Imports System

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedSetterAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public Property Name() As String
		Get
			Return m_name
		End Get
		Protected Set
			m_name = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedInternalSetterAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public Property Name() As String
		Get
			Return m_name
		End Get
		Protected Friend Set
			m_name = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PrivateSetterAttribute
	Inherits Attribute
	'Good
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public Property Name() As String
		Get
			Return m_name
		End Get
		Private Set
			m_name = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class InternalSetterAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public Property Name() As String
		Get
			Return m_name
		End Get
		Friend Set
			m_name = value
		End Set
	End Property
End Class

");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_NoDiagnostic_ConstructorVisibilityTest()
        {
            VerifyCSharp(@"
using System;

[AttributeUsage(AttributeTargets.All)]
public sealed class PrivateConstructorNoGetterAttribute : Attribute
{
    private string m_name;

    private PrivateConstructorNoGetterAttribute(string name, char firstLetter)
    {
        m_name = name + firstLetter;
    }

    public PrivateConstructorNoGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class PrivateConstructorPrivateGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    private PrivateConstructorPrivateGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public PrivateConstructorPrivateGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    private char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ProtectedConstructorProtectedGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    protected ProtectedConstructorProtectedGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public ProtectedConstructorProtectedGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    protected char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class InternalConstructorInternalGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    internal InternalConstructorInternalGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public InternalConstructorInternalGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    internal char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ProtectedInternalConstructorProtectedInternalGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    protected internal ProtectedInternalConstructorProtectedInternalGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public ProtectedInternalConstructorProtectedInternalGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    protected internal char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class PublicConstructorPublicGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    public PublicConstructorPublicGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public PublicConstructorPublicGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    public char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class PrivateConstructorPublicGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    private PrivateConstructorPublicGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public PrivateConstructorPublicGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    public char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ProtectedConstructorPublicGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    protected ProtectedConstructorPublicGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public ProtectedConstructorPublicGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    public char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class InternalConstructorPublicGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    internal InternalConstructorPublicGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public InternalConstructorPublicGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    public char FirstLetter
    {
        get { return m_firstLetter; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ProtectedInternalConstructorPublicGetterAttribute : Attribute
{
    private string m_name;
    private char m_firstLetter;

    protected internal ProtectedInternalConstructorPublicGetterAttribute(string name, char firstLetter)
    {
        m_name = name;
        m_firstLetter = firstLetter;
    }

    public ProtectedInternalConstructorPublicGetterAttribute(string name)
        : this(name, name[0])
    { }

    public string Name
    {
        get { return m_name; }
    }

    public char FirstLetter
    {
        get { return m_firstLetter; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_NoDiagnostic_ConstructorVisibilityTest()
        {
            VerifyBasic(@"
Imports System

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PrivateConstructorNoGetterAttribute
	Inherits Attribute
	Private m_name As String

	Private Sub New(name As String, firstLetter As Char)
		m_name = name & firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PrivateConstructorPrivateGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Private Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Private ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedConstructorProtectedGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Protected Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Protected ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class InternalConstructorInternalGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Friend Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Friend ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedInternalConstructorProtectedInternalGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Protected Friend Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Protected Friend ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PublicConstructorPublicGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Public Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PrivateConstructorPublicGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Private Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedConstructorPublicGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Protected Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class InternalConstructorPublicGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Friend Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedInternalConstructorPublicGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstLetter As Char

	Protected Friend Sub New(name As String, firstLetter As Char)
		m_name = name
		m_firstLetter = firstLetter
	End Sub

	Public Sub New(name As String)
		Me.New(name, name(0))
	End Sub

	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public ReadOnly Property FirstLetter() As Char
		Get
			Return m_firstLetter
		End Get
	End Property
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_NoDiagnostic_NestedVisibilityTest()
        {
            VerifyCSharp(@"
using System;

public class PublicContainerClass
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class NestedPublicAttribute : Attribute
    {
        private string m_name;

        public NestedPublicAttribute(string name)
        {
            m_name = name;
        }

        public string Name
        {
            get { return m_name; }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    private sealed class NestedPrivateAttribute : Attribute
    {
        private string m_name;

        public NestedPrivateAttribute(string name)
        {
            m_name = name;
        }

        private string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
    }

    private InternalContainerClass myInternalContainerClass;
    public PublicContainerClass()
    {
        myInternalContainerClass = new InternalContainerClass();
    }
    public override string ToString()
    {
        return base.ToString() + myInternalContainerClass.ToString();
    }
}

internal class InternalContainerClass
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class NestedPublicAttribute : Attribute
    {
        private string m_name;

        public NestedPublicAttribute(string name)
        {
            m_name = name;
        }

        public string Name
        {
            get { return m_name; }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    private sealed class NestedPrivateAttribute : Attribute
    {
        private string m_name;

        public NestedPrivateAttribute(string name)
        {
            m_name = name;
        }

        private string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_NoDiagnostic_NestedVisibilityTest()
        {
            VerifyBasic(@"
Imports System

Public Class PublicContainerClass
	<AttributeUsage(AttributeTargets.All)> _
	Public NotInheritable Class NestedPublicAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Public ReadOnly Property Name() As String
			Get
				Return m_name
			End Get
		End Property
	End Class

	<AttributeUsage(AttributeTargets.All)> _
	Private NotInheritable Class NestedPrivateAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Private Property Name() As String
			Get
				Return m_name
			End Get
			Set
				m_name = value
			End Set
		End Property
	End Class

	Private myInternalContainerClass As InternalContainerClass
	Public Sub New()
		myInternalContainerClass = New InternalContainerClass()
	End Sub
	Public Overrides Function ToString() As String
		Return MyBase.ToString() & myInternalContainerClass.ToString()
	End Function
End Class

Friend Class InternalContainerClass
	<AttributeUsage(AttributeTargets.All)> _
	Public NotInheritable Class NestedPublicAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Public ReadOnly Property Name() As String
			Get
				Return m_name
			End Get
		End Property
	End Class

	<AttributeUsage(AttributeTargets.All)> _
	Private NotInheritable Class NestedPrivateAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Private Property Name() As String
			Get
				Return m_name
			End Get
			Set
				m_name = value
			End Set
		End Property
	End Class
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_NoDiagnostic_InheritanceTest()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.All)]
public class ParentPublicAccessorAttribute : Attribute
{
    private string m_name;
    public ParentPublicAccessorAttribute(string name)
    {
        m_name = name;
    }
    public string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ChildPublicAccessorAttribute : ParentPublicAccessorAttribute
{
    private int m_phoneNumber;
    public ChildPublicAccessorAttribute(string name, int phoneNumber)
        : base(name)
    {
        m_phoneNumber = phoneNumber;
    }
    public int PhoneNumber
    {
        get { return m_phoneNumber; }
    }
}

// Testing to see that we don't punish child class for parent class mistake by
// not firing on the child class telling it to make the parent class property
// read-only
[AttributeUsage(AttributeTargets.All)]
public class ParentWritableAccessorAttribute : Attribute
{
    protected string m_name;
    public ParentWritableAccessorAttribute()
    {
    }

    public string Name
    {
        get { return m_name; }
        set { m_name = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ChildWritableAccessorAttribute : ParentWritableAccessorAttribute
{
    private int m_phoneNumber;
    public ChildWritableAccessorAttribute(string name, int phoneNumber)
    {
        m_phoneNumber = phoneNumber;
        base.Name = name;
    }
    public int PhoneNumber
    {
        get { return m_phoneNumber; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class GenericCovariantParameterAttribute : Attribute
{
    private IEnumerable<string> m_data;
    public GenericCovariantParameterAttribute(IEnumerable<string> data)
    {
        m_data = data;
    }

    public IEnumerable<object> Data
    {
        get { return m_data; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class GenericContravariantParameterAttribute : Attribute
{
    private IComparer<object> m_data;
    public GenericContravariantParameterAttribute(IComparer<object> data)
    {
        m_data = data;
    }
    public IComparer<string> Data
    {
        get { return m_data; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class GenericNestedCovariantParameterAttribute : Attribute
{
    private List<IEnumerable<string>> m_data;

    public GenericNestedCovariantParameterAttribute(List<IEnumerable<string>> data)
    {
        m_data = data;
    }

    public IEnumerable<IEnumerable<object>> Data
    {
        get { return m_data; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_NoDiagnostic_InheritanceTest()
        {
            VerifyBasic(@"
Imports System
Imports System.Collections.Generic

<AttributeUsage(AttributeTargets.All)> _
Public Class ParentPublicAccessorAttribute
	Inherits Attribute
	Private m_name As String
	Public Sub New(name As String)
		m_name = name
	End Sub
	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ChildPublicAccessorAttribute
	Inherits ParentPublicAccessorAttribute
	Private m_phoneNumber As Integer
	Public Sub New(name As String, phoneNumber As Integer)
		MyBase.New(name)
		m_phoneNumber = phoneNumber
	End Sub
	Public ReadOnly Property PhoneNumber() As Integer
		Get
			Return m_phoneNumber
		End Get
	End Property
End Class

' Testing to see that we don't punish child class for parent class mistake by
' not firing on the child class telling it to make the parent class property
' read-only
<AttributeUsage(AttributeTargets.All)> _
Public Class ParentWritableAccessorAttribute
	Inherits Attribute
	Protected m_name As String
	Public Sub New()
	End Sub

	Public Property Name() As String
		Get
			Return m_name
		End Get
		Set
			m_name = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ChildWritableAccessorAttribute
	Inherits ParentWritableAccessorAttribute
	Private m_phoneNumber As Integer
	Public Sub New(name As String, phoneNumber As Integer)
		m_phoneNumber = phoneNumber
		MyBase.Name = name
	End Sub
	Public ReadOnly Property PhoneNumber() As Integer
		Get
			Return m_phoneNumber
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class GenericCovariantParameterAttribute
	Inherits Attribute
	Private m_data As IEnumerable(Of String)
	Public Sub New(data As IEnumerable(Of String))
		m_data = data
	End Sub

	Public ReadOnly Property Data() As IEnumerable(Of Object)
		Get
			Return m_data
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class GenericContravariantParameterAttribute
	Inherits Attribute
	Private m_data As IComparer(Of Object)
	Public Sub New(data As IComparer(Of Object))
		m_data = data
	End Sub
	Public ReadOnly Property Data() As IComparer(Of String)
		Get
			Return m_data
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class GenericNestedCovariantParameterAttribute
	Inherits Attribute
	Private m_data As List(Of IEnumerable(Of String))

	Public Sub New(data As List(Of IEnumerable(Of String)))
		m_data = data
	End Sub

	Public ReadOnly Property Data() As IEnumerable(Of IEnumerable(Of Object))
		Get
			Return m_data
		End Get
	End Property
End Class
");
        }

        #endregion

        #region Diagnostic Tests

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_GeneralTest()
        {
            VerifyCSharp(@"
using System;

[AttributeUsage(AttributeTargets.All)]
public sealed class NoAccessorTestAttribute : Attribute
{
    private string m_name;

    public NoAccessorTestAttribute(string name)
    {
        m_name = name;
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class SetterOnlyTestAttribute : Attribute
{
    private string m_name;

    public SetterOnlyTestAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        set { m_name = value; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class ArgWithSetterTestAttribute : Attribute
{
    private string m_name;
    private char m_firstInitial;

    public ArgWithSetterTestAttribute(string name)
    {
        m_name = name;
        m_firstInitial = name[0];
    }

    public string Name
    {
        get { return m_name; }
        set { m_name = value; }
    }

    public char FirstInitial
    {
        get { return m_firstInitial; }
        set { m_firstInitial = value; }
    }
}
",
            GetCA1019CSharpDefaultResultAt(9, 43, "name", "NoAccessorTestAttribute"),
            GetCA1019CSharpDefaultResultAt(20, 43, "name", "SetterOnlyTestAttribute"),
            GetCA1019CSharpRemoveSetterResultAt(27, 9, "Name", "name"),
            GetCA1019CSharpRemoveSetterResultAt(46, 9, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_GeneralTestWithScope()
        {
            VerifyCSharp(@"
using System;

[AttributeUsage(AttributeTargets.All)]
public sealed class NoAccessorTestAttribute : Attribute
{
    private string m_name;

    public NoAccessorTestAttribute(string name)
    {
        m_name = name;
    }
}

[|[AttributeUsage(AttributeTargets.All)]
public sealed class SetterOnlyTestAttribute : Attribute
{
    private string m_name;

    public SetterOnlyTestAttribute(string name)
    {
        m_name = name;
    }

    public string Name
    {
        set { m_name = value; }
    }
}|]

[AttributeUsage(AttributeTargets.All)]
public sealed class ArgWithSetterTestAttribute : Attribute
{
    private string m_name;
    private char m_firstInitial;

    public ArgWithSetterTestAttribute(string name)
    {
        m_name = name;
        m_firstInitial = name[0];
    }

    public string Name
    {
        get { return m_name; }
        set { m_name = value; }
    }

    public char FirstInitial
    {
        get { return m_firstInitial; }
        set { m_firstInitial = value; }
    }
}
",
            GetCA1019CSharpDefaultResultAt(20, 43, "name", "SetterOnlyTestAttribute"),
            GetCA1019CSharpRemoveSetterResultAt(27, 9, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_GeneralTest()
        {
            VerifyBasic(@"
Imports System

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class NoAccessorTestAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class SetterOnlyTestAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public WriteOnly Property Name() As String
		Set
			m_name = value
		End Set
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class ArgWithSetterTestAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstInitial As Char

	Public Sub New(name As String)
		m_name = name
		m_firstInitial = name(0)
	End Sub

	Public Property Name() As String
		Get
			Return m_name
		End Get
		Set
			m_name = value
		End Set
	End Property

	Public Property FirstInitial() As Char
		Get
			Return m_firstInitial
		End Get
		Set
			m_firstInitial = value
		End Set
	End Property
End Class
",
            GetCA1019BasicDefaultResultAt(9, 17, "name", "NoAccessorTestAttribute"),
            GetCA1019BasicDefaultResultAt(19, 17, "name", "SetterOnlyTestAttribute"),
            GetCA1019BasicRemoveSetterResultAt(24, 3, "Name", "name"),
            GetCA1019BasicRemoveSetterResultAt(45, 3, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_GeneralTestWithScope()
        {
            VerifyBasic(@"
Imports System

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class NoAccessorTestAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub
End Class

[|<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class SetterOnlyTestAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public WriteOnly Property Name() As String
		Set
			m_name = value
		End Set
	End Property
End Class|]

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class ArgWithSetterTestAttribute
	Inherits Attribute
	Private m_name As String
	Private m_firstInitial As Char

	Public Sub New(name As String)
		m_name = name
		m_firstInitial = name(0)
	End Sub

	Public Property Name() As String
		Get
			Return m_name
		End Get
		Set
			m_name = value
		End Set
	End Property

	Public Property FirstInitial() As Char
		Get
			Return m_firstInitial
		End Get
		Set
			m_firstInitial = value
		End Set
	End Property
End Class
",
            GetCA1019BasicDefaultResultAt(19, 17, "name", "SetterOnlyTestAttribute"),
            GetCA1019BasicRemoveSetterResultAt(24, 3, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_IncreaseVisibility_GetterVisibilityTest()
        {
            VerifyCSharp(@"
using System;

[AttributeUsage(AttributeTargets.All)]
internal sealed class InternalGetterInternalAttribute : Attribute
{
    private string m_name;

    public InternalGetterInternalAttribute(string name)
    {
        m_name = name;
    }

    internal string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
internal class ProtectedInternalGetterInternalAttribute : Attribute
{
    private string m_name;

    public ProtectedInternalGetterInternalAttribute(string name)
    {
        m_name = name;
    }

    protected internal string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ProtectedGetterAttribute : Attribute
{
    private string m_name;
    private int m_data;

    public ProtectedGetterAttribute(string name)
    {
        m_name = name;
    }
    public ProtectedGetterAttribute(string name, int data)
    {
        m_name = name;
        m_data = data;
    }
    public ProtectedGetterAttribute(int data)
    {
        m_data = data;
    }

    protected string Name
    {
        get { return m_name; }
    }

    public int Data
    {
        get { return m_data; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ProtectedInternalGetterAttribute : Attribute
{
    private string m_name;

    public ProtectedInternalGetterAttribute(string name)
    {
        m_name = name;
    }

    protected internal string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class InternalGetterAttribute : Attribute   //Bad
{
    private string m_name;

    public InternalGetterAttribute(string name)
    {
        m_name = name;
    }

    internal string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class PrivateGetterAttribute : Attribute   //Bad
{
    private string m_name;

    public PrivateGetterAttribute(string name)
    {
        m_name = name;
    }

    private string Name
    {
        get { return m_name; }
    }
}
",
            GetCA1019CSharpIncreaseVisibilityResultAt(16, 9, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(32, 9, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(58, 9, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(79, 9, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(95, 9, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(111, 9, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_IncreaseVisibility_GetterVisibilityTest()
        {
            VerifyBasic(@"
Imports System

<AttributeUsage(AttributeTargets.All)> _
Friend NotInheritable Class InternalGetterInternalAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Friend ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Friend Class ProtectedInternalGetterInternalAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Protected Friend ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedGetterAttribute
	Inherits Attribute
	Private m_name As String
	Private m_data As Integer

	Public Sub New(name As String)
		m_name = name
	End Sub
	Public Sub New(name As String, data As Integer)
		m_name = name
		m_data = data
	End Sub
	Public Sub New(data As Integer)
		m_data = data
	End Sub

	Protected ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property

	Public ReadOnly Property Data() As Integer
		Get
			Return m_data
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ProtectedInternalGetterAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Protected Friend ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class InternalGetterAttribute
	Inherits Attribute
	'Bad
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Friend ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PrivateGetterAttribute
	Inherits Attribute
	'Bad
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Private ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class PublicPropertyPrivateAccessorTestAttribute
	Inherits Attribute
	Private m_name As String

	Public Sub New(name As String)
		m_name = name
	End Sub

	Public ReadOnly Property Name() As String
		Private Get
			Return m_name
		End Get
	End Property
End Class
",
            GetCA1019BasicIncreaseVisibilityResultAt(14, 3, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(30, 3, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(54, 3, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(76, 3, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(93, 3, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(110, 3, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(126, 11, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_IncreaseVisibility_NestedVisibilityTest()
        {
            VerifyCSharp(@"
using System;

public partial class PublicContainerClass
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class NestedInternalAttribute : Attribute
    {
        private string m_name;

        public NestedInternalAttribute(string name)
        {
            m_name = name;
        }

        internal string Name
        {
            get { return m_name; }
        }
    }
}

internal class InternalContainerClass
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class NestedInternalAttribute : Attribute
    {
        private string m_name;

        public NestedInternalAttribute(string name)
        {
            m_name = name;
        }

        internal string Name
        {
            get { return m_name; }
        }
    }
}

public partial class PublicContainerClass
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class NestedPublicAttribute : Attribute
    {
        private string m_name;

        public NestedPublicAttribute(string name)
        {
            Name = name;
        }

        private string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    protected class NestedProtectedAttribute : Attribute
    {
        private string m_name;

        public NestedProtectedAttribute(string name)
        {
            m_name = name;
        }

        protected string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    protected internal class NestedProtectedInternalAttribute : Attribute
    {
        private string m_name;

        public NestedProtectedInternalAttribute(string name)
        {
            m_name = name;
        }

        protected internal string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
    }
}
",
            GetCA1019CSharpIncreaseVisibilityResultAt(18, 13, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(37, 13, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(56, 13, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(73, 13, "Name", "name"),
            GetCA1019CSharpIncreaseVisibilityResultAt(90, 13, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_IncreaseVisibility_NestedVisibilityTest()
        {
            VerifyBasic(@"
Imports System

Public Partial Class PublicContainerClass
	<AttributeUsage(AttributeTargets.All)> _
	Friend NotInheritable Class NestedInternalAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Friend ReadOnly Property Name() As String
			Get
				Return m_name
			End Get
		End Property
	End Class
End Class

Friend Class InternalContainerClass
	<AttributeUsage(AttributeTargets.All)> _
	Friend NotInheritable Class NestedInternalAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Friend ReadOnly Property Name() As String
			Get
				Return m_name
			End Get
		End Property
	End Class
End Class

Public Partial Class PublicContainerClass
	<AttributeUsage(AttributeTargets.All)> _
	Public NotInheritable Class NestedPublicAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			Me.Name = name
		End Sub

		Private Property Name() As String
			Get
				Return m_name
			End Get
			Set
				m_name = value
			End Set
		End Property
	End Class

	<AttributeUsage(AttributeTargets.All)> _
	Protected Class NestedProtectedAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Protected Property Name() As String
			Get
				Return m_name
			End Get
			Set
				m_name = value
			End Set
		End Property
	End Class

	<AttributeUsage(AttributeTargets.All)> _
	Protected Friend Class NestedProtectedInternalAttribute
		Inherits Attribute
		Private m_name As String

		Public Sub New(name As String)
			m_name = name
		End Sub

		Protected Friend Property Name() As String
			Get
				Return m_name
			End Get
			Set
				m_name = value
			End Set
		End Property
	End Class
End Class
",
            GetCA1019BasicIncreaseVisibilityResultAt(15, 4, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(33, 4, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(51, 4, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(70, 4, "Name", "name"),
            GetCA1019BasicIncreaseVisibilityResultAt(89, 4, "Name", "name"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_CA1019_RemvoeSetter_InheritanceTest()
        {
            VerifyCSharp(@"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.All)]
public class ParentNoAccessorAttribute : Attribute
{
    private string m_name;
    public ParentNoAccessorAttribute(string name)
    {
        m_name = name;
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ChildNoParentAccessorAttribute : ParentNoAccessorAttribute
{
    private int m_phoneNumber;
    public ChildNoParentAccessorAttribute(string name, int phoneNumber)
        : base(name)
    {
        m_phoneNumber = phoneNumber;
    }
    public int PhoneNumber
    {
        get { return m_phoneNumber; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ParentProtectedAccessorAttribute : Attribute
{
    private string m_name;
    public ParentProtectedAccessorAttribute(string name)
    {
        m_name = name;
    }
    protected string Name
    {
        get { return m_name; }
    }
}

// I should be firing on this but I should be firing the default resolution instead of 
// the IncreaseVisiblity resolution because I can't assume the user can modify the parent class
[AttributeUsage(AttributeTargets.All)]
public class ChildProtectedAccessorAttribute : ParentProtectedAccessorAttribute
{
    private int m_phoneNumber;
    public ChildProtectedAccessorAttribute(string name, int phoneNumber)
        : base(name)
    {
        m_phoneNumber = phoneNumber;
    }
    public int PhoneNumber
    {
        get { return m_phoneNumber; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ParentPrivateAccessorAttribute : Attribute
{
    private string m_name;
    public ParentPrivateAccessorAttribute(string name)
    {
        m_name = name;
    }
    private string Name
    {
        get { return m_name; }
    }
}

// I should be firing on this but I should be firing the default resolution instead of 
// the IncreaseVisiblity resolution because I can't assume the user can modify the parent class
[AttributeUsage(AttributeTargets.All)]
public class ChildPrivateAccessorAttribute : ParentPrivateAccessorAttribute
{
    private int m_phoneNumber;
    public ChildPrivateAccessorAttribute(string name, int phoneNumber)
        : base(name)
    {
        m_phoneNumber = phoneNumber;
    }
    public int PhoneNumber
    {
        get { return m_phoneNumber; }
    }
}

// Testing to see  I can handle multiple instances of the
// same property name in the inheritance hierarchy
[AttributeUsage(AttributeTargets.All)]
public class ParentSameAccessorAttribute : Attribute
{
    private string m_name;
    public ParentSameAccessorAttribute(string name)
    {
        m_name = name;
    }
    public string Name
    {
        get { return m_name; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ChildSameAccessorAttribute : ParentSameAccessorAttribute
{
    private int m_phoneNumber;
    public ChildSameAccessorAttribute(string name, int phoneNumber)
        : base(name)
    {
        m_phoneNumber = phoneNumber;
    }

    protected new string Name
    {
        get { return base.Name; }
    }
    public int PhoneNumber
    {
        get { return m_phoneNumber; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class GenericCovariantParameterAttribute : Attribute
{
    private IEnumerable<object> m_data;
    public GenericCovariantParameterAttribute(IEnumerable<object> data)
    {
        m_data = data;
    }

    // IEnumerable<object> is not assignable to IEnumerable<string>, so the rule doesn't recognize this accessor and it should fire.
    public IEnumerable<string> Data
    {
        get { return (IEnumerable<string>)m_data; }
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class GenericContravariantParameterAttribute : Attribute
{
    private IComparer<string> m_data;
    public GenericContravariantParameterAttribute(IComparer<string> data)
    {
        m_data = data;
    }

    // IComparer<string> is not assignable to IComparer<object>, so the rule doesn't recognize this accessor and it should fire.
    public IComparer<object> Data
    {
        get { return (IComparer<object>)m_data; }
    }
}
",
            GetCA1019CSharpDefaultResultAt(9, 45, "name", "ParentNoAccessorAttribute"),
            GetCA1019CSharpDefaultResultAt(19, 50, "name", "ChildNoParentAccessorAttribute"),
            GetCA1019CSharpIncreaseVisibilityResultAt(40, 9, "Name", "name"),
            GetCA1019CSharpDefaultResultAt(50, 51, "name", "ChildProtectedAccessorAttribute"),
            GetCA1019CSharpIncreaseVisibilityResultAt(71, 9, "Name", "name"),
            GetCA1019CSharpDefaultResultAt(81, 49, "name", "ChildPrivateAccessorAttribute"),
            GetCA1019CSharpIncreaseVisibilityResultAt(120, 9, "Name", "name"),
            GetCA1019CSharpDefaultResultAt(132, 67, "data", "GenericCovariantParameterAttribute"),
            GetCA1019CSharpDefaultResultAt(148, 69, "data", "GenericContravariantParameterAttribute"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_CA1019_RemvoeSetter_InheritanceTest()
        {
            VerifyBasic(@"
Imports System
Imports System.Collections.Generic

<AttributeUsage(AttributeTargets.All)> _
Public Class ParentNoAccessorAttribute
	Inherits Attribute
	Private m_name As String
	Public Sub New(name As String)
		m_name = name
	End Sub
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ChildNoParentAccessorAttribute
	Inherits ParentNoAccessorAttribute
	Private m_phoneNumber As Integer
	Public Sub New(name As String, phoneNumber As Integer)
		MyBase.New(name)
		m_phoneNumber = phoneNumber
	End Sub
	Public ReadOnly Property PhoneNumber() As Integer
		Get
			Return m_phoneNumber
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ParentProtectedAccessorAttribute
	Inherits Attribute
	Private m_name As String
	Public Sub New(name As String)
		m_name = name
	End Sub
	Protected ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

' I should be firing on this but I should be firing the default resolution instead of 
' the IncreaseVisiblity resolution because I can't assume the user can modify the parent class
<AttributeUsage(AttributeTargets.All)> _
Public Class ChildProtectedAccessorAttribute
	Inherits ParentProtectedAccessorAttribute
	Private m_phoneNumber As Integer
	Public Sub New(name As String, phoneNumber As Integer)
		MyBase.New(name)
		m_phoneNumber = phoneNumber
	End Sub
	Public ReadOnly Property PhoneNumber() As Integer
		Get
			Return m_phoneNumber
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ParentPrivateAccessorAttribute
	Inherits Attribute
	Private m_name As String
	Public Sub New(name As String)
		m_name = name
	End Sub
	Private ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

' I should be firing on this but I should be firing the default resolution instead of 
' the IncreaseVisiblity resolution because I can't assume the user can modify the parent class
<AttributeUsage(AttributeTargets.All)> _
Public Class ChildPrivateAccessorAttribute
	Inherits ParentPrivateAccessorAttribute
	Private m_phoneNumber As Integer
	Public Sub New(name As String, phoneNumber As Integer)
		MyBase.New(name)
		m_phoneNumber = phoneNumber
	End Sub
	Public ReadOnly Property PhoneNumber() As Integer
		Get
			Return m_phoneNumber
		End Get
	End Property
End Class

' Testing to see  I can handle multiple instances of the
' same property name in the inheritance hierarchy
<AttributeUsage(AttributeTargets.All)> _
Public Class ParentSameAccessorAttribute
	Inherits Attribute
	Private m_name As String
	Public Sub New(name As String)
		m_name = name
	End Sub
	Public ReadOnly Property Name() As String
		Get
			Return m_name
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public Class ChildSameAccessorAttribute
	Inherits ParentSameAccessorAttribute
	Private m_phoneNumber As Integer
	Public Sub New(name As String, phoneNumber As Integer)
		MyBase.New(name)
		m_phoneNumber = phoneNumber
	End Sub

	Protected Shadows ReadOnly Property Name() As String
		Get
			Return MyBase.Name
		End Get
	End Property
	Public ReadOnly Property PhoneNumber() As Integer
		Get
			Return m_phoneNumber
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class GenericCovariantParameterAttribute
	Inherits Attribute
	Private m_data As IEnumerable(Of Object)
	Public Sub New(data As IEnumerable(Of Object))
		m_data = data
	End Sub

	' IEnumerable<object> is not assignable to IEnumerable<string>, so the rule doesn't recognize this accessor and it should fire.
	Public ReadOnly Property Data() As IEnumerable(Of String)
		Get
			Return DirectCast(m_data, IEnumerable(Of String))
		End Get
	End Property
End Class

<AttributeUsage(AttributeTargets.All)> _
Public NotInheritable Class GenericContravariantParameterAttribute
	Inherits Attribute
	Private m_data As IComparer(Of String)
	Public Sub New(data As IComparer(Of String))
		m_data = data
	End Sub

	' IComparer<string> is not assignable to IComparer<object>, so the rule doesn't recognize this accessor and it should fire.
	Public ReadOnly Property Data() As IComparer(Of Object)
		Get
			Return DirectCast(m_data, IComparer(Of Object))
		End Get
	End Property
End Class
",
            GetCA1019BasicDefaultResultAt(9, 17, "name", "ParentNoAccessorAttribute"),
            GetCA1019BasicDefaultResultAt(18, 17, "name", "ChildNoParentAccessorAttribute"),
            GetCA1019BasicIncreaseVisibilityResultAt(37, 3, "Name", "name"),
            GetCA1019BasicDefaultResultAt(49, 17, "name", "ChildProtectedAccessorAttribute"),
            GetCA1019BasicIncreaseVisibilityResultAt(68, 3, "Name", "name"),
            GetCA1019BasicDefaultResultAt(80, 17, "name", "ChildPrivateAccessorAttribute"),
            GetCA1019BasicIncreaseVisibilityResultAt(117, 3, "Name", "name"),
            GetCA1019BasicDefaultResultAt(132, 17, "data", "GenericCovariantParameterAttribute"),
            GetCA1019BasicDefaultResultAt(148, 17, "data", "GenericContravariantParameterAttribute"));
        }

        #endregion

        internal static string CA1019Name = "CA1019";

        private static DiagnosticResult GetCA1019CSharpDefaultResultAt(int line, int column, string paramName, string attributeTypeName)
        {
            // Add a public read-only property accessor for positional argument '{0}' of attribute '{1}'.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsDefault, paramName, attributeTypeName);
            return GetCSharpResultAt(line, column, CA1019Name, message);
        }

        private static DiagnosticResult GetCA1019BasicDefaultResultAt(int line, int column, string paramName, string attributeTypeName)
        {
            // Add a public read-only property accessor for positional argument '{0}' of attribute '{1}'.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsDefault, paramName, attributeTypeName);
            return GetBasicResultAt(line, column, CA1019Name, message);
        }

        private static DiagnosticResult GetCA1019CSharpIncreaseVisibilityResultAt(int line, int column, string propertyName, string paramName)
        {
            // If '{0}' is the property accessor for positional argument '{1}', make it public.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsIncreaseVisibility, propertyName, paramName);
            return GetCSharpResultAt(line, column, CA1019Name, message);
        }

        private static DiagnosticResult GetCA1019BasicIncreaseVisibilityResultAt(int line, int column, string propertyName, string paramName)
        {
            // If '{0}' is the property accessor for positional argument '{1}', make it public.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsIncreaseVisibility, propertyName, paramName);
            return GetBasicResultAt(line, column, CA1019Name, message);
        }

        private static DiagnosticResult GetCA1019CSharpRemoveSetterResultAt(int line, int column, string propertyName, string paramName)
        {
            // Remove the property setter from '{0}' or reduce its accessibility because it corresponds to positional argument '{1}'.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsRemoveSetter, propertyName, paramName);
            return GetCSharpResultAt(line, column, CA1019Name, message);
        }

        private static DiagnosticResult GetCA1019BasicRemoveSetterResultAt(int line, int column, string propertyName, string paramName)
        {
            // Remove the property setter from '{0}' or reduce its accessibility because it corresponds to positional argument '{1}'.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsRemoveSetter, propertyName, paramName);
            return GetBasicResultAt(line, column, CA1019Name, message);
        }
    }
}
