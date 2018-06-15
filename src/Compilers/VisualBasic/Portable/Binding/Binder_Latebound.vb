' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder

        Private Function BindLateBoundMemberAccess(node As SyntaxNode,
                                           name As String,
                                           typeArguments As TypeArgumentListSyntax,
                                           receiver As BoundExpression,
                                           containerType As TypeSymbol,
                                           diagnostics As DiagnosticBag) As BoundExpression

            Dim boundTypeArguments As BoundTypeArguments = BindTypeArguments(typeArguments, diagnostics)
            Return BindLateBoundMemberAccess(node, name, boundTypeArguments, receiver, containerType, diagnostics)
        End Function

        Private Function BindLateBoundMemberAccess(node As SyntaxNode,
                                           name As String,
                                           boundTypeArguments As BoundTypeArguments,
                                           receiver As BoundExpression,
                                           containerType As TypeSymbol,
                                           diagnostics As DiagnosticBag,
                                           Optional suppressLateBindingResolutionDiagnostics As Boolean = False) As BoundExpression

            receiver = AdjustReceiverAmbiguousTypeOrValue(receiver, diagnostics)

            If OptionStrict = VisualBasic.OptionStrict.On Then
                ' "Option Strict On disallows late binding."
                If Not suppressLateBindingResolutionDiagnostics Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_StrictDisallowsLateBinding)
                End If

                Dim children = ArrayBuilder(Of BoundExpression).GetInstance
                If receiver IsNot Nothing Then
                    children.Add(receiver)
                End If

                If boundTypeArguments IsNot Nothing Then
                    children.Add(boundTypeArguments)
                End If

                Return BadExpression(node, children.ToImmutableAndFree, ErrorTypeSymbol.UnknownResultType)

            ElseIf OptionStrict = VisualBasic.OptionStrict.Custom AndAlso Not suppressLateBindingResolutionDiagnostics Then
                ReportDiagnostic(diagnostics, node, ERRID.WRN_LateBindingResolution)
            End If

            Dim objType = Me.GetSpecialType(SpecialType.System_Object, node, diagnostics)

            If receiver IsNot Nothing AndAlso
                receiver.Kind = BoundKind.MeReference AndAlso
                (IsMeOrMyBaseOrMyClassInSharedContext() OrElse IsInsideChainedConstructorCallArguments) Then

                receiver = Nothing
            End If

            If receiver IsNot Nothing AndAlso Not receiver.IsLValue Then
                receiver = MakeRValue(receiver, diagnostics)
            End If

            Dim result = New BoundLateMemberAccess(node, name, containerType, receiver, boundTypeArguments, LateBoundAccessKind.Unknown, objType)

            Return result
        End Function

        Private Function BindLateBoundInvocation(node As SyntaxNode,
                                   group As BoundMethodOrPropertyGroup,
                                   isDefaultMemberAccess As Boolean,
                                   arguments As ImmutableArray(Of BoundExpression),
                                   argumentNames As ImmutableArray(Of String),
                                   diagnostics As DiagnosticBag) As BoundExpression

            Dim memberName As String = If(isDefaultMemberAccess,
                                          Nothing,
                                          group.MemberName)

            Dim typeArguments As BoundTypeArguments = group.TypeArguments
            Dim containingType As TypeSymbol = group.ContainerOfFirstInGroup

            Dim receiver As BoundExpression = AdjustReceiverAmbiguousTypeOrValue(group, diagnostics)

            If receiver IsNot Nothing AndAlso
                (receiver.Kind = BoundKind.TypeExpression OrElse receiver.Kind = BoundKind.NamespaceExpression) Then

                receiver = Nothing
            End If

            Dim memberSyntax As SyntaxNode
            Dim invocationSyntax = TryCast(node, InvocationExpressionSyntax)
            If invocationSyntax IsNot Nothing Then
                memberSyntax = If(invocationSyntax.Expression, group.Syntax)
            Else
                memberSyntax = node
            End If

            Dim lateMember = BindLateBoundMemberAccess(memberSyntax, memberName, typeArguments, receiver, containingType, diagnostics,
                                                       suppressLateBindingResolutionDiagnostics:=True) ' BindLateBoundInvocation will take care of the diagnostics.

            If group.WasCompilerGenerated Then
                lateMember.SetWasCompilerGenerated()
            End If

            If receiver IsNot Nothing AndAlso receiver.Type IsNot Nothing AndAlso receiver.Type.IsInterfaceType Then
                ReportDiagnostic(diagnostics, GetLocationForOverloadResolutionDiagnostic(node, group), ERRID.ERR_LateBoundOverloadInterfaceCall1, memberName)
            End If

            Return BindLateBoundInvocation(node, group, lateMember, arguments, argumentNames, diagnostics)
        End Function

        Friend Function BindLateBoundInvocation(node As SyntaxNode,
                                           groupOpt As BoundMethodOrPropertyGroup,
                                           receiver As BoundExpression,
                                           arguments As ImmutableArray(Of BoundExpression),
                                           argumentNames As ImmutableArray(Of String),
                                           diagnostics As DiagnosticBag,
                                           Optional suppressLateBindingResolutionDiagnostics As Boolean = False) As BoundExpression

            Debug.Assert(receiver IsNot Nothing AndAlso receiver.Kind <> BoundKind.TypeOrValueExpression)
            Debug.Assert(groupOpt Is Nothing OrElse groupOpt.ReceiverOpt Is Nothing OrElse groupOpt.ReceiverOpt.Kind <> BoundKind.TypeOrValueExpression)
            ' The given receiver should also have been derived from groupOpt.ReceiverOpt (if it exists).

            'TODO: may need to distinguish indexing/calling/dictionary
            'TODO: for example "Dim a = ("a".Clone)()" is an IndexGet

            If receiver.IsNothingLiteral Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_IllegalCallOrIndex)

                Return BadExpression(node, arguments, ErrorTypeSymbol.UnknownResultType)
            End If

            If OptionStrict = VisualBasic.OptionStrict.On Then
                Debug.Assert(Not suppressLateBindingResolutionDiagnostics)

                ' "Option Strict On disallows late binding."
                ReportDiagnostic(diagnostics, GetLocationForOverloadResolutionDiagnostic(node, groupOpt), ERRID.ERR_StrictDisallowsLateBinding)

                Dim children = ArrayBuilder(Of BoundExpression).GetInstance
                If receiver IsNot Nothing Then
                    children.Add(receiver)
                End If

                If Not arguments.IsEmpty Then
                    children.AddRange(arguments)
                End If

                Return BadExpression(node, children.ToImmutableAndFree, ErrorTypeSymbol.UnknownResultType)

            ElseIf OptionStrict = VisualBasic.OptionStrict.Custom AndAlso Not suppressLateBindingResolutionDiagnostics Then
                ReportDiagnostic(diagnostics, GetLocationForOverloadResolutionDiagnostic(node, groupOpt), ERRID.WRN_LateBindingResolution)
            End If

            Dim isIndexing As Boolean = receiver IsNot Nothing AndAlso Not receiver.Kind = BoundKind.LateMemberAccess
            Dim objectType = GetSpecialType(SpecialType.System_Object, node, diagnostics)

            If Not arguments.IsEmpty Then
                CheckNamedArgumentsForLateboundInvocation(argumentNames, arguments, diagnostics)

                Dim builder As ArrayBuilder(Of BoundExpression) = Nothing

                For i As Integer = 0 To arguments.Length - 1
                    Dim origArgument = arguments(i)

                    Dim argument As BoundExpression = origArgument

                    If argument.Kind = BoundKind.OmittedArgument Then
                        Dim omitted = DirectCast(argument, BoundOmittedArgument)
                        ' Omitted arguments are valid in a context of latebound invocation
                        ' so we will reclassify it as an object value.
                        ' NOTE: We do not want to make this a part of general reclassification as 
                        '       in general omitted argument is not a value.
                        argument = omitted.Update(GetSpecialType(SpecialType.System_Object, argument.Syntax, diagnostics))
                    End If

                    ' indexing is always ByVal
                    ' otherwise everything potentially assignable is passed ByRef
                    Dim passByRef As Boolean = Not isIndexing AndAlso IsSupportingAssignment(argument)

                    If Not isIndexing AndAlso IsSupportingAssignment(argument) Then
                        ' Leave property access and late bound nodes with unknown access kind as we don't know whether there will be
                        ' an attempt to copy back value at runtime.

                    Else
                        argument = ApplyImplicitConversion(argument.Syntax, objectType, argument, diagnostics)
                    End If


                    If builder IsNot Nothing Then
                        builder.Add(argument)
                    Else
                        If argument IsNot origArgument Then
                            builder = ArrayBuilder(Of BoundExpression).GetInstance(arguments.Length)
                            For j = 0 To i - 1
                                builder.Add(arguments(j))
                            Next
                            builder.Add(argument)
                        End If
                    End If
                Next

                If builder IsNot Nothing Then
                    arguments = builder.ToImmutableAndFree
                End If
            End If

            If receiver IsNot Nothing AndAlso
                receiver.Kind = BoundKind.MeReference AndAlso
                (IsMeOrMyBaseOrMyClassInSharedContext() OrElse IsInsideChainedConstructorCallArguments) Then

                receiver = Nothing
            End If

            If receiver IsNot Nothing AndAlso Not receiver.IsLValue AndAlso receiver.Kind <> BoundKind.LateMemberAccess Then
                receiver = MakeRValue(receiver, diagnostics)
            End If

            Dim objType = Me.GetSpecialType(SpecialType.System_Object, node, diagnostics)
            Return New BoundLateInvocation(node, receiver, arguments, argumentNames, LateBoundAccessKind.Unknown, groupOpt, objType)
        End Function

        Private Sub CheckNamedArgumentsForLateboundInvocation(argumentNames As ImmutableArray(Of String),
                                                            arguments As ImmutableArray(Of BoundExpression),
                                                            diagnostics As DiagnosticBag)

            Debug.Assert(Not arguments.IsDefault)

            If argumentNames.IsDefault OrElse argumentNames.Count = 0 Then
                Return
            End If

            If Not Compilation.LanguageVersion.AllowNonTrailingNamedArguments() Then
                Return
            End If

            Dim seenName As Boolean = False
            For i As Integer = 0 To argumentNames.Count - 1

                If argumentNames(i) IsNot Nothing Then
                    seenName = True
                ElseIf seenName Then
                    ReportDiagnostic(diagnostics, arguments(i).Syntax, ERRID.ERR_NamedArgumentSpecificationBeforeFixedArgumentInLateboundInvocation)
                    Return
                End If
            Next

        End Sub
    End Class
End Namespace

