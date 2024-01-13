' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend NotInheritable Class AnonymousTypeManager

        Private Class AnonymousDelegateTemplateSymbol
            Inherits AnonymousTypeOrDelegateTemplateSymbol

            Protected ReadOnly TypeDescr As AnonymousTypeDescriptor
            Private ReadOnly _members As ImmutableArray(Of SynthesizedDelegateMethodSymbol)

            Friend Shared Function Create(manager As AnonymousTypeManager, typeDescr As AnonymousTypeDescriptor) As AnonymousDelegateTemplateSymbol
                Dim parameters = typeDescr.Parameters
                Return If(parameters.Length = 1 AndAlso parameters.IsSubDescription(),
                    New NonGenericAnonymousDelegateSymbol(manager, typeDescr),
                    New AnonymousDelegateTemplateSymbol(manager, typeDescr))
            End Function

            Public Sub New(manager As AnonymousTypeManager,
                           typeDescr As AnonymousTypeDescriptor)
                MyBase.New(manager, typeDescr)

                Debug.Assert(typeDescr.Parameters.Length > 1 OrElse
                             Not typeDescr.Parameters.IsSubDescription() OrElse
                             TypeOf Me Is NonGenericAnonymousDelegateSymbol)

                Me.TypeDescr = typeDescr

                Dim parameterDescriptors As ImmutableArray(Of AnonymousTypeField) = typeDescr.Parameters
                Dim returnType As TypeSymbol = If(parameterDescriptors.IsSubDescription(), DirectCast(manager.System_Void, TypeSymbol), Me.TypeParameters.Last)
                Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance(parameterDescriptors.Length + 1)
                Dim i As Integer

                ' A delegate has the following members: (see CLI spec 13.6)
                ' (1) a method named Invoke with the specified signature
                Dim delegateInvoke = New SynthesizedDelegateMethodSymbol(WellKnownMemberNames.DelegateInvokeName,
                                                                         Me,
                                                                         SourceNamedTypeSymbol.DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindDelegateInvoke,
                                                                         returnType)

                For i = 0 To parameterDescriptors.Length - 2
                    parameters.Add(New AnonymousTypeOrDelegateParameterSymbol(delegateInvoke,
                                                                              Me.TypeParameters(i),
                                                                              i,
                                                                              parameterDescriptors(i).IsByRef,
                                                                              parameterDescriptors(i).Name,
                                                                              i))
                Next

                delegateInvoke.SetParameters(parameters.ToImmutable())
                parameters.Clear()

                ' (2) a constructor with argument types (object, System.IntPtr)
                Dim delegateCtor = New SynthesizedDelegateMethodSymbol(WellKnownMemberNames.InstanceConstructorName,
                                                                       Me,
                                                                       SourceNamedTypeSymbol.DelegateConstructorMethodFlags,
                                                                       manager.System_Void)

                delegateCtor.SetParameters(
                    ImmutableArray.Create(Of ParameterSymbol)(
                           New AnonymousTypeOrDelegateParameterSymbol(delegateCtor, manager.System_Object, 0, False, StringConstants.DelegateConstructorInstanceParameterName),
                           New AnonymousTypeOrDelegateParameterSymbol(delegateCtor, manager.System_IntPtr, 1, False, StringConstants.DelegateConstructorMethodParameterName)
                           ))

                Dim delegateBeginInvoke As SynthesizedDelegateMethodSymbol
                Dim delegateEndInvoke As SynthesizedDelegateMethodSymbol

                ' Don't add Begin/EndInvoke members to winmd compilations.
                ' Invoke must be the last member, regardless.
                If Me.IsCompilationOutputWinMdObj() Then
                    delegateBeginInvoke = Nothing
                    delegateEndInvoke = Nothing
                    _members = ImmutableArray.Create(delegateCtor, delegateInvoke)
                Else
                    ' (3) BeginInvoke
                    delegateBeginInvoke = New SynthesizedDelegateMethodSymbol(WellKnownMemberNames.DelegateBeginInvokeName,
                                                                                  Me,
                                                                                  SourceNamedTypeSymbol.DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindOrdinary,
                                                                                  manager.System_IAsyncResult)

                    For i = 0 To delegateInvoke.ParameterCount - 1
                        Dim parameter As ParameterSymbol = delegateInvoke.Parameters(i)
                        parameters.Add(New AnonymousTypeOrDelegateParameterSymbol(delegateBeginInvoke, parameter.Type, i, parameter.IsByRef(), parameter.Name, i))
                    Next

                    parameters.Add(New AnonymousTypeOrDelegateParameterSymbol(delegateBeginInvoke, manager.System_AsyncCallback, i, False, StringConstants.DelegateMethodCallbackParameterName))
                    i += 1
                    parameters.Add(New AnonymousTypeOrDelegateParameterSymbol(delegateBeginInvoke, manager.System_Object, i, False, StringConstants.DelegateMethodInstanceParameterName))
                    delegateBeginInvoke.SetParameters(parameters.ToImmutable())
                    parameters.Clear()

                    ' and (4) EndInvoke methods
                    delegateEndInvoke = New SynthesizedDelegateMethodSymbol(WellKnownMemberNames.DelegateEndInvokeName,
                                                                                Me,
                                                                                SourceNamedTypeSymbol.DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindOrdinary,
                                                                                returnType)
                    Dim ordinal As Integer = 0
                    For i = 0 To delegateInvoke.ParameterCount - 1
                        Dim parameter As ParameterSymbol = delegateInvoke.Parameters(i)

                        If parameter.IsByRef Then
                            parameters.Add(New AnonymousTypeOrDelegateParameterSymbol(delegateEndInvoke, parameter.Type, ordinal, parameter.IsByRef(), parameter.Name, i))
                            ordinal += 1
                        End If
                    Next

                    parameters.Add(New AnonymousTypeOrDelegateParameterSymbol(delegateEndInvoke, manager.System_IAsyncResult, ordinal, False, StringConstants.DelegateMethodResultParameterName))
                    delegateEndInvoke.SetParameters(parameters.ToImmutable())

                    _members = ImmutableArray.Create(delegateCtor, delegateBeginInvoke, delegateEndInvoke, delegateInvoke)
                End If

                Debug.Assert(_members.All(Function(m) m IsNot Nothing))
                parameters.Free()
            End Sub

            Friend Overrides Function GetAnonymousTypeKey() As AnonymousTypeKey
                Dim parameters = TypeDescr.Parameters.SelectAsArray(Function(p) New AnonymousTypeKeyField(p.Name, isKey:=p.IsByRef, ignoreCase:=True))
                Return New AnonymousTypeKey(parameters, isDelegate:=True)
            End Function

            Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
                Return StaticCast(Of Symbol).From(_members)
            End Function

            Friend NotOverridable Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
                Return SpecializedCollections.EmptyEnumerable(Of FieldSymbol)()
            End Function

            Friend Overrides ReadOnly Property GeneratedNamePrefix As String
                Get
                    Return GeneratedNameConstants.AnonymousDelegateTemplateNamePrefix
                End Get
            End Property

            Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
                Return Manager.System_MulticastDelegate
            End Function

            Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides ReadOnly Property DelegateInvokeMethod As MethodSymbol
                Get
                    ' The invoke method is always the last method, in regular or winmd scenarios
                    Return _members(_members.Length - 1)
                End Get
            End Property

            Public Overrides ReadOnly Property TypeKind As TypeKind
                Get
                    Return TypeKind.Delegate
                End Get
            End Property

            Friend Overrides ReadOnly Property IsInterface As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

                ' Attribute: System.Runtime.CompilerServices.CompilerGeneratedAttribute()
                AddSynthesizedAttribute(attributes, Manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

                ' Attribute: System.Diagnostics.DebuggerDisplayAttribute("<generated method>",Type := "<generated method>")
                Dim value As New TypedConstant(Manager.System_String, TypedConstantKind.Primitive, "<generated method>")
                AddSynthesizedAttribute(attributes, Manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor,
                    ImmutableArray.Create(value),
                    ImmutableArray.Create(New KeyValuePair(Of WellKnownMember, TypedConstant)(
                        WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__Type, value))))
            End Sub
        End Class

        ''' <summary>
        ''' This is a symbol to represent Anonymous Delegate for a lambda
        ''' like:
        '''        Sub() ...
        ''' 
        ''' This delegate type doesn't have generic parameters. Unlike generic anonymous types,
        ''' for which we are constructing new instance of substituted symbol for each use site 
        ''' with reference to the location, we are creating new instance of this symbol with its
        ''' own location for each use site. But all of them are representing the same delegate 
        ''' type and are going to be equal to each other. 
        ''' </summary>
        Private NotInheritable Class NonGenericAnonymousDelegateSymbol
            Inherits AnonymousDelegateTemplateSymbol

            Public Sub New(manager As AnonymousTypeManager,
                           typeDescr As AnonymousTypeDescriptor)
                MyBase.New(manager, typeDescr)
                Debug.Assert(typeDescr.Parameters.Length = 1)
                Debug.Assert(typeDescr.Parameters.IsSubDescription())
            End Sub

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray.Create(TypeDescr.Location)
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return Manager.GetHashCode()
            End Function

            Public Overrides Function Equals(obj As TypeSymbol, comparison As TypeCompareKind) As Boolean
                If obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, NonGenericAnonymousDelegateSymbol)

                Return other IsNot Nothing AndAlso other.Manager Is Me.Manager
            End Function
        End Class

    End Class
End Namespace
