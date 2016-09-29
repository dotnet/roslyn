' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis

Friend Module ObjectInfoHelper
    'Helpers that use Reflection to return details pertaining to an object including
    'its type + the types, names and values of properties on this object.

#Region "GetObjectInfo"
    Friend Function GetObjectInfo(nodeOrToken As SyntaxNodeOrToken) As ObjectInfo
        Dim info As ObjectInfo = Nothing

        If nodeOrToken.IsNode Then
            info = GetObjectInfo(nodeOrToken.AsNode)
        Else
            info = GetObjectInfo(nodeOrToken.AsToken)
        End If

        Return info
    End Function

    Friend Function GetObjectInfo(node As SyntaxNode) As ObjectInfo
        Dim type = node.GetType()

        Dim properties = type.GetProperties(System.Reflection.BindingFlags.Instance Or
                                            System.Reflection.BindingFlags.Public)
        Dim propertyInfos = (From p In properties Where IsSimpleProperty(p)
                             Select GetPropertyInfo(p, node)).ToList()

        Return New ObjectInfo(type.Name, propertyInfos)
    End Function

    Friend Function GetObjectInfo(token As SyntaxToken) As ObjectInfo
        Dim type = token.GetType()

        Dim properties = type.GetProperties(System.Reflection.BindingFlags.Instance Or
                                            System.Reflection.BindingFlags.Public)
        Dim propertyInfos = (From p In properties Where IsSimpleProperty(p)
                             Select GetPropertyInfo(p, token)).ToList()

        Return New ObjectInfo(type.Name, propertyInfos)
    End Function

    Friend Function GetObjectInfo(trivia As SyntaxTrivia) As ObjectInfo
        Dim type = trivia.GetType()

        Dim properties = type.GetProperties(System.Reflection.BindingFlags.Instance Or
                                            System.Reflection.BindingFlags.Public)
        Dim propertyInfos = (From p In properties Where IsSimpleProperty(p)
                             Select GetPropertyInfo(p, trivia)).ToList()
        Return New ObjectInfo(type.Name, propertyInfos)
    End Function
#End Region

#Region "GetPropertyInfo"
    Private Function IsSimpleProperty(prop As System.Reflection.PropertyInfo) As Boolean
        Dim type = prop.PropertyType
        If type Is GetType(Char) OrElse
           type Is GetType(Boolean) OrElse
           type Is GetType(Short) OrElse
           type Is GetType(UShort) OrElse
           type Is GetType(Integer) OrElse
           type Is GetType(UInteger) OrElse
           type Is GetType(Long) OrElse
           type Is GetType(ULong) OrElse
           type Is GetType(Single) OrElse
           type Is GetType(Double) OrElse
           type Is GetType(Date) OrElse
           type Is GetType(Decimal) OrElse
           type Is GetType(String) OrElse
           type.IsEnum Then
            Return True
        Else
            Return False
        End If
    End Function

    'Only called if IsSimpleProperty returns true.
    Private Function GetPropertyInfo(prop As System.Reflection.PropertyInfo,
                                     node As SyntaxNode) As ObjectInfo.PropertyInfo
        Return New ObjectInfo.PropertyInfo(prop.Name, prop.PropertyType,
                                           prop.GetValue(node, Nothing))
    End Function

    'Only called if IsSimpleProperty returns true.
    Private Function GetPropertyInfo(prop As System.Reflection.PropertyInfo,
                                     token As SyntaxToken) As ObjectInfo.PropertyInfo
        Return New ObjectInfo.PropertyInfo(prop.Name, prop.PropertyType,
                                           prop.GetValue(token, Nothing))
    End Function

    'Only called if IsSimpleProperty returns true.
    Private Function GetPropertyInfo(prop As System.Reflection.PropertyInfo,
                                     trivia As SyntaxTrivia) As ObjectInfo.PropertyInfo
        Return New ObjectInfo.PropertyInfo(prop.Name, prop.PropertyType,
                                           prop.GetValue(trivia, Nothing))
    End Function
#End Region
End Module

'Encapsulates details pertaining to an object including its type + the types, names
'and values of properties on this object.
Friend Class ObjectInfo
    Private ReadOnly _typeName As String
    Private ReadOnly _propertyInfos As IEnumerable(Of PropertyInfo)
    Private Shared ReadOnly s_emptyPropertyInfos As IEnumerable(Of PropertyInfo) = Array.Empty(Of PropertyInfo)

    Friend ReadOnly Property TypeName As String
        Get
            Return _typeName
        End Get
    End Property

    Friend ReadOnly Property PropertyInfos As IEnumerable(Of PropertyInfo)
        Get
            If _propertyInfos Is Nothing Then
                Return s_emptyPropertyInfos
            Else
                Return _propertyInfos
            End If
        End Get
    End Property

    Friend Sub New(typeName As String, propertyInfos As IEnumerable(Of PropertyInfo))
        _typeName = typeName
        _propertyInfos = propertyInfos
    End Sub

    'Encapsulates the name, type and value of a property on an object.
    Friend Class PropertyInfo
        Private ReadOnly _name As String
        Private ReadOnly _type As Type
        Private ReadOnly _value As Object

        Friend ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend ReadOnly Property Type As Type
            Get
                Return _type
            End Get
        End Property

        Friend ReadOnly Property Value As Object
            Get
                Return _value
            End Get
        End Property

        Friend Sub New(name As String, type As Type, value As Object)
            _name = name
            _type = type
            _value = value
        End Sub
    End Class
End Class
