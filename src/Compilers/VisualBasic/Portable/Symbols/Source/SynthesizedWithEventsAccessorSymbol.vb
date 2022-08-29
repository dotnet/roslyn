' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' WithEvents property accessor.
    ''' </summary>
    Friend MustInherit Class SynthesizedWithEventsAccessorSymbol
        Inherits SynthesizedPropertyAccessorBase(Of PropertySymbol)

        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)
        Private _lazyExplicitImplementations As ImmutableArray(Of MethodSymbol) ' lazily populated with explicit implementations

        Protected Sub New(container As SourceMemberContainerTypeSymbol, [property] As PropertySymbol)
            MyBase.New(container, [property])
            Debug.Assert([property].IsWithEvents)
        End Sub

        Protected ReadOnly Property ContainingProperty As PropertySymbol
            Get
                Return m_propertyOrEvent
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If _lazyExplicitImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(
                        _lazyExplicitImplementations,
                        GetExplicitInterfaceImplementations())
                End If

                Return _lazyExplicitImplementations
            End Get
        End Property

        Private Function GetExplicitInterfaceImplementations() As ImmutableArray(Of MethodSymbol)
            Dim sourceProperty = TryCast(ContainingProperty, SourcePropertySymbol)
            If sourceProperty IsNot Nothing Then
                Return sourceProperty.GetAccessorImplementations(getter:=(MethodKind = MethodKind.PropertyGet))
            End If

            Return ImmutableArray(Of MethodSymbol).Empty
        End Function

        Public Overrides ReadOnly Property OverriddenMethod As MethodSymbol
            Get
                Return ContainingProperty.GetAccessorOverride(getter:=(MethodKind = MethodKind.PropertyGet))
            End Get
        End Property
        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Dim sourceProperty = TryCast(ContainingProperty, SourcePropertySymbol)
                If sourceProperty IsNot Nothing Then
                    Return sourceProperty.Syntax
                End If

                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property BackingFieldSymbol As FieldSymbol
            Get
                Dim sourceProperty = TryCast(ContainingProperty, SourcePropertySymbol)
                If sourceProperty IsNot Nothing Then
                    Return sourceProperty.AssociatedField
                End If

                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If _lazyParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(_lazyParameters, GetParameters())
                End If

                Return _lazyParameters
            End Get
        End Property

        Protected MustOverride Function GetParameters() As ImmutableArray(Of ParameterSymbol)

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            Debug.Assert(Not ContainingType.IsImplicitlyDeclared)
            Dim compilation = Me.DeclaringCompilation
            AddSynthesizedAttribute(attributes,
                                    compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

            ' Dev11 adds DebuggerNonUserCode; there is no reason to do so since:
            ' - we emit no debug info for the body
            ' - the code doesn't call any user code that could inspect the stack and find the accessor's frame
            ' - the code doesn't throw exceptions whose stack frames we would need to hide
        End Sub

        Friend NotOverridable Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class

    Friend NotInheritable Class SynthesizedWithEventsGetAccessorSymbol
        Inherits SynthesizedWithEventsAccessorSymbol

        Public Sub New(container As SourceMemberContainerTypeSymbol,
                       propertySymbol As PropertySymbol)

            MyBase.New(container, propertySymbol)
        End Sub

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.PropertyGet
            End Get
        End Property

        Protected Overrides Function GetParameters() As ImmutableArray(Of ParameterSymbol)
            Dim propertySymbol = ContainingProperty

            If propertySymbol.ParameterCount > 0 Then
                Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance(propertySymbol.ParameterCount)

                propertySymbol.CloneParameters(Me, parameters)
                Return parameters.ToImmutableAndFree()
            Else
                Return ImmutableArray(Of ParameterSymbol).Empty
            End If
        End Function

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return m_propertyOrEvent.Type
            End Get
        End Property
    End Class

    Friend NotInheritable Class SynthesizedWithEventsSetAccessorSymbol
        Inherits SynthesizedWithEventsAccessorSymbol

        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _valueParameterName As String

        Public Sub New(container As SourceMemberContainerTypeSymbol,
                       propertySymbol As PropertySymbol,
                       returnType As TypeSymbol,
                       valueParameterName As String)

            MyBase.New(container, propertySymbol)
            _returnType = returnType
            _valueParameterName = valueParameterName
        End Sub

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.PropertySet
            End Get
        End Property

        Protected Overrides Function GetParameters() As ImmutableArray(Of ParameterSymbol)
            Dim propertySymbol = ContainingProperty
            Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance(propertySymbol.ParameterCount + 1)

            propertySymbol.CloneParameters(Me, parameters)
            ' Add the "value" parameter
            parameters.Add(SynthesizedParameterSymbol.CreateSetAccessorValueParameter(Me, propertySymbol, _valueParameterName))
            Return parameters.ToImmutableAndFree()
        End Function

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Dim result = MyBase.ImplementationAttributes

                If DirectCast(Me.AssociatedSymbol, PropertySymbol).IsWithEvents Then
                    result = result Or Reflection.MethodImplAttributes.Synchronized
                End If

                Return result
            End Get
        End Property
    End Class
End Namespace
