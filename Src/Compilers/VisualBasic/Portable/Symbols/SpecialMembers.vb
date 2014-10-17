' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Class AssemblySymbol

        ''' <summary>
        ''' Lookup member declaration in predefined CorLib type used by this Assembly.
        ''' </summary>
        Friend Function GetSpecialTypeMember(member As SpecialMember) As Symbol
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

    Partial Class MetadataOrSourceAssemblySymbol

        ''' <summary>
        ''' Lazy cache of special members.
        ''' Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        ''' </summary>
        Private m_LazySpecialTypeMembers() As Symbol

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

            If m_LazySpecialTypeMembers Is Nothing OrElse m_LazySpecialTypeMembers(member) Is ErrorTypeSymbol.UnknownResultType Then
                If (m_LazySpecialTypeMembers Is Nothing) Then
                    Dim specialTypeMembers = New Symbol(SpecialMember.Count - 1) {}

                    For i As Integer = 0 To specialTypeMembers.Length - 1
                        specialTypeMembers(i) = ErrorTypeSymbol.UnknownResultType
                    Next

                    Interlocked.CompareExchange(m_LazySpecialTypeMembers, specialTypeMembers, Nothing)
                End If

                Dim descriptor = SpecialMembers.GetDescriptor(member)
                Dim type = GetDeclaredSpecialType(CType(descriptor.DeclaringTypeId, SpecialType))
                Dim result As Symbol = Nothing

                If Not type.IsErrorType() Then
                    result = VisualBasicCompilation.GetRuntimeMember(type, descriptor, VisualBasicCompilation.SpecialMembersSignatureComparer.Instance, accessWithinOpt:=Nothing)
                End If

                Interlocked.CompareExchange(m_LazySpecialTypeMembers(member), result, DirectCast(ErrorTypeSymbol.UnknownResultType, Symbol))
            End If

            Return m_LazySpecialTypeMembers(member)
        End Function

    End Class

End Namespace