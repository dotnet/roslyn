' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a declare method defined in source.
    ''' </summary>
    Friend NotInheritable Class SourceDeclareMethodSymbol
        Inherits SourceNonPropertyAccessorMethodSymbol

        Private ReadOnly _name As String
        Private _lazyMetadataName As String

        ' Flags indicates results of quick scan of the attributes
        Private ReadOnly _quickAttributes As QuickAttributes

        ' Platform Invoke information for Declare Method
        Private ReadOnly _platformInvokeInfo As DllImportData

        Public Sub New(container As SourceMemberContainerTypeSymbol,
                       name As String,
                       flags As SourceMemberFlags,
                       binder As Binder,
                       syntax As MethodBaseSyntax,
                       platformInvokeInfo As DllImportData)
            MyBase.New(container, flags, binder.GetSyntaxReference(syntax))

            Debug.Assert(MyBase.MethodKind = MethodKind.DeclareMethod)
            Debug.Assert(platformInvokeInfo IsNot Nothing)

            _platformInvokeInfo = platformInvokeInfo
            _name = name

            ' Check attributes quickly.
            _quickAttributes = binder.QuickAttributeChecker.CheckAttributes(syntax.AttributeLists)
            If ContainingType.TypeKind <> TypeKind.Module Then
                ' Extension methods in source can only be inside modules.
                _quickAttributes = _quickAttributes And Not QuickAttributes.Extension
            End If
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                If _lazyMetadataName Is Nothing Then
                    OverloadingHelper.SetMetadataNameForAllOverloads(_name, SymbolKind.Method, m_containingType)
                    Debug.Assert(_lazyMetadataName IsNot Nothing)
                End If

                Return _lazyMetadataName
            End Get
        End Property

        Friend Overrides Sub SetMetadataName(metadataName As String)
            Dim old = Interlocked.CompareExchange(_lazyMetadataName, metadataName, Nothing)
            Debug.Assert(old Is Nothing OrElse old = metadataName)
        End Sub

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return (_quickAttributes And QuickAttributes.Extension) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                If MayBeReducibleExtensionMethod Then
                    Return MyBase.IsExtensionMethod
                Else
                    Return False
                End If
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                If (_quickAttributes And QuickAttributes.Obsolete) <> 0 Then
                    Return MyBase.ObsoleteAttributeData
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return Me._platformInvokeInfo
        End Function

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return Reflection.MethodImplAttributes.PreserveSig
            End Get
        End Property
    End Class
End Namespace
