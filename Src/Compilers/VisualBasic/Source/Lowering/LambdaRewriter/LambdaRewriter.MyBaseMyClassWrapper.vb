Imports System
Imports Roslyn.Compilers.Common

Namespace Roslyn.Compilers.VisualBasic
    Partial Class LambdaRewriter

        ''' <summary>
        ''' A method that wraps the call to a method through MyBase/MyClass receiver.
        ''' </summary>
        ''' <remarks>
        ''' <example>
        ''' Class A
        '''     Protected Overridable Sub F(a As Integer)
        '''     End Sub
        ''' End Class
        ''' 
        ''' Class B
        '''     Inherits A
        ''' 
        '''     Public Sub M()
        '''         Dim b As Integer = 1
        '''         Dim f As System.Action = Sub() MyBase.F(b)
        '''     End Sub
        ''' End Class
        ''' </example>
        ''' </remarks>
        Friend NotInheritable Class SynthesizedWrapperMethod
            Inherits SynthesizedMethod

            Private ReadOnly m_wrappedMethod As MethodSymbol
            Private ReadOnly m_typeMap As TypeSubstitution
            Private ReadOnly m_typeParameters As ReadOnlyArray(Of TypeParameterSymbol)
            Private ReadOnly m_parameters As ReadOnlyArray(Of ParameterSymbol)
            Private ReadOnly m_returnType As TypeSymbol
            Private ReadOnly m_locations As ReadOnlyArray(Of Location)

            ''' <summary>
            ''' Creates a symbol for a synthesized lambda method.
            ''' </summary>
            ''' <param name="containingType">Type that contains wrapper method.</param>
            ''' <param name="methodToWrap">Method to wrap</param>
            ''' <param name="wrapperName">Wrapper method name</param>
            ''' <param name="syntax">Syntax node.</param>
            Friend Sub New(compilation As Compilation,
                           containingType As InstanceTypeSymbol,
                           methodToWrap As MethodSymbol,
                           wrapperName As String,
                           syntax As SyntaxNode,
                           diagnostics As DiagnosticBag)

                MyBase.New(syntax, containingType, wrapperName, False)

                Me.m_locations = ReadOnlyArray.Singleton(Of Location)(syntax.GetLocation())

                Me.m_typeMap = Nothing

                If Not methodToWrap.IsGenericMethod Then
                    Me.m_typeParameters = ReadOnlyArray(Of TypeParameterSymbol).Empty
                    Me.m_wrappedMethod = methodToWrap
                Else
                    Me.m_typeParameters = MappedTypeParameterSymbol.MakeTypeParameters(methodToWrap.OriginalMethodDefinition.TypeParameters, Me)

                    Dim typeArgs(Me.m_typeParameters.Count - 1) As TypeSymbol
                    For ind = 0 To Me.m_typeParameters.Count - 1
                        typeArgs(ind) = Me.m_typeParameters(ind)
                    Next

                    Dim newConstructedWrappedMethod As MethodSymbol = methodToWrap.Construct(typeArgs.AsReadOnlyWrap())

                    Me.m_typeMap = TypeSubstitution.Create(newConstructedWrappedMethod.OriginalMethodDefinition,
                                                           newConstructedWrappedMethod.OriginalMethodDefinition.TypeParameters,
                                                           typeArgs.AsReadOnlyWrap())

                    Me.m_wrappedMethod = newConstructedWrappedMethod
                End If

                Dim params(Me.m_wrappedMethod.Parameters.Count - 1) As ParameterSymbol
                For i = 0 To params.Count - 1
                    Dim curParam = Me.m_wrappedMethod.Parameters(i)
                    params(i) = SynthesizedLambdaMethod.WithNewContainerAndType(Me,
                                    If(Me.m_typeMap Is Nothing, curParam.Type, curParam.Type.InternalSubstituteTypeParameters(Me.m_typeMap)), curParam)
                Next
                Me.m_parameters = params.AsReadOnlyWrap()

                Me.m_returnType = If(Me.m_typeMap Is Nothing, Me.m_wrappedMethod.ReturnType,
                                     Me.m_wrappedMethod.ReturnType.InternalSubstituteTypeParameters(Me.m_typeMap))
            End Sub

            Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
                Get
                    Return Me.m_typeMap
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(attributes)

                Dim compilation = DirectCast(Me.ContainingAssembly, SourceAssemblySymbol).Compilation

                AddSynthesizedAttribute(attributes, compilation.SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

                ' Dev11 emits DebuggerNonUserCode. We emit DebuggerHidden to hide the method even if JustMyCode is off.
                AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
            End Sub

            Public ReadOnly Property WrappedMethod As MethodSymbol
                Get
                    Return Me.m_wrappedMethod
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ReadOnlyArray(Of TypeParameterSymbol)
                Get
                    Return m_typeParameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeArguments As ReadOnlyArray(Of TypeSymbol)
                Get
                    ' This is always a method definition, so the type arguments are the same as the type parameters.
                    If Arity > 0 Then
                        Return ReadOnlyArray(Of TypeSymbol).CreateFrom(Me.TypeParameters)
                    Else
                        Return ReadOnlyArray(Of TypeSymbol).Empty
                    End If
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ReadOnlyArray(Of Location)
                Get
                    Return m_locations
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ReadOnlyArray(Of ParameterSymbol)
                Get
                    Return m_parameters
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return m_returnType
                End Get
            End Property

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsSub As Boolean
                Get
                    Return Me.m_wrappedMethod.IsSub
                End Get
            End Property

            Public Overrides ReadOnly Property IsVararg As Boolean
                Get
                    Return Me.m_wrappedMethod.IsVararg
                End Get

            End Property

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return m_typeParameters.Count
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return Accessibility.Private
                End Get
            End Property

            Friend Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return Me.m_parameters.Count
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class
    End Class
End Namespace

