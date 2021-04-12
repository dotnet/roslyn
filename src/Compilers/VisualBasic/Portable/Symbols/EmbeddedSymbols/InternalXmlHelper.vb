' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

' See Compiler::LoadXmlSolutionExtension
Namespace My
    <Global.Microsoft.VisualBasic.Embedded()>
    <Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(), _
     Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
    Friend NotInheritable Class InternalXmlHelper
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Private Sub New()
        End Sub
        Public Shared Property Value(source As Global.System.Collections.Generic.IEnumerable(Of Global.System.Xml.Linq.XElement)) As String
            Get
                For Each item As Global.System.Xml.Linq.XElement In source
                    Return item.Value
                Next
                Return Nothing
            End Get
            Set(value As String)
                For Each item As Global.System.Xml.Linq.XElement In source
                    item.Value = value
                    Exit For
                Next
            End Set
        End Property
        Public Shared Property AttributeValue(source As Global.System.Collections.Generic.IEnumerable(Of Global.System.Xml.Linq.XElement), name As Global.System.Xml.Linq.XName) As String
            Get
                For Each item As Global.System.Xml.Linq.XElement In source
                    Return CType(item.Attribute(name), String)
                Next
                Return Nothing
            End Get
            Set(value As String)
                For Each item As Global.System.Xml.Linq.XElement In source
                    item.SetAttributeValue(name, value)
                    Exit For
                Next
            End Set
        End Property
        Public Shared Property AttributeValue(source As Global.System.Xml.Linq.XElement, name As Global.System.Xml.Linq.XName) As String
            Get
                Return CType(source.Attribute(name), String)
            End Get
            Set(value As String)
                source.SetAttributeValue(name, value)
            End Set
        End Property
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Public Shared Function CreateAttribute(name As Global.System.Xml.Linq.XName, value As Object) As Global.System.Xml.Linq.XAttribute
            If value Is Nothing Then
                Return Nothing
            End If
            Return New Global.System.Xml.Linq.XAttribute(name, value)
        End Function
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Public Shared Function CreateNamespaceAttribute(name As Global.System.Xml.Linq.XName, ns As Global.System.Xml.Linq.XNamespace) As Global.System.Xml.Linq.XAttribute
            Dim a As New Global.System.Xml.Linq.XAttribute(name, ns.NamespaceName)
            a.AddAnnotation(ns)
            Return a
        End Function
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Public Shared Function RemoveNamespaceAttributes(inScopePrefixes() As String, inScopeNs() As Global.System.Xml.Linq.XNamespace, attributes As Global.System.Collections.Generic.List(Of Global.System.Xml.Linq.XAttribute), obj As Object) As Object
            If obj IsNot Nothing Then
                Dim elem As Global.System.Xml.Linq.XElement = TryCast(obj, Global.System.Xml.Linq.XElement)
                If Not elem Is Nothing Then
                    Return RemoveNamespaceAttributes(inScopePrefixes, inScopeNs, attributes, elem)
                Else
                    Dim elems As Global.System.Collections.IEnumerable = TryCast(obj, Global.System.Collections.IEnumerable)
                    If elems IsNot Nothing Then
                        Return RemoveNamespaceAttributes(inScopePrefixes, inScopeNs, attributes, elems)
                    End If
                End If
            End If
            Return obj
        End Function
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Public Shared Function RemoveNamespaceAttributes(inScopePrefixes() As String, inScopeNs() As Global.System.Xml.Linq.XNamespace, attributes As Global.System.Collections.Generic.List(Of Global.System.Xml.Linq.XAttribute), obj As Global.System.Collections.IEnumerable) As Global.System.Collections.IEnumerable
            If obj IsNot Nothing Then
                Dim elems As Global.System.Collections.Generic.IEnumerable(Of Global.System.Xml.Linq.XElement) = TryCast(obj, Global.System.Collections.Generic.IEnumerable(Of Global.System.Xml.Linq.XElement))
                If elems IsNot Nothing Then
                    Return Global.System.Linq.Enumerable.Select(elems, AddressOf New RemoveNamespaceAttributesClosure(inScopePrefixes, inScopeNs, attributes).ProcessXElement)
                Else
                    Return Global.System.Linq.Enumerable.Select(Global.System.Linq.Enumerable.Cast(Of Object)(obj), AddressOf New RemoveNamespaceAttributesClosure(inScopePrefixes, inScopeNs, attributes).ProcessObject)
                End If
            End If
            Return obj
        End Function
        <Global.System.Diagnostics.DebuggerNonUserCodeAttribute()> _
        <Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()> _
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Private NotInheritable Class RemoveNamespaceAttributesClosure
            Private ReadOnly m_inScopePrefixes As String()
            Private ReadOnly m_inScopeNs As Global.System.Xml.Linq.XNamespace()
            Private ReadOnly m_attributes As Global.System.Collections.Generic.List(Of Global.System.Xml.Linq.XAttribute)
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Friend Sub New(inScopePrefixes() As String, inScopeNs() As Global.System.Xml.Linq.XNamespace, attributes As Global.System.Collections.Generic.List(Of Global.System.Xml.Linq.XAttribute))
                m_inScopePrefixes = inScopePrefixes
                m_inScopeNs = inScopeNs
                m_attributes = attributes
            End Sub
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Friend Function ProcessXElement(elem As Global.System.Xml.Linq.XElement) As Global.System.Xml.Linq.XElement
                Return InternalXmlHelper.RemoveNamespaceAttributes(m_inScopePrefixes, m_inScopeNs, m_attributes, elem)
            End Function
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Friend Function ProcessObject(obj As Object) As Object
                Dim elem As Global.System.Xml.Linq.XElement = TryCast(obj, Global.System.Xml.Linq.XElement)
                If elem IsNot Nothing Then
                    Return InternalXmlHelper.RemoveNamespaceAttributes(m_inScopePrefixes, m_inScopeNs, m_attributes, elem)
                Else
                    Return obj
                End If
            End Function
        End Class
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Public Shared Function RemoveNamespaceAttributes(inScopePrefixes() As String, inScopeNs() As Global.System.Xml.Linq.XNamespace, attributes As Global.System.Collections.Generic.List(Of Global.System.Xml.Linq.XAttribute), e As Global.System.Xml.Linq.XElement) As Global.System.Xml.Linq.XElement
            If e IsNot Nothing Then
                Dim a As Global.System.Xml.Linq.XAttribute = e.FirstAttribute

                While a IsNot Nothing
                    Dim nextA As Global.System.Xml.Linq.XAttribute = a.NextAttribute

                    If a.IsNamespaceDeclaration() Then
                        Dim ns As Global.System.Xml.Linq.XNamespace = a.Annotation(Of Global.System.Xml.Linq.XNamespace)()
                        Dim prefix As String = a.Name.LocalName

                        If ns IsNot Nothing Then
                            If inScopePrefixes IsNot Nothing AndAlso inScopeNs IsNot Nothing Then
                                Dim lastIndex As Integer = inScopePrefixes.Length - 1

                                For i As Integer = 0 To lastIndex
                                    Dim currentInScopePrefix As String = inScopePrefixes(i)
                                    Dim currentInScopeNs As Global.System.Xml.Linq.XNamespace = inScopeNs(i)
                                    If prefix.Equals(currentInScopePrefix) Then
                                        If ns = currentInScopeNs Then
                                            'prefix and namespace match.  Remove the unneeded ns attribute 
                                            a.Remove()
                                        End If

                                        'prefix is in scope but refers to something else.  Leave the ns attribute. 
                                        a = Nothing
                                        Exit For
                                    End If
                                Next
                            End If

                            If a IsNot Nothing Then
                                'Prefix is not in scope 
                                'Now check whether it's going to be in scope because it is in the attributes list 

                                If attributes IsNot Nothing Then
                                    Dim lastIndex As Integer = attributes.Count - 1
                                    For i As Integer = 0 To lastIndex
                                        Dim currentA As Global.System.Xml.Linq.XAttribute = attributes(i)
                                        Dim currentInScopePrefix As String = currentA.Name.LocalName
                                        Dim currentInScopeNs As Global.System.Xml.Linq.XNamespace = currentA.Annotation(Of Global.System.Xml.Linq.XNamespace)()
                                        If currentInScopeNs IsNot Nothing Then
                                            If prefix.Equals(currentInScopePrefix) Then
                                                If ns = currentInScopeNs Then
                                                    'prefix and namespace match.  Remove the unneeded ns attribute 
                                                    a.Remove()
                                                End If

                                                'prefix is in scope but refers to something else.  Leave the ns attribute. 
                                                a = Nothing
                                                Exit For
                                            End If
                                        End If
                                    Next
                                End If

                                If a IsNot Nothing Then
                                    'Prefix is definitely not in scope  
                                    a.Remove()
                                    'namespace is not defined either.  Add this attributes list 
                                    attributes.Add(a)
                                End If
                            End If
                        End If
                    End If

                    a = nextA
                End While
            End If
            Return e
        End Function

    End Class
End Namespace
