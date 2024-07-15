' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend Class AssemblySymbol

        ''' <summary>
        ''' Lookup member declaration in predefined CorLib type used by this Assembly.
        ''' </summary>
        Friend Overridable Function GetSpecialTypeMember(member As SpecialMember) As Symbol
            Debug.Assert(member >= 0 AndAlso member < SpecialMember.Count)

            Return CorLibrary.GetDeclaredSpecialTypeMember(member)
        End Function

        ''' <summary>
        ''' Lookup member declaration in predefined CorLib type in this Assembly. Only valid if this 
        ''' assembly is the Cor Library
        ''' </summary>
        Friend Overridable Function GetDeclaredSpecialTypeMember(member As SpecialMember) As Symbol
            Return Nothing
        End Function

    End Class

    Partial Friend Class MetadataOrSourceAssemblySymbol

        ''' <summary>
        ''' Lazy cache of special members.
        ''' Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        ''' </summary>
        Private _lazySpecialTypeMembers() As Symbol

        ''' <summary>
        ''' Lookup member declaration in predefined CorLib type in this Assembly. Only valid if this 
        ''' assembly is the Cor Library
        ''' </summary>
        Friend Overrides Function GetDeclaredSpecialTypeMember(member As SpecialMember) As Symbol
#If DEBUG Then
            For Each [module] In Me.Modules
                Debug.Assert([module].GetReferencedAssemblies().Length = 0)
            Next
#End If

            If _lazySpecialTypeMembers Is Nothing OrElse _lazySpecialTypeMembers(member) Is ErrorTypeSymbol.UnknownResultType Then
                If (_lazySpecialTypeMembers Is Nothing) Then
                    Dim specialTypeMembers = New Symbol(SpecialMember.Count - 1) {}

                    For i As Integer = 0 To specialTypeMembers.Length - 1
                        specialTypeMembers(i) = ErrorTypeSymbol.UnknownResultType
                    Next

                    Interlocked.CompareExchange(_lazySpecialTypeMembers, specialTypeMembers, Nothing)
                End If

                Dim descriptor = SpecialMembers.GetDescriptor(member)
                Dim type = GetDeclaredSpecialType(descriptor.DeclaringSpecialType)
                Dim result As Symbol = Nothing

                If Not type.IsErrorType() Then
                    result = VisualBasicCompilation.GetRuntimeMember(type, descriptor, VisualBasicCompilation.SpecialMembersSignatureComparer.Instance, accessWithinOpt:=Nothing)
                End If

                Interlocked.CompareExchange(_lazySpecialTypeMembers(member), result, DirectCast(ErrorTypeSymbol.UnknownResultType, Symbol))
            End If

            Return _lazySpecialTypeMembers(member)
        End Function

    End Class

End Namespace
