' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdCSInterfaceProjection() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesWinRT="true" AssemblyName="SampleComponent">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document>
using System;
using Windows.Foundation;
namespace SampleComponent
{
    public sealed class Test1 : IDisposable
    {
        public void Dispose() { }
        void IDisposable.{|Definition:Dispose|}() { }
        public void Close() { }
    }
    public sealed class Test2 : IDisposable
    {
        public void {|Definition:Dispose|}() { }
    }
    public sealed class M
    {
        void Some()
        {
            Test1 t1 = new Test1();
            t1.Dispose();
            (t1 as IDisposable).[|$$Dispose|]();
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdVBInterfaceProjection() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferencesWinRT="true">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document>
Imports System
Imports Windows.Foundation
Public NotInheritable Class Class1
    Implements IDisposable
    Private Sub {|Definition:Dispose|}() Implements IDisposable.[|Dispose|]
    End Sub
    Public Sub Close()
    End Sub
End Class
Public NotInheritable Class Class2
    Implements IDisposable
    Private Sub {|Definition:IDisposable_Dispose|}() Implements IDisposable.[|Dispose|]
        Throw New NotImplementedException()
    End Sub
    Public Sub Dispose()
    End Sub
End Class
Public NotInheritable Class M
    Sub Some()
        Dim c1 As New Class1
        Dim c3 = DirectCast(New Class1(), IDisposable)
        c3.[|Dispose|]()
        c1.Close()
        Dim c2 As IDisposable = CType(c1, IDisposable)
        c2.[|$$Dispose|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdCSCollectionProjection() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesWinRT="true" AssemblyName="SampleComponent">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document><![CDATA[
using System.Collections.Generic;
using System.Collections;
using Windows.Foundation.Collections;
namespace SampleComponent
{
    public sealed class Test : IEnumerable<int>
    {
        int[] x = new int[] { 1, 2, 3 };
        IIterator<int> y;
        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return x[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            for(int i=0; i< 3; i++)
            {
                yield return x[i];
            }
        }
        public IIterator<int> {|Definition:$$First|}()
        {
            return y;
        }
    } 
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdVBCollectionProjection() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferencesWinRT="true">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document>
Imports System.Collections
Imports System.Collections.Generic
Imports Windows.Foundation.Collections
Public NotInheritable Class Class1
    Implements IEnumerable(Of Integer)
    Dim x As IEnumerator(Of Integer)
    Dim y As IIterator(Of Integer)
    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Return x
    End Function
    Private Function AGetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return x
    End Function
    Public Function {|Definition:$$First|}() As IIterator(Of Integer)
        Return y
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdCSEventProjection() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesWinRT="true" AssemblyName="SampleComponent">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document><![CDATA[
using System;
using System.Runtime.InteropServices.WindowsRuntime;
namespace SampleComponent
{
    public sealed class Class1 
    {
        private EventRegistrationTokenTable<EventHandler<int>> A = null;
        public event EventHandler<int> {|Definition:AChange|}
        {
            add
            {
                return EventRegistrationTokenTable<EventHandler<int>>
                    .GetOrCreateEventRegistrationTokenTable(ref A)
                    .AddEventHandler(value);
            }
            remove
            {
                EventRegistrationTokenTable<EventHandler<int>>
                    .GetOrCreateEventRegistrationTokenTable(ref A)
                    .RemoveEventHandler(value);
            }
        }
        public void Foo()
        {
            Class1 c1 = new Class1();
            Class2 c2 = new Class2();
            c1.[|AChange|] += null;
            c1.[|$$AChange|] -= null;
            c2.AChange += null;
            c2.AChange -= null;
        }
    }
    public sealed class Class2
    {
        public event EventHandler<int> AChange
        {
            add
            {
                throw null;
            }
            remove
            {
                throw null;
            }
        }
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdVBEventProjection() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferencesWinRT="true">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document>
Imports System
Imports System.Runtime.InteropServices.WindowsRuntime
Public NotInheritable Class Class1
    Private A As _
    EventRegistrationTokenTable(Of EventHandler(Of Integer))

    Public Custom Event {|Definition:AChange|} As EventHandler(Of Integer)

        AddHandler(ByVal handler As EventHandler(Of Integer))
            Return EventRegistrationTokenTable(Of EventHandler(Of Integer)).
            GetOrCreateEventRegistrationTokenTable(A).
            AddEventHandler(handler)
        End AddHandler

        RemoveHandler(ByVal handler As EventRegistrationToken)
            EventRegistrationTokenTable(Of EventHandler(Of Integer)).
            GetOrCreateEventRegistrationTokenTable(A).
            RemoveEventHandler(handler)
        End RemoveHandler

        RaiseEvent(ByVal sender As Class1, ByVal args As Integer)
            Dim temp As EventHandler(Of Integer) =
            EventRegistrationTokenTable(Of EventHandler(Of Integer)).
            GetOrCreateEventRegistrationTokenTable(A).
            InvocationList
            If temp IsNot Nothing Then
                temp(sender, args)
            End If
        End RaiseEvent
    End Event
End Class
Module N
    Public Event AChange As EventHandler(Of Integer)
    Public Sub Test()
        Dim c1 As New Class1
        AddHandler c1.[|$$AChange|], AddressOf HandleSub
    End Sub
    Private Sub HandleSub(sender As Object, e As Integer)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdCSAllIsWellTest() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesWinRT="true" AssemblyName="SampleComponent">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
namespace SampleComponent
{
    public interface I
    {
        void Add(int item);
    }
    public sealed class Class1 : I, IList<int>
    {
        int IList<int>.this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        int ICollection<int>.Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool ICollection<int>.IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void ICollection<int>.{|Definition:Add|}(int item)
        {
            throw new NotImplementedException();
        }

        public void Add(int item)
        {
            throw new NotImplementedException();
        }

        void ICollection<int>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<int>.Contains(int item)
        {
            throw new NotImplementedException();
        }

        void ICollection<int>.CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        int IList<int>.IndexOf(int item)
        {
            throw new NotImplementedException();
        }

        void IList<int>.Insert(int index, int item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<int>.Remove(int item)
        {
            throw new NotImplementedException();
        }

        void IList<int>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class Class2 : IList<int>
    {
        public int this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void {|Definition:Add|}(int item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(int item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<int> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(int item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, int item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(int item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public sealed class Test
    {
        public void Foo()
        {
            Class1 c1 = new Class1();
            Class1 c2 = new Class1();
            Class2 c3 = new Class2();
            c2.Add(3);
            c1.Add(3);
            c3.[|Add|](3);
            (c1 as IList<int>).[|$$Add|](3);

        }
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestWinmdVBAllIsWellTest() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferencesWinRT="true">
        <CompilationOptions OutputType="WindowsRuntimeMetadata"/>
        <Document>
Imports System
Imports System.Collections
Imports System.Collections.Generic
Public Interface I
    Sub Add(ByVal item As Integer)
End Interface

Public NotInheritable Class Class1
    Implements I, IList(Of Integer)

    Public ReadOnly Property Count As Integer Implements ICollection(Of Integer).Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of Integer).IsReadOnly
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Default Public Property Item(index As Integer) As Integer Implements IList(Of Integer).Item
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As Integer)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Sub Add(item As Integer) Implements I.Add
        Throw New NotImplementedException()
    End Sub

    Public Sub Clear() Implements ICollection(Of Integer).Clear
        Throw New NotImplementedException()
    End Sub

    Public Sub CopyTo(array() As Integer, arrayIndex As Integer) Implements ICollection(Of Integer).CopyTo
        Throw New NotImplementedException()
    End Sub

    Public Sub Insert(index As Integer, item As Integer) Implements IList(Of Integer).Insert
        Throw New NotImplementedException()
    End Sub

    Public Sub RemoveAt(index As Integer) Implements IList(Of Integer).RemoveAt
        Throw New NotImplementedException()
    End Sub

    Private Sub {|Definition:ICollection_Add|}(item As Integer) Implements ICollection(Of Integer).[|Add|]
        Throw New NotImplementedException()
    End Sub

    Public Function Contains(item As Integer) As Boolean Implements ICollection(Of Integer).Contains
        Throw New NotImplementedException()
    End Function

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Function IndexOf(item As Integer) As Integer Implements IList(Of Integer).IndexOf
        Throw New NotImplementedException()
    End Function

    Public Function Remove(item As Integer) As Boolean Implements ICollection(Of Integer).Remove
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Public NotInheritable Class Class2
    Implements IList(Of Integer)

    Public ReadOnly Property Count As Integer Implements ICollection(Of Integer).Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of Integer).IsReadOnly
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Default Public Property Item(index As Integer) As Integer Implements IList(Of Integer).Item
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As Integer)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Sub {|Definition:Add|}(item As Integer) Implements ICollection(Of Integer).[|Add|]
        Throw New NotImplementedException()
    End Sub

    Public Sub Clear() Implements ICollection(Of Integer).Clear
        Throw New NotImplementedException()
    End Sub

    Public Sub CopyTo(array() As Integer, arrayIndex As Integer) Implements ICollection(Of Integer).CopyTo
        Throw New NotImplementedException()
    End Sub

    Public Sub Insert(index As Integer, item As Integer) Implements IList(Of Integer).Insert
        Throw New NotImplementedException()
    End Sub

    Public Sub RemoveAt(index As Integer) Implements IList(Of Integer).RemoveAt
        Throw New NotImplementedException()
    End Sub

    Public Function Contains(item As Integer) As Boolean Implements ICollection(Of Integer).Contains
        Throw New NotImplementedException()
    End Function

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Function IndexOf(item As Integer) As Integer Implements IList(Of Integer).IndexOf
        Throw New NotImplementedException()
    End Function

    Public Function Remove(item As Integer) As Boolean Implements ICollection(Of Integer).Remove
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Public NotInheritable Class Test
    Public Sub Foo()
        Dim c1 = DirectCast(New Class1(), IList(Of Integer))
        Dim c2 As New Class1
        Dim c3 As New Class2
        c2.Add(3)
        c1.[|Add|](3)
        c3.[|$$Add|](3)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
    End Class
End Namespace
