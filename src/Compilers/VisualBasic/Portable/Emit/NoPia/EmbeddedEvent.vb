' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

#If Not DEBUG Then
Imports EventSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.EventSymbol
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedEvent
        Inherits EmbeddedTypesManager.CommonEmbeddedEvent

        Public Sub New(underlyingEvent As EventSymbolAdapter, adder As EmbeddedMethod, remover As EmbeddedMethod, caller As EmbeddedMethod)
            MyBase.New(underlyingEvent, adder, remover, caller)
        End Sub

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingEvent.AdaptedEventSymbol.GetCustomAttributesToEmit(moduleBuilder)
        End Function

        Protected Overrides ReadOnly Property IsRuntimeSpecial As Boolean
            Get
                Return UnderlyingEvent.AdaptedEventSymbol.HasRuntimeSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingEvent.AdaptedEventSymbol.HasSpecialName
            End Get
        End Property

        Protected Overrides Function [GetType](moduleBuilder As PEModuleBuilder, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Cci.ITypeReference
            Return moduleBuilder.Translate(UnderlyingEvent.AdaptedEventSymbol.Type, syntaxNodeOpt, diagnostics)
        End Function

        Protected Overrides ReadOnly Property ContainingType As EmbeddedType
            Get
                Return AnAccessor.ContainingType
            End Get
        End Property

        Protected Overrides ReadOnly Property Visibility As Cci.TypeMemberVisibility
            Get
                Return UnderlyingEvent.AdaptedEventSymbol.MetadataVisibility
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingEvent.AdaptedEventSymbol.MetadataName
            End Get
        End Property

        Protected Overrides Sub EmbedCorrespondingComEventInterfaceMethodInternal(syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag, isUsedForComAwareEventBinding As Boolean)
            ' If the event happens to belong to a class with a ComEventInterfaceAttribute, there will also be
            ' a paired method living on its source interface. The ComAwareEventInfo class expects to find this 
            ' method through reflection. If we embed an event, therefore, we must ensure that the associated source
            ' interface method is also included, even if it is not otherwise referenced in the embedding project.
            Dim underlyingContainingType = ContainingType.UnderlyingNamedType

            For Each attrData In underlyingContainingType.AdaptedNamedTypeSymbol.GetAttributes()
                Dim signatureIndex As Integer = attrData.GetTargetAttributeSignatureIndex(AttributeDescription.ComEventInterfaceAttribute)

                If signatureIndex = 0 Then
                    Dim foundMatch = False
                    Dim sourceInterface As NamedTypeSymbol = Nothing

                    Dim errorInfo As DiagnosticInfo = attrData.ErrorInfo
                    If errorInfo IsNot Nothing Then
                        diagnostics.Add(errorInfo, If(syntaxNodeOpt?.Location, NoLocation.Singleton))
                    End If

                    If Not attrData.HasErrors Then
                        sourceInterface = TryCast(attrData.CommonConstructorArguments(0).ValueInternal, NamedTypeSymbol)
                        If sourceInterface IsNot Nothing Then
                            foundMatch = EmbedMatchingInterfaceMethods(sourceInterface, syntaxNodeOpt, diagnostics)

                            For Each source In sourceInterface.AllInterfacesNoUseSiteDiagnostics
                                If EmbedMatchingInterfaceMethods(source, syntaxNodeOpt, diagnostics) Then
                                    foundMatch = True
                                End If
                            Next
                        End If
                    End If

                    If Not foundMatch AndAlso isUsedForComAwareEventBinding Then
                        If sourceInterface Is Nothing Then
                            ' ERRID_SourceInterfaceMustBeInterface/ERR_MissingSourceInterface
                            EmbeddedTypesManager.ReportDiagnostic(diagnostics, ERRID.ERR_SourceInterfaceMustBeInterface, syntaxNodeOpt, underlyingContainingType.AdaptedNamedTypeSymbol, UnderlyingEvent.AdaptedEventSymbol)
                        Else
                            Dim useSiteInfo = CompoundUseSiteInfo(Of AssemblySymbol).DiscardedDependencies
                            sourceInterface.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteInfo)
                            diagnostics.Add(If(syntaxNodeOpt Is Nothing, NoLocation.Singleton, syntaxNodeOpt.GetLocation()), useSiteInfo.Diagnostics)

                            ' ERRID_EventNoPIANoBackingMember/ERR_MissingMethodOnSourceInterface
                            EmbeddedTypesManager.ReportDiagnostic(diagnostics, ERRID.ERR_EventNoPIANoBackingMember, syntaxNodeOpt, sourceInterface, UnderlyingEvent.AdaptedEventSymbol.MetadataName, UnderlyingEvent.AdaptedEventSymbol)
                        End If
                    End If

                    Exit For
                End If
            Next
        End Sub

        Private Function EmbedMatchingInterfaceMethods(sourceInterface As NamedTypeSymbol, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Boolean
            Dim foundMatch = False
            For Each m In sourceInterface.GetMembers(UnderlyingEvent.AdaptedEventSymbol.MetadataName)
                If m.Kind = SymbolKind.Method Then
                    TypeManager.EmbedMethodIfNeedTo(DirectCast(m, MethodSymbol).GetCciAdapter(), syntaxNodeOpt, diagnostics)
                    foundMatch = True
                End If
            Next
            Return foundMatch
        End Function

    End Class

End Namespace
