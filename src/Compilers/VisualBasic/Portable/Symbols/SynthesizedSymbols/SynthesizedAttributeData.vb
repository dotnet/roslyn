' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Class to represent a synthesized attribute
    ''' </summary>
    Friend NotInheritable Class SynthesizedAttributeData
        Inherits SourceAttributeData

        Friend Sub New(wellKnownMember As MethodSymbol,
                       arguments As ImmutableArray(Of TypedConstant),
                       namedArgs As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)))

            MyBase.New(Nothing,
                       wellKnownMember.ContainingType,
                       wellKnownMember,
                       arguments,
                       namedArgs,
                       isConditionallyOmitted:=False,
                       hasErrors:=False)

            Debug.Assert(wellKnownMember IsNot Nothing AndAlso Not arguments.IsDefault)
        End Sub

        ''' <summary>
        ''' Synthesizes attribute data for given constructor symbol.
        ''' If the constructor has UseSiteErrors and the attribute is optional returns Nothing.
        ''' </summary>
        Friend Shared Function Create(
            constructorSymbol As MethodSymbol,
            constructor As WellKnownMember,
            Optional arguments As ImmutableArray(Of TypedConstant) = Nothing,
            Optional namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)) = Nothing) As SynthesizedAttributeData

            ' If we reach here, unlikely a VBCore scenario.  Will result in ERR_MissingRuntimeHelper diagnostic            
            If Binder.GetUseSiteErrorForWellKnownTypeMember(constructorSymbol, constructor, False) IsNot Nothing Then
                If WellKnownMembers.IsSynthesizedAttributeOptional(constructor) Then
                    Return Nothing
                Else
                    'UseSiteErrors for member have not been checked before emitting
                    Throw ExceptionUtilities.Unreachable
                End If
            Else
                If arguments.IsDefault Then
                    arguments = ImmutableArray(Of TypedConstant).Empty
                End If

                If namedArguments.IsDefault Then
                    namedArguments = ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty
                End If

                Return New SynthesizedAttributeData(constructorSymbol, arguments, namedArguments)
            End If
        End Function
    End Class
End Namespace
