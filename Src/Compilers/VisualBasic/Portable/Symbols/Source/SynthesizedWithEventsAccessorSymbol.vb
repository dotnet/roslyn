' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' WithEvents property accessor.
    ''' </summary>
    Friend MustInherit Class SynthesizedWithEventsAccessorSymbol
        Inherits SynthesizedPropertyAccessorBase(Of PropertySymbol)

        Private m_lazyParameters As ImmutableArray(Of ParameterSymbol)
        Private m_lazyExplicitImplementations As ImmutableArray(Of MethodSymbol) ' lazily populated with explicit implementations

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
                If m_lazyExplicitImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(
                        m_lazyExplicitImplementations,
                        GetExplicitInterfaceImplementations())
                End If

                Return m_lazyExplicitImplementations
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
        Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
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
                If m_lazyParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(m_lazyParameters, GetParameters())
                End If

                Return m_lazyParameters
            End Get
        End Property

        Protected MustOverride Function GetParameters() As ImmutableArray(Of ParameterSymbol)

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            Debug.Assert(Not ContainingType.IsImplicitlyDeclared)
            Dim compilation = Me.DeclaringCompilation
            AddSynthesizedAttribute(attributes,
                                    compilation.SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

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

        Private ReadOnly m_returnType As TypeSymbol
        Private ReadOnly m_valueParameterName As String

        Public Sub New(container As SourceMemberContainerTypeSymbol,
                       propertySymbol As PropertySymbol,
                       returnType As TypeSymbol,
                       valueParameterName As String)

            MyBase.New(container, propertySymbol)
            m_returnType = returnType
            m_valueParameterName = valueParameterName
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
            parameters.Add(SynthesizedParameterSymbol.CreateSetAccessorValueParameter(Me, propertySymbol, m_valueParameterName))
            Return parameters.ToImmutableAndFree()
        End Function

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return m_returnType
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