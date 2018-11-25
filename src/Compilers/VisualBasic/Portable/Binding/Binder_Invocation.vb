' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' Binding of method/property invocation is implemented in this part.

    Partial Friend Class Binder

        Private Function CreateBoundMethodGroup(
            node As SyntaxNode,
            lookupResult As LookupResult,
            lookupOptionsUsed As LookupOptions,
            receiver As BoundExpression,
            typeArgumentsOpt As BoundTypeArguments,
            qualKind As QualificationKind,
            Optional hasError As Boolean = False
        ) As BoundMethodGroup
            Dim pendingExtensionMethods As ExtensionMethodGroup = Nothing

            Debug.Assert(lookupResult.Kind = LookupResultKind.Good OrElse lookupResult.Kind = LookupResultKind.Inaccessible)

            ' Name lookup does not look for extension methods if it found a suitable
            ' instance method. So, if the first symbol we have is not a reduced extension
            ' method, we might need to look for extension methods later, on demand.
            Debug.Assert((lookupOptionsUsed And LookupOptions.EagerlyLookupExtensionMethods) = 0)
            If lookupResult.IsGood AndAlso Not lookupResult.Symbols(0).IsReducedExtensionMethod() Then
                pendingExtensionMethods = New ExtensionMethodGroup(Me, lookupOptionsUsed)
            End If

            Return New BoundMethodGroup(
                node,
                typeArgumentsOpt,
                lookupResult.Symbols.ToDowncastedImmutable(Of MethodSymbol),
                pendingExtensionMethods,
                lookupResult.Kind,
                receiver,
                qualKind,
                hasErrors:=hasError)
        End Function

        ''' <summary>
        ''' Returns if all the rules for a "Me.New" or "MyBase.New" constructor call are satisfied:
        '''   a) In instance constructor body
        '''   b) First statement of that constructor
        '''   c) "Me", "MyClass", or "MyBase" is the receiver.
        ''' </summary>
        Private Function IsConstructorCallAllowed(invocationExpression As InvocationExpressionSyntax, boundMemberGroup As BoundMethodOrPropertyGroup) As Boolean
            If Me.ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(Me.ContainingMember, MethodSymbol).MethodKind = MethodKind.Constructor Then
                ' (a) we are in an instance constructor body

                Dim node As VisualBasicSyntaxNode = invocationExpression.Parent
                If node Is Nothing OrElse (node.Kind <> SyntaxKind.CallStatement AndAlso node.Kind <> SyntaxKind.ExpressionStatement) Then
                    Return False
                End If

                Dim nodeParent As VisualBasicSyntaxNode = node.Parent
                If nodeParent Is Nothing OrElse nodeParent.Kind <> SyntaxKind.ConstructorBlock Then
                    Return False
                End If

                If DirectCast(nodeParent, ConstructorBlockSyntax).Statements(0) Is node Then
                    ' (b) call statement we are binding is 'the first' statement of the constructor
                    Dim receiver As BoundExpression = boundMemberGroup.ReceiverOpt

                    If receiver IsNot Nothing AndAlso (receiver.Kind = BoundKind.MeReference OrElse
                                                       receiver.Kind = BoundKind.MyBaseReference OrElse
                                                       receiver.Kind = BoundKind.MyClassReference) Then
                        ' (c) receiver is 'Me'/'MyClass'/'MyBase'
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Friend Class ConstructorCallArgumentsBinder
            Inherits Binder

            Public Sub New(containingBinder As Binder)
                MyBase.New(containingBinder)
            End Sub

            Protected Overrides ReadOnly Property IsInsideChainedConstructorCallArguments As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Bind a Me.New(...), MyBase.New (...), MyClass.New(...) constructor call. 
        ''' (NOT a normal constructor call like New Type(...)).
        ''' </summary>
        Private Function BindDirectConstructorCall(node As InvocationExpressionSyntax, group As BoundMethodGroup, diagnostics As DiagnosticBag) As BoundExpression
            Dim boundArguments As ImmutableArray(Of BoundExpression) = Nothing
            Dim argumentNames As ImmutableArray(Of String) = Nothing
            Dim argumentNamesLocations As ImmutableArray(Of Location) = Nothing
            Dim argumentList As ArgumentListSyntax = node.ArgumentList

            Debug.Assert(IsGroupOfConstructors(group))

            ' Direct constructor call is only allowed if: (a) we are in an instance constructor body,
            ' and (b) call statement we are binding is 'the first' statement of the constructor, 
            ' and (c) receiver is 'Me'/'MyClass'/'MyBase'
            If IsConstructorCallAllowed(node, group) Then
                ' Bind arguments with special binder that prevents use of Me.
                Dim argumentsBinder As Binder = New ConstructorCallArgumentsBinder(Me)
                argumentsBinder.BindArgumentsAndNames(argumentList, boundArguments, argumentNames, argumentNamesLocations, diagnostics)

                ' Bind constructor call, errors will be generated if needed
                Return BindInvocationExpression(node, node.Expression, ExtractTypeCharacter(node.Expression),
                                                group, boundArguments, argumentNames, diagnostics,
                                                allowConstructorCall:=True, callerInfoOpt:=group.Syntax)
            Else
                ' Error case -- constructor call in wrong location.
                ' Report error BC30282 about invalid constructor call
                ' For semantic model / IDE purposes, we still bind it even if in a location that wasn't allowed.
                If Not group.HasErrors Then
                    ReportDiagnostic(diagnostics, group.Syntax, ERRID.ERR_InvalidConstructorCall)
                End If

                BindArgumentsAndNames(argumentList, boundArguments, argumentNames, argumentNamesLocations, diagnostics)

                ' Bind constructor call, ignore errors by putting into discarded bag.
                Dim discardedDiagnostics = DiagnosticBag.GetInstance()
                Dim expr = BindInvocationExpression(node, node.Expression, ExtractTypeCharacter(node.Expression),
                                                group, boundArguments, argumentNames, discardedDiagnostics,
                                                allowConstructorCall:=True, callerInfoOpt:=group.Syntax)
                discardedDiagnostics.Free()
                If expr.Kind = BoundKind.Call Then
                    ' Set HasErrors to prevent cascading errors.
                    Dim callExpr = DirectCast(expr, BoundCall)
                    expr = New BoundCall(
                        callExpr.Syntax,
                        callExpr.Method,
                        callExpr.MethodGroupOpt,
                        callExpr.ReceiverOpt,
                        callExpr.Arguments,
                        callExpr.DefaultArguments,
                        callExpr.ConstantValueOpt,
                        isLValue:=False,
                        suppressObjectClone:=False,
                        type:=callExpr.Type,
                        hasErrors:=True)
                End If

                Return expr
            End If

        End Function

        Private Function BindInvocationExpression(node As InvocationExpressionSyntax, diagnostics As DiagnosticBag) As BoundExpression

            ' Set "IsInvocationsOrAddressOf" to prevent binding to return value variable.
            Dim target As BoundExpression

            If node.Expression Is Nothing Then
                ' Must be conditional case
                Dim conditionalAccess As ConditionalAccessExpressionSyntax = node.GetCorrespondingConditionalAccessExpression()

                If conditionalAccess IsNot Nothing Then
                    target = GetConditionalAccessReceiver(conditionalAccess)
                Else
                    target = ReportDiagnosticAndProduceBadExpression(diagnostics, node, ERRID.ERR_Syntax).MakeCompilerGenerated()
                End If
            Else
                target = BindExpression(node.Expression, diagnostics:=diagnostics, isInvocationOrAddressOf:=True, isOperandOfConditionalBranch:=False, eventContext:=False)
            End If

            ' If 'target' is a bound constructor group, we need to do special checks and special processing of arguments.
            If target.Kind = BoundKind.MethodGroup Then
                Dim group = DirectCast(target, BoundMethodGroup)
                If IsGroupOfConstructors(group) Then
                    Return BindDirectConstructorCall(node, group, diagnostics)
                End If
            End If

            Dim boundArguments As ImmutableArray(Of BoundExpression) = Nothing
            Dim argumentNames As ImmutableArray(Of String) = Nothing
            Dim argumentNamesLocations As ImmutableArray(Of Location) = Nothing
            Me.BindArgumentsAndNames(node.ArgumentList, boundArguments, argumentNames, argumentNamesLocations, diagnostics)

            If target.Kind = BoundKind.MethodGroup OrElse target.Kind = BoundKind.PropertyGroup Then
                Return BindInvocationExpressionPossiblyWithoutArguments(
                    node,
                    ExtractTypeCharacter(node.Expression),
                    DirectCast(target, BoundMethodOrPropertyGroup),
                    boundArguments,
                    argumentNames,
                    argumentNamesLocations,
                    allowBindingWithoutArguments:=True,
                    diagnostics:=diagnostics)
            End If

            If target.Kind = BoundKind.NamespaceExpression Then
                Dim namespaceExp As BoundNamespaceExpression = DirectCast(target, BoundNamespaceExpression)
                Dim diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_NamespaceNotExpression1, namespaceExp.NamespaceSymbol)
                ReportDiagnostic(diagnostics, node.Expression, diagInfo)

            ElseIf target.Kind = BoundKind.TypeExpression Then
                Dim typeExp As BoundTypeExpression = DirectCast(target, BoundTypeExpression)

                If Not IsCallStatementContext(node) Then
                    ' Try default instance property through DefaultInstanceAlias
                    Dim instance As BoundExpression = TryDefaultInstanceProperty(typeExp, diagnostics)

                    If instance IsNot Nothing Then
                        Return BindIndexedInvocationExpression(
                            node,
                            instance,
                            boundArguments,
                            argumentNames,
                            argumentNamesLocations,
                            allowBindingWithoutArguments:=False,
                            hasIndexableTarget:=False,
                            diagnostics:=diagnostics)
                    End If
                End If

                Dim diagInfo = ErrorFactory.ErrorInfo(GetTypeNotExpressionErrorId(typeExp.Type), typeExp.Type)
                ReportDiagnostic(diagnostics, node.Expression, diagInfo)

            Else
                Return BindIndexedInvocationExpression(
                        node,
                        target,
                        boundArguments,
                        argumentNames,
                        argumentNamesLocations,
                        allowBindingWithoutArguments:=True,
                        hasIndexableTarget:=False,
                        diagnostics:=diagnostics)
            End If

            Return GenerateBadExpression(node, target, boundArguments)
        End Function

        ''' <summary>
        ''' Bind an invocation expression representing an array access,
        ''' delegate invocation, or default member.
        ''' </summary>
        Private Function BindIndexedInvocationExpression(
            node As InvocationExpressionSyntax,
            target As BoundExpression,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            argumentNamesLocations As ImmutableArray(Of Location),
            allowBindingWithoutArguments As Boolean,
            <Out()> ByRef hasIndexableTarget As Boolean,
            diagnostics As DiagnosticBag) As BoundExpression

            Debug.Assert(target.Kind <> BoundKind.NamespaceExpression)
            Debug.Assert(target.Kind <> BoundKind.TypeExpression)
            Debug.Assert(target.Kind <> BoundKind.MethodGroup)
            Debug.Assert(target.Kind <> BoundKind.PropertyGroup)

            hasIndexableTarget = False

            If Not target.IsLValue AndAlso target.Kind <> BoundKind.LateMemberAccess Then
                target = MakeRValue(target, diagnostics)
            End If

            Dim targetType As TypeSymbol = target.Type

            ' there are values or variables like "Nothing" which have no type
            If targetType IsNot Nothing Then

                ' this method is also called for e.g. Arrays because they are also InvocationExpressions
                ' if target is an array, then call BindArrayAccess only if target is not a direct successor
                ' of a call statement
                If targetType.IsArrayType Then
                    hasIndexableTarget = True

                    ' only bind to an array if this method was called outside of a call statement context
                    If Not IsCallStatementContext(node) Then
                        Return BindArrayAccess(node, target, boundArguments, argumentNames, diagnostics)
                    End If

                ElseIf targetType.Kind = SymbolKind.NamedType AndAlso targetType.TypeKind = TypeKind.Delegate Then
                    hasIndexableTarget = True

                    ' an invocation of a delegate actually calls the delegate's Invoke method.
                    Dim delegateInvoke = DirectCast(targetType, NamedTypeSymbol).DelegateInvokeMethod

                    If delegateInvoke Is Nothing Then
                        If Not target.HasErrors Then
                            ReportDiagnostic(diagnostics, target.Syntax, ERRID.ERR_DelegateNoInvoke1, target.Type)
                        End If

                    ElseIf ReportDelegateInvokeUseSiteError(diagnostics, target.Syntax, targetType, delegateInvoke) Then
                        delegateInvoke = Nothing
                    End If

                    If delegateInvoke IsNot Nothing Then
                        Dim methodGroup = New BoundMethodGroup(
                            If(node.Expression, node),
                            Nothing,
                            ImmutableArray.Create(Of MethodSymbol)(delegateInvoke),
                            LookupResultKind.Good,
                            target,
                            QualificationKind.QualifiedViaValue).MakeCompilerGenerated()

                        Return BindInvocationExpression(
                            node,
                            If(node.Expression, node),
                            ExtractTypeCharacter(node.Expression),
                            methodGroup,
                            boundArguments,
                            argumentNames,
                            diagnostics,
                            callerInfoOpt:=node,
                            representCandidateInDiagnosticsOpt:=targetType)
                    Else
                        Dim badExpressionChildren = ArrayBuilder(Of BoundExpression).GetInstance()
                        badExpressionChildren.Add(target)
                        badExpressionChildren.AddRange(boundArguments)
                        Return BadExpression(node, badExpressionChildren.ToImmutableAndFree(), ErrorTypeSymbol.UnknownResultType)
                    End If
                End If
            End If

            If target.Kind = BoundKind.BadExpression Then
                ' Error already reported for a bad expression, so don't report another error

            ElseIf Not IsCallStatementContext(node) Then
                ' If the invocation is outside of a call statement
                ' context, bind to the default property group if any.

                If target.Type.SpecialType = SpecialType.System_Object OrElse
                   target.Type.SpecialType = SpecialType.System_Array Then

                    hasIndexableTarget = True

                    Return BindLateBoundInvocation(node, Nothing, target, boundArguments, argumentNames, diagnostics,
                                                   suppressLateBindingResolutionDiagnostics:=(target.Kind = BoundKind.LateMemberAccess))
                End If

                If Not target.HasErrors Then
                    ' Bind to the default property group.
                    Dim defaultPropertyGroup As BoundExpression = BindDefaultPropertyGroup(If(node.Expression, node), target, diagnostics)

                    If defaultPropertyGroup IsNot Nothing Then
                        Debug.Assert(defaultPropertyGroup.Kind = BoundKind.PropertyGroup OrElse
                                     defaultPropertyGroup.Kind = BoundKind.MethodGroup OrElse
                                     defaultPropertyGroup.HasErrors)

                        hasIndexableTarget = True

                        If defaultPropertyGroup.Kind = BoundKind.PropertyGroup OrElse defaultPropertyGroup.Kind = BoundKind.MethodGroup Then
                            Return BindInvocationExpressionPossiblyWithoutArguments(
                                node,
                                TypeCharacter.None,
                                DirectCast(defaultPropertyGroup, BoundMethodOrPropertyGroup),
                                boundArguments,
                                argumentNames,
                                argumentNamesLocations,
                                allowBindingWithoutArguments,
                                diagnostics)
                        End If
                    Else
                        ReportNoDefaultProperty(target, diagnostics)
                    End If
                End If

            ElseIf target.Kind = BoundKind.LateMemberAccess Then
                hasIndexableTarget = True

                Dim lateMember = DirectCast(target, BoundLateMemberAccess)
                Return BindLateBoundInvocation(node, Nothing, lateMember, boundArguments, argumentNames, diagnostics)

            ElseIf Not target.HasErrors Then
                ' "Expression is not a method."
                Dim diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedProcedure)
                ReportDiagnostic(diagnostics, If(node.Expression, node), diagInfo)
            End If

            Return GenerateBadExpression(node, target, boundArguments)
        End Function

        Private Function BindInvocationExpressionPossiblyWithoutArguments(
            node As InvocationExpressionSyntax,
            typeChar As TypeCharacter,
            group As BoundMethodOrPropertyGroup,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            argumentNamesLocations As ImmutableArray(Of Location),
            allowBindingWithoutArguments As Boolean,
            diagnostics As DiagnosticBag) As BoundExpression

            ' Spec §11.8 Invocation Expressions
            ' ...
            ' If the method group only contains one accessible method, including both instance and 
            ' extension methods, and that method takes no arguments and is a function, then the method 
            ' group is interpreted as an invocation expression with an empty argument list and the result 
            ' is used as the target of an invocation expression with the provided argument list(s).
            If allowBindingWithoutArguments AndAlso
                boundArguments.Length > 0 AndAlso
                Not IsCallStatementContext(node) AndAlso
                ShouldBindWithoutArguments(node, group, diagnostics) Then

                Dim tmpDiagnostics = DiagnosticBag.GetInstance()
                Dim result As BoundExpression = Nothing

                Debug.Assert(node.Expression IsNot Nothing)

                ' NOTE: when binding without arguments, we pass node.Expression as the first parameter 
                ' so that the new bound node references it instead of invocation expression
                Dim withoutArgs = BindInvocationExpression(
                            node.Expression,
                            node.Expression,
                            typeChar,
                            group,
                            ImmutableArray(Of BoundExpression).Empty,
                            Nothing,
                            tmpDiagnostics,
                            callerInfoOpt:=group.Syntax)

                If withoutArgs.Kind = BoundKind.Call OrElse withoutArgs.Kind = BoundKind.PropertyAccess Then
                    ' We were able to bind the method group or property access without arguments,
                    ' possibly with some diagnostic.
                    diagnostics.AddRange(tmpDiagnostics)
                    tmpDiagnostics.Clear()

                    If withoutArgs.Kind = BoundKind.PropertyAccess Then
                        Dim receiverOpt As BoundExpression = DirectCast(withoutArgs, BoundPropertyAccess).ReceiverOpt
                        If receiverOpt?.Syntax Is withoutArgs.Syntax AndAlso Not receiverOpt.WasCompilerGenerated Then
                            withoutArgs.MakeCompilerGenerated()
                        End If

                        withoutArgs = MakeRValue(withoutArgs, diagnostics)

                    Else
                        Dim receiverOpt As BoundExpression = DirectCast(withoutArgs, BoundCall).ReceiverOpt
                        If receiverOpt?.Syntax Is withoutArgs.Syntax AndAlso Not receiverOpt.WasCompilerGenerated Then
                            withoutArgs.MakeCompilerGenerated()
                        End If
                    End If

                    If withoutArgs.Kind = BoundKind.BadExpression Then
                        result = GenerateBadExpression(node, withoutArgs, boundArguments)

                    Else
                        Dim hasIndexableTarget = False

                        ' Bind the invocation with arguments as an indexed invocation.
                        Dim withArgs = BindIndexedInvocationExpression(
                            node,
                            withoutArgs,
                            boundArguments,
                            argumentNames,
                            argumentNamesLocations,
                            allowBindingWithoutArguments:=False,
                            hasIndexableTarget:=hasIndexableTarget,
                            diagnostics:=tmpDiagnostics)

                        If hasIndexableTarget Then
                            diagnostics.AddRange(tmpDiagnostics)
                            result = withArgs

                        Else
                            ' Report BC32016 if something wrong.
                            ReportDiagnostic(diagnostics,
                                             node.Expression,
                                             ERRID.ERR_FunctionResultCannotBeIndexed1,
                                             withoutArgs.ExpressionSymbol)

                            ' If result of binding with no args was not indexable after all, then instead
                            ' of just producing a bad expression, bind the invocation expression normally,
                            ' but without reporting any more diagnostics. This produces more accurate
                            ' bound nodes for semantic model questions and may allow the type of the
                            ' expression to be computed, thus leading to fewer errors later.
                            result = BindInvocationExpression(
                                node,
                                node.Expression,
                                typeChar,
                                group,
                                boundArguments,
                                argumentNames,
                                tmpDiagnostics,
                                callerInfoOpt:=group.Syntax)
                        End If
                    End If
                End If

                tmpDiagnostics.Free()

                If result IsNot Nothing Then
                    Return result
                End If
            End If

            Return BindInvocationExpression(
                node,
                If(node.Expression, group.Syntax),
                typeChar,
                group,
                boundArguments,
                argumentNames,
                diagnostics,
                callerInfoOpt:=group.Syntax)
        End Function

        ''' <summary>
        ''' Returns a BoundPropertyGroup if the expression represents a valid
        ''' default property access. If there is a default property but the property
        ''' access is invalid, a BoundBadExpression is returned. If there is no
        ''' default property for the expression type, Nothing is returned.
        ''' 
        ''' Note, that default Query Indexer may be a method, not a property.
        ''' </summary>
        Private Function BindDefaultPropertyGroup(node As VisualBasicSyntaxNode, target As BoundExpression, diagnostics As DiagnosticBag) As BoundExpression
            Dim result = LookupResult.GetInstance()
            Dim defaultMemberGroup As BoundExpression = Nothing
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            MemberLookup.LookupDefaultProperty(result, target.Type, Me, useSiteDiagnostics)

            ' We're not reporting any diagnostic if there are no symbols.
            Debug.Assert(result.HasSymbol OrElse Not result.HasDiagnostic)

            If result.HasSymbol Then
                defaultMemberGroup = BindSymbolAccess(node, result, LookupOptions.Default, target, Nothing, QualificationKind.QualifiedViaValue, diagnostics)
                Debug.Assert(defaultMemberGroup IsNot Nothing)
                Debug.Assert((defaultMemberGroup.Kind = BoundKind.BadExpression) OrElse (defaultMemberGroup.Kind = BoundKind.PropertyGroup))

            Else
                ' All queryable sources have default indexer, which maps to an ElementAtOrDefault method or property on the source.
                Dim tempDiagnostics = DiagnosticBag.GetInstance()

                target = MakeRValue(target, tempDiagnostics)

                Dim controlVariableType As TypeSymbol = Nothing
                target = ConvertToQueryableType(target, tempDiagnostics, controlVariableType)

                If controlVariableType IsNot Nothing Then
                    result.Clear()

                    Const options As LookupOptions = LookupOptions.AllMethodsOfAnyArity ' overload resolution filters methods by arity.

                    LookupMember(result, target.Type, StringConstants.ElementAtMethod, 0, options, useSiteDiagnostics)

                    If result.IsGood Then
                        Dim kind As SymbolKind = result.Symbols(0).Kind

                        If kind = SymbolKind.Method OrElse kind = SymbolKind.Property Then
                            diagnostics.AddRange(tempDiagnostics)
                            defaultMemberGroup = BindSymbolAccess(node, result, options, target, Nothing, QualificationKind.QualifiedViaValue, diagnostics)
                        End If
                    End If
                End If

                tempDiagnostics.Free()
            End If

            diagnostics.Add(node, useSiteDiagnostics)
            result.Free()

            ' We don't want the default property GROUP to override the meaning of the item it's being 
            ' accessed off of, so mark it as compiler generated.
            If defaultMemberGroup IsNot Nothing Then
                defaultMemberGroup.SetWasCompilerGenerated()
            End If

            Return defaultMemberGroup
        End Function

        ''' <summary>
        ''' Tests whether or not the method or property group should be bound without arguments. 
        ''' In case of method group it may also update the group by filtering out all subs
        ''' </summary>
        Private Function ShouldBindWithoutArguments(node As VisualBasicSyntaxNode, ByRef group As BoundMethodOrPropertyGroup, diagnostics As DiagnosticBag) As Boolean
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim result = ShouldBindWithoutArguments(group, useSiteDiagnostics)
            diagnostics.Add(node, useSiteDiagnostics)
            Return result
        End Function

        Private Function ShouldBindWithoutArguments(ByRef group As BoundMethodOrPropertyGroup, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean

            If group.Kind = BoundKind.MethodGroup Then

                Dim newMethods As ArrayBuilder(Of MethodSymbol) = ArrayBuilder(Of MethodSymbol).GetInstance()

                ' check method group members
                Dim methodGroup = DirectCast(group, BoundMethodGroup)
                Debug.Assert(methodGroup.Methods.Length > 0)

                ' any sub should be removed from a group in case we try binding without arguments
                Dim shouldUpdateGroup As Boolean = False
                Dim atLeastOneFunction As Boolean = False

                ' Dev10 behavior: 
                ' For instance methods - 
                '     Check if all functions from the group (ignoring subs) 
                '     have the same arity as specified in the call and 0 parameters.
                '     However, the part that handles arity check is rather inconsistent between methods 
                '     overloaded within the same type and in derived type. Also, language spec doesn't mention 
                '     any restrictions for arity. So, we will not try to duplicate Dev10 logic around arity 
                '     because the logic is close to impossible to match and behavior change will not be a breaking
                '     change.
                ' In presence of extension methods the rules are more constrained-
                '    If group contains an extension method, it must be the only method in the group,
                '    it must have no parameters, must have no type parameters and must be a Function.

                ' Note, we should avoid requesting AdditionalExtensionMethods whenever possible because 
                ' lookup of extension methods might be very expensive.
                Dim extensionMethod As MethodSymbol = Nothing

                For Each method In methodGroup.Methods

                    If method.IsReducedExtensionMethod Then
                        extensionMethod = method
                        Exit For
                    End If

                    If (method.IsSub) Then
                        If method.CanBeCalledWithNoParameters() Then
                            ' If its a sub that could be called parameterlessly, it might hide the function. So it is included
                            ' in the group for further processing in overload resolution (which will process possible hiding).
                            ' If overload resolution does select the Sub, we'll get an error about return type not indexable. 
                            ' See Roslyn bug 14019 for example.
                            newMethods.Add(method)
                        Else
                            ' ignore other subs entirely 
                            shouldUpdateGroup = True
                        End If
                    ElseIf method.ParameterCount > 0 Then
                        ' any function with more than 0 parameters 
                        newMethods.Free()
                        Return False
                    Else
                        newMethods.Add(method)
                        atLeastOneFunction = True
                    End If
                Next

                If extensionMethod Is Nothing Then
                    Dim additionalExtensionMethods As ImmutableArray(Of MethodSymbol) = methodGroup.AdditionalExtensionMethods(useSiteDiagnostics)

                    If additionalExtensionMethods.Length > 0 Then
                        Debug.Assert(methodGroup.Methods.Length > 0)
                        ' We have at least one extension method in the group and it is not the only one method in the
                        ' group. Cannot apply default property transformation.
                        newMethods.Free()
                        Return False
                    End If
                Else
                    newMethods.Free()

                    Debug.Assert(extensionMethod IsNot Nothing)

                    ' This method must have no parameters, must have no type parameters and must not be a Sub.
                    Return methodGroup.Methods.Length = 1 AndAlso methodGroup.TypeArgumentsOpt Is Nothing AndAlso
                           extensionMethod.ParameterCount = 0 AndAlso
                           extensionMethod.Arity = 0 AndAlso
                           Not extensionMethod.IsSub AndAlso
                           methodGroup.AdditionalExtensionMethods(useSiteDiagnostics).Length = 0
                End If

                If Not atLeastOneFunction Then
                    newMethods.Free()
                    Return False
                End If

                If shouldUpdateGroup Then
                    ' at least one sub was removed
                    If newMethods.IsEmpty Then
                        ' no functions left
                        newMethods.Free()
                        Return False
                    End If

                    ' there are some functions, update the group
                    group = methodGroup.Update(methodGroup.TypeArgumentsOpt,
                                               newMethods.ToImmutable(),
                                               Nothing,
                                               methodGroup.ResultKind,
                                               methodGroup.ReceiverOpt,
                                               methodGroup.QualificationKind)
                End If

                newMethods.Free()
                Return True

            Else
                ' check property group members
                Dim propertyGroup = DirectCast(group, BoundPropertyGroup)
                Debug.Assert(propertyGroup.Properties.Length > 0)

                For Each prop In propertyGroup.Properties
                    If (prop.ParameterCount > 0) Then
                        Return False
                    End If
                Next

                ' assuming property group was not empty
                Return True
            End If

        End Function

        Private Shared Function IsGroupOfConstructors(group As BoundMethodOrPropertyGroup) As Boolean

            If group.Kind = BoundKind.MethodGroup Then
                Dim methodGroup = DirectCast(group, BoundMethodGroup)
                Debug.Assert(methodGroup.Methods.Length > 0)
                Return methodGroup.Methods(0).MethodKind = MethodKind.Constructor
            End If

            Return False
        End Function

        Friend Function BindInvocationExpression(
            node As SyntaxNode,
            target As SyntaxNode,
            typeChar As TypeCharacter,
            group As BoundMethodOrPropertyGroup,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            diagnostics As DiagnosticBag,
            callerInfoOpt As SyntaxNode,
            Optional allowConstructorCall As Boolean = False,
            Optional suppressAbstractCallDiagnostics As Boolean = False,
            Optional isDefaultMemberAccess As Boolean = False,
            Optional representCandidateInDiagnosticsOpt As Symbol = Nothing,
            Optional forceExpandedForm As Boolean = False
        ) As BoundExpression

            Debug.Assert(group IsNot Nothing)
            Debug.Assert(allowConstructorCall OrElse Not IsGroupOfConstructors(group))
            Debug.Assert(group.ResultKind = LookupResultKind.Good OrElse group.ResultKind = LookupResultKind.Inaccessible)

            ' It is possible to get here with method group with ResultKind = LookupResultKind.Inaccessible.
            ' When this happens, it is worth trying to do overload resolution on the "bad" set
            ' to report better errors.
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim results As OverloadResolution.OverloadResolutionResult = OverloadResolution.MethodOrPropertyInvocationOverloadResolution(group, boundArguments, argumentNames, Me, callerInfoOpt, useSiteDiagnostics, forceExpandedForm:=forceExpandedForm)

            If diagnostics.Add(node, useSiteDiagnostics) Then
                If group.ResultKind <> LookupResultKind.Inaccessible Then
                    ' Suppress additional diagnostics
                    diagnostics = New DiagnosticBag()
                End If
            End If

            If Not results.BestResult.HasValue Then

                If results.ResolutionIsLateBound Then
                    Debug.Assert(OptionStrict <> VisualBasic.OptionStrict.On)

                    ' Did we have extension methods among candidates?
                    If group.Kind = BoundKind.MethodGroup Then
                        Dim haveAnExtensionMethod As Boolean = False
                        Dim methodGroup = DirectCast(group, BoundMethodGroup)

                        For Each method As MethodSymbol In methodGroup.Methods
                            If method.ReducedFrom IsNot Nothing Then
                                haveAnExtensionMethod = True
                                Exit For
                            End If
                        Next

                        If Not haveAnExtensionMethod Then
                            useSiteDiagnostics = Nothing
                            haveAnExtensionMethod = Not methodGroup.AdditionalExtensionMethods(useSiteDiagnostics).IsEmpty
                            diagnostics.Add(node, useSiteDiagnostics)
                        End If

                        If haveAnExtensionMethod Then
                            ReportDiagnostic(diagnostics, GetLocationForOverloadResolutionDiagnostic(node, group), ERRID.ERR_ExtensionMethodCannotBeLateBound)

                            Dim builder = ArrayBuilder(Of BoundExpression).GetInstance()

                            builder.Add(group)

                            If Not boundArguments.IsEmpty Then
                                builder.AddRange(boundArguments)
                            End If

                            Return New BoundBadExpression(node, LookupResultKind.OverloadResolutionFailure,
                                                          ImmutableArray(Of Symbol).Empty, builder.ToImmutableAndFree(),
                                                          ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
                        End If
                    End If

                    Return BindLateBoundInvocation(node, group, isDefaultMemberAccess, boundArguments, argumentNames, diagnostics)
                End If

                ' Create and report the diagnostic.
                If results.Candidates.Length = 0 Then
                    results = OverloadResolution.MethodOrPropertyInvocationOverloadResolution(group, boundArguments, argumentNames, Me, includeEliminatedCandidates:=True, callerInfoOpt:=callerInfoOpt,
                                                                                              useSiteDiagnostics:=Nothing, forceExpandedForm:=forceExpandedForm)
                End If

                Return ReportOverloadResolutionFailureAndProduceBoundNode(node, group, boundArguments, argumentNames, results, diagnostics,
                                                                          callerInfoOpt,
                                                                          representCandidateInDiagnosticsOpt:=representCandidateInDiagnosticsOpt)
            Else
                Return CreateBoundCallOrPropertyAccess(
                            node,
                            target,
                            typeChar,
                            group,
                            boundArguments,
                            results.BestResult.Value,
                            results.AsyncLambdaSubToFunctionMismatch,
                            diagnostics,
                            suppressAbstractCallDiagnostics)
            End If
        End Function

        Private Function CreateBoundCallOrPropertyAccess(
            node As SyntaxNode,
            target As SyntaxNode,
            typeChar As TypeCharacter,
            group As BoundMethodOrPropertyGroup,
            boundArguments As ImmutableArray(Of BoundExpression),
            bestResult As OverloadResolution.CandidateAnalysisResult,
            asyncLambdaSubToFunctionMismatch As ImmutableArray(Of BoundExpression),
            diagnostics As DiagnosticBag,
            Optional suppressAbstractCallDiagnostics As Boolean = False
        ) As BoundExpression

            Dim candidate = bestResult.Candidate
            Dim methodOrProperty = candidate.UnderlyingSymbol
            Dim returnType = candidate.ReturnType

            If group.ResultKind = LookupResultKind.Inaccessible Then
                ReportDiagnostic(diagnostics, target, GetInaccessibleErrorInfo(bestResult.Candidate.UnderlyingSymbol, useSiteDiagnostics:=Nothing))
            Else
                Debug.Assert(group.ResultKind = LookupResultKind.Good)
                CheckMemberTypeAccessibility(diagnostics, node, methodOrProperty)
            End If

            If bestResult.TypeArgumentInferenceDiagnosticsOpt IsNot Nothing Then
                diagnostics.AddRange(bestResult.TypeArgumentInferenceDiagnosticsOpt)
            End If

            Dim argumentInfo As (Arguments As ImmutableArray(Of BoundExpression), DefaultArguments As BitVector) = PassArguments(node, bestResult, boundArguments, diagnostics)
            boundArguments = argumentInfo.Arguments
            Debug.Assert(Not boundArguments.IsDefault)

            Dim hasErrors As Boolean = False

            Dim receiver As BoundExpression = group.ReceiverOpt

            If group.ResultKind = LookupResultKind.Good Then
                hasErrors = CheckSharedSymbolAccess(target, methodOrProperty.IsShared, receiver, group.QualificationKind, diagnostics)  ' give diagnostics if sharedness is wrong.
            End If

            ReportDiagnosticsIfObsolete(diagnostics, methodOrProperty, node)

            hasErrors = hasErrors Or group.HasErrors

            If Not returnType.IsErrorType() Then
                VerifyTypeCharacterConsistency(node, returnType, typeChar, diagnostics)
            End If

            Dim resolvedTypeOrValueReceiver As BoundExpression = Nothing
            If receiver IsNot Nothing AndAlso Not hasErrors Then
                receiver = AdjustReceiverTypeOrValue(receiver, receiver.Syntax, methodOrProperty.IsShared, diagnostics, resolvedTypeOrValueReceiver)
            End If

            If Not suppressAbstractCallDiagnostics AndAlso receiver IsNot Nothing AndAlso (receiver.IsMyBaseReference OrElse receiver.IsMyClassReference) Then

                If methodOrProperty.IsMustOverride Then
                    '  Generate an error, but continue processing
                    ReportDiagnostic(diagnostics, group.Syntax,
                                     If(receiver.IsMyBaseReference,
                                        ERRID.ERR_MyBaseAbstractCall1,
                                        ERRID.ERR_MyClassAbstractCall1),
                                     methodOrProperty)
                End If
            End If

            If Not asyncLambdaSubToFunctionMismatch.IsEmpty Then
                For Each lambda In asyncLambdaSubToFunctionMismatch
                    Dim errorLocation As SyntaxNode = lambda.Syntax
                    Dim lambdaNode = TryCast(errorLocation, LambdaExpressionSyntax)

                    If lambdaNode IsNot Nothing Then
                        errorLocation = lambdaNode.SubOrFunctionHeader
                    End If

                    ReportDiagnostic(diagnostics, errorLocation, ERRID.WRN_AsyncSubCouldBeFunction)
                Next
            End If

            If methodOrProperty.Kind = SymbolKind.Method Then

                Dim method = DirectCast(methodOrProperty, MethodSymbol)
                Dim reducedFrom = method.ReducedFrom
                Dim constantValue As ConstantValue = Nothing

                If reducedFrom Is Nothing Then
                    If receiver IsNot Nothing AndAlso receiver.IsPropertyOrXmlPropertyAccess() Then
                        receiver = MakeRValue(receiver, diagnostics)
                    End If

                    If method.IsUserDefinedOperator() AndAlso Me.ContainingMember Is method Then
                        ReportDiagnostic(diagnostics, target, ERRID.WRN_RecursiveOperatorCall, method)
                    End If

                    ' replace call with literal if possible (Chr, ChrW, Asc, AscW)
                    constantValue = OptimizeLibraryCall(method,
                                                        boundArguments,
                                                        node,
                                                        hasErrors,
                                                        diagnostics)

                Else
                    ' We are calling an extension method, prepare the receiver to be 
                    ' passed as the first parameter.
                    receiver = UpdateReceiverForExtensionMethodOrPropertyGroup(receiver, method.ReceiverType, reducedFrom.Parameters(0), diagnostics)

                End If

                ' Remove receiver from the method group
                ' NOTE: we only remove it if we pass it to a new BoundCall node, 
                '       otherwise we keep it in the group to support semantic queries
                Dim methodGroup = DirectCast(group, BoundMethodGroup)
                Dim newReceiver As BoundExpression = If(receiver IsNot Nothing, Nothing, If(resolvedTypeOrValueReceiver, methodGroup.ReceiverOpt))
                methodGroup = methodGroup.Update(methodGroup.TypeArgumentsOpt,
                                                 methodGroup.Methods,
                                                 methodGroup.PendingExtensionMethodsOpt,
                                                 methodGroup.ResultKind,
                                                 newReceiver,
                                                 methodGroup.QualificationKind)

                Return New BoundCall(
                    node,
                    method,
                    methodGroup,
                    receiver,
                    boundArguments,
                    constantValue,
                    returnType,
                    suppressObjectClone:=False,
                    hasErrors:=hasErrors,
                    defaultArguments:=argumentInfo.DefaultArguments)

            Else
                Dim [property] = DirectCast(methodOrProperty, PropertySymbol)
                Dim reducedFrom = [property].ReducedFromDefinition

                Debug.Assert(Not boundArguments.Any(Function(a) a.Kind = BoundKind.ByRefArgumentWithCopyBack))

                If reducedFrom Is Nothing Then
                    If receiver IsNot Nothing AndAlso receiver.IsPropertyOrXmlPropertyAccess() Then
                        receiver = MakeRValue(receiver, diagnostics)
                    End If
                Else
                    receiver = UpdateReceiverForExtensionMethodOrPropertyGroup(receiver, [property].ReceiverType, reducedFrom.Parameters(0), diagnostics)
                End If

                ' Remove receiver from the property group
                ' NOTE: we only remove it if we pass it to a new BoundPropertyAccess node, 
                '       otherwise we keep it in the group to support semantic queries
                Dim propertyGroup = DirectCast(group, BoundPropertyGroup)
                Dim newReceiver As BoundExpression = If(receiver IsNot Nothing, Nothing, If(resolvedTypeOrValueReceiver, propertyGroup.ReceiverOpt))
                propertyGroup = propertyGroup.Update(propertyGroup.Properties,
                                                     propertyGroup.ResultKind,
                                                     newReceiver,
                                                     propertyGroup.QualificationKind)

                Return New BoundPropertyAccess(
                    node,
                    [property],
                    propertyGroup,
                    PropertyAccessKind.Unknown,
                    [property].IsWritable(receiver, Me),
                    receiver,
                    boundArguments,
                    argumentInfo.DefaultArguments,
                    hasErrors:=hasErrors)
            End If

        End Function

        Friend Sub WarnOnRecursiveAccess(propertyAccess As BoundPropertyAccess, accessKind As PropertyAccessKind, diagnostics As DiagnosticBag)
            Dim [property] As PropertySymbol = propertyAccess.PropertySymbol

            If [property].ReducedFromDefinition Is Nothing AndAlso [property].ParameterCount = 0 AndAlso
               ([property].IsShared OrElse (propertyAccess.ReceiverOpt IsNot Nothing AndAlso propertyAccess.ReceiverOpt.Kind = BoundKind.MeReference)) Then

                Dim reportRecursiveCall As Boolean = False

                If [property].GetMethod Is ContainingMember Then
                    If (accessKind And PropertyAccessKind.Get) <> 0 AndAlso (propertyAccess.AccessKind And PropertyAccessKind.Get) = 0 Then
                        reportRecursiveCall = True
                    End If
                ElseIf [property].SetMethod Is ContainingMember Then
                    If (accessKind And PropertyAccessKind.Set) <> 0 AndAlso (propertyAccess.AccessKind And PropertyAccessKind.Set) = 0 Then
                        reportRecursiveCall = True
                    End If
                End If

                If reportRecursiveCall Then
                    ReportDiagnostic(diagnostics, propertyAccess.Syntax, ERRID.WRN_RecursivePropertyCall, [property])
                End If
            End If
        End Sub

        Friend Sub WarnOnRecursiveAccess(node As BoundExpression, accessKind As PropertyAccessKind, diagnostics As DiagnosticBag)
            Select Case node.Kind
                Case BoundKind.XmlMemberAccess
                    ' Nothing to do 

                Case BoundKind.PropertyAccess
                    WarnOnRecursiveAccess(DirectCast(node, BoundPropertyAccess), accessKind, diagnostics)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)

            End Select
        End Sub

        Private Function UpdateReceiverForExtensionMethodOrPropertyGroup(
            receiver As BoundExpression,
            targetType As TypeSymbol,
            thisParameterDefinition As ParameterSymbol,
            diagnostics As DiagnosticBag
        ) As BoundExpression

            If receiver IsNot Nothing AndAlso
                receiver.IsValue AndAlso
                Not targetType.IsErrorType() AndAlso
                Not receiver.Type.IsErrorType() Then

                Dim oldReceiver As BoundExpression = receiver
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                receiver = PassArgument(receiver,
                                        Conversions.ClassifyConversion(receiver, targetType, Me, useSiteDiagnostics),
                                        False,
                                        Conversions.ClassifyConversion(targetType, receiver.Type, useSiteDiagnostics),
                                        targetType,
                                        thisParameterDefinition,
                                        diagnostics)

                diagnostics.Add(receiver, useSiteDiagnostics)

                If oldReceiver.WasCompilerGenerated AndAlso receiver IsNot oldReceiver Then
                    Select Case oldReceiver.Kind
                        Case BoundKind.MeReference,
                             BoundKind.WithLValueExpressionPlaceholder,
                             BoundKind.WithRValueExpressionPlaceholder
                            receiver.SetWasCompilerGenerated()
                    End Select
                End If
            End If

            Return receiver
        End Function

        Private Function IsWellKnownTypeMember(memberId As WellKnownMember, method As MethodSymbol) As Boolean
            Return Compilation.GetWellKnownTypeMember(memberId) Is method
        End Function

        ''' <summary>
        ''' Optimizes some runtime library calls through replacing them with a literal if possible.
        ''' VB Spec 11.2 defines the following runtime functions as being constant:
        '''  - Microsoft.VisualBasic.Strings.ChrW
        '''  - Microsoft.VisualBasic.Strings.Chr, if the constant value is between 0 and 128
        '''  - Microsoft.VisualBasic.Strings.AscW, if the constant string is not empty
        '''  - Microsoft.VisualBasic.Strings.Asc, if the constant string is not empty
        ''' </summary>
        ''' <param name="method">The method.</param>
        ''' <param name="arguments">The arguments of the method call.</param>
        ''' <param name="syntax">The syntax node for report errors.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        ''' <param name="hasErrors">Set to true if there are conversion errors (e.g. Asc("")). Otherwise it's not written to.</param>
        ''' <returns>The constant value that replaces this node, or nothing.</returns>
        Private Function OptimizeLibraryCall(
            method As MethodSymbol,
            arguments As ImmutableArray(Of BoundExpression),
            syntax As SyntaxNode,
            ByRef hasErrors As Boolean,
            diagnostics As DiagnosticBag
        ) As ConstantValue

            ' cheapest way to filter out methods that do not match
            If arguments.Length = 1 AndAlso arguments(0).IsConstant AndAlso Not arguments(0).ConstantValueOpt.IsBad Then

                ' only continue checking if containing type is Microsoft.VisualBasic.Strings
                If Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_Strings) IsNot method.ContainingType Then
                    Return Nothing
                End If

                ' AscW(char) / AscW(String)
                ' all values can be optimized as a literal, except an empty string that produces a diagnostic
                If IsWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscWCharInt32, method) OrElse
                    IsWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscWStringInt32, method) Then

                    Dim argumentConstantValue = arguments(0).ConstantValueOpt
                    Dim argumentValue As String
                    If argumentConstantValue.IsNull Then
                        argumentValue = String.Empty
                    ElseIf argumentConstantValue.IsChar Then
                        argumentValue = argumentConstantValue.CharValue
                    Else
                        Debug.Assert(argumentConstantValue.IsString())
                        argumentValue = argumentConstantValue.StringValue
                    End If

                    If argumentValue.IsEmpty() Then
                        ReportDiagnostic(diagnostics, syntax, ERRID.ERR_CannotConvertValue2, argumentValue, method.ReturnType)

                        hasErrors = True
                        Return Nothing
                    End If

                    Return ConstantValue.Create(AscW(argumentValue))
                End If

                ' ChrW
                ' for -32768 < value or value > 65535 we show a diagnostic
                If IsWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__ChrWInt32Char, method) Then
                    Dim argumentValue = arguments(0).ConstantValueOpt.Int32Value
                    If argumentValue < -32768 OrElse argumentValue > 65535 Then
                        ReportDiagnostic(diagnostics, syntax, ERRID.ERR_CannotConvertValue2, argumentValue, method.ReturnType)

                        hasErrors = True
                        Return Nothing
                    End If

                    Return ConstantValue.Create(ChrW(argumentValue))
                End If

                ' Asc(Char) / Asc(String)
                ' all values from 0 to 127 can be optimized to a literal.
                If IsWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscCharInt32, method) OrElse
                    IsWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscStringInt32, method) Then

                    Dim constantValue = arguments(0).ConstantValueOpt
                    Dim argumentValue As String
                    If constantValue.IsNull Then
                        argumentValue = String.Empty
                    ElseIf constantValue.IsChar Then
                        argumentValue = constantValue.CharValue
                    Else
                        Debug.Assert(constantValue.IsString())
                        argumentValue = constantValue.StringValue
                    End If

                    If argumentValue.IsEmpty() Then
                        ReportDiagnostic(diagnostics, syntax, ERRID.ERR_CannotConvertValue2, argumentValue, method.ReturnType)

                        hasErrors = True
                        Return Nothing
                    End If

                    ' we are only folding 7bit ASCII chars, so it's ok to use AscW here, although this is the Asc folding.
                    Dim charValue = AscW(argumentValue)
                    If charValue < 128 Then
                        Return ConstantValue.Create(charValue)
                    End If

                    Return Nothing
                End If

                ' Chr
                ' values from 0 to 127 can be optimized as a literal
                ' for -32768 < value or value > 65535 we show a diagnostic
                If IsWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__ChrInt32Char, method) Then
                    Dim argumentValue = arguments(0).ConstantValueOpt.Int32Value
                    If argumentValue >= 0 AndAlso argumentValue < 128 Then
                        Return ConstantValue.Create(ChrW(argumentValue))
                    ElseIf argumentValue < -32768 OrElse argumentValue > 65535 Then
                        ReportDiagnostic(diagnostics, syntax, ERRID.ERR_CannotConvertValue2, argumentValue, method.ReturnType)

                        hasErrors = True
                        Return Nothing
                    End If
                End If
            End If

            Return Nothing
        End Function

        Private Function ReportOverloadResolutionFailureAndProduceBoundNode(
            node As SyntaxNode,
            group As BoundMethodOrPropertyGroup,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            <[In]> ByRef results As OverloadResolution.OverloadResolutionResult,
            diagnostics As DiagnosticBag,
            callerInfoOpt As SyntaxNode,
            Optional overrideCommonReturnType As TypeSymbol = Nothing,
            Optional queryMode As Boolean = False,
            Optional boundTypeExpression As BoundTypeExpression = Nothing,
            Optional representCandidateInDiagnosticsOpt As Symbol = Nothing,
            Optional diagnosticLocationOpt As Location = Nothing
        ) As BoundExpression
            Return ReportOverloadResolutionFailureAndProduceBoundNode(
                        node,
                        group.ResultKind,
                        boundArguments,
                        argumentNames,
                        results,
                        diagnostics,
                        callerInfoOpt,
                        group,
                        overrideCommonReturnType,
                        queryMode,
                        boundTypeExpression,
                        representCandidateInDiagnosticsOpt,
                        diagnosticLocationOpt)
        End Function

        Private Function ReportOverloadResolutionFailureAndProduceBoundNode(
            node As SyntaxNode,
            lookupResult As LookupResultKind,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            <[In]> ByRef results As OverloadResolution.OverloadResolutionResult,
            diagnostics As DiagnosticBag,
            callerInfoOpt As SyntaxNode,
            Optional groupOpt As BoundMethodOrPropertyGroup = Nothing,
            Optional overrideCommonReturnType As TypeSymbol = Nothing,
            Optional queryMode As Boolean = False,
            Optional boundTypeExpression As BoundTypeExpression = Nothing,
            Optional representCandidateInDiagnosticsOpt As Symbol = Nothing,
            Optional diagnosticLocationOpt As Location = Nothing
        ) As BoundExpression

            Dim bestCandidates = ArrayBuilder(Of OverloadResolution.CandidateAnalysisResult).GetInstance()
            Dim bestSymbols = ImmutableArray(Of Symbol).Empty

            Dim commonReturnType As TypeSymbol = GetSetOfTheBestCandidates(results, bestCandidates, bestSymbols)

            If overrideCommonReturnType IsNot Nothing Then
                commonReturnType = overrideCommonReturnType
            End If

            Dim result As BoundExpression = ReportOverloadResolutionFailureAndProduceBoundNode(
                node,
                lookupResult,
                bestCandidates,
                bestSymbols,
                commonReturnType,
                boundArguments,
                argumentNames,
                diagnostics,
                callerInfoOpt,
                groupOpt,
                Nothing,
                queryMode,
                boundTypeExpression,
                representCandidateInDiagnosticsOpt,
                diagnosticLocationOpt)

            bestCandidates.Free()

            Return result
        End Function

        Private Function ReportOverloadResolutionFailureAndProduceBoundNode(
            node As SyntaxNode,
            group As BoundMethodOrPropertyGroup,
            bestCandidates As ArrayBuilder(Of OverloadResolution.CandidateAnalysisResult),
            bestSymbols As ImmutableArray(Of Symbol),
            commonReturnType As TypeSymbol,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            diagnostics As DiagnosticBag,
            callerInfoOpt As SyntaxNode,
            Optional delegateSymbol As Symbol = Nothing,
            Optional queryMode As Boolean = False,
            Optional boundTypeExpression As BoundTypeExpression = Nothing,
            Optional representCandidateInDiagnosticsOpt As Symbol = Nothing
        ) As BoundExpression
            Return ReportOverloadResolutionFailureAndProduceBoundNode(
                       node,
                       group.ResultKind,
                       bestCandidates,
                       bestSymbols,
                       commonReturnType,
                       boundArguments,
                       argumentNames,
                       diagnostics,
                       callerInfoOpt,
                       group,
                       delegateSymbol,
                       queryMode,
                       boundTypeExpression,
                       representCandidateInDiagnosticsOpt)
        End Function

        Public Shared Function GetLocationForOverloadResolutionDiagnostic(node As SyntaxNode, Optional groupOpt As BoundMethodOrPropertyGroup = Nothing) As Location
            Dim result As SyntaxNode

            If groupOpt IsNot Nothing Then
                If node.SyntaxTree Is groupOpt.Syntax.SyntaxTree AndAlso node.Span.Contains(groupOpt.Syntax.Span) Then
                    result = groupOpt.Syntax

                    If result Is node AndAlso (groupOpt.ReceiverOpt Is Nothing OrElse groupOpt.ReceiverOpt.Syntax Is result) Then
                        Return result.GetLocation()
                    End If
                Else
                    Return node.GetLocation()
                End If

            ElseIf node.IsKind(SyntaxKind.InvocationExpression) Then
                result = If(DirectCast(node, InvocationExpressionSyntax).Expression, node)
            Else
                Return node.GetLocation()
            End If

            Select Case result.Kind
                Case SyntaxKind.QualifiedName
                    Return DirectCast(result, QualifiedNameSyntax).Right.GetLocation()

                Case SyntaxKind.SimpleMemberAccessExpression
                    If result.Parent IsNot Nothing AndAlso result.Parent.IsKind(SyntaxKind.AddressOfExpression) Then
                        Return result.GetLocation()
                    End If

                    Return DirectCast(result, MemberAccessExpressionSyntax).Name.GetLocation()

                Case SyntaxKind.XmlElementAccessExpression,
                     SyntaxKind.XmlDescendantAccessExpression,
                     SyntaxKind.XmlAttributeAccessExpression
                    Return DirectCast(result, XmlMemberAccessExpressionSyntax).Name.GetLocation()

                Case SyntaxKind.HandlesClauseItem
                    Return DirectCast(result, HandlesClauseItemSyntax).EventMember.GetLocation()
            End Select

            Return result.GetLocation()
        End Function

        Private Function ReportOverloadResolutionFailureAndProduceBoundNode(
            node As SyntaxNode,
            lookupResult As LookupResultKind,
            bestCandidates As ArrayBuilder(Of OverloadResolution.CandidateAnalysisResult),
            bestSymbols As ImmutableArray(Of Symbol),
            commonReturnType As TypeSymbol,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            diagnostics As DiagnosticBag,
            callerInfoOpt As SyntaxNode,
            Optional groupOpt As BoundMethodOrPropertyGroup = Nothing,
            Optional delegateSymbol As Symbol = Nothing,
            Optional queryMode As Boolean = False,
            Optional boundTypeExpression As BoundTypeExpression = Nothing,
            Optional representCandidateInDiagnosticsOpt As Symbol = Nothing,
            Optional diagnosticLocationOpt As Location = Nothing
        ) As BoundExpression

            Debug.Assert(commonReturnType IsNot Nothing AndAlso bestSymbols.Length > 0 AndAlso bestCandidates.Count >= bestSymbols.Length)
            Debug.Assert(groupOpt Is Nothing OrElse lookupResult = groupOpt.ResultKind)

            Dim state = OverloadResolution.CandidateAnalysisResultState.Count

            If bestCandidates.Count > 0 Then
                state = bestCandidates(0).State
            End If

            If boundArguments.IsDefault Then
                boundArguments = ImmutableArray(Of BoundExpression).Empty
            End If

            Dim singleCandidateAnalysisResult As OverloadResolution.CandidateAnalysisResult = Nothing
            Dim singleCandidate As OverloadResolution.Candidate = Nothing
            Dim allowUnexpandedParamArrayForm As Boolean = False
            Dim allowExpandedParamArrayForm As Boolean = False

            ' Figure out if we should report single candidate errors
            If bestSymbols.Length = 1 AndAlso bestCandidates.Count < 3 Then
                singleCandidateAnalysisResult = bestCandidates(0)
                singleCandidate = singleCandidateAnalysisResult.Candidate
                allowExpandedParamArrayForm = singleCandidateAnalysisResult.IsExpandedParamArrayForm
                allowUnexpandedParamArrayForm = Not allowExpandedParamArrayForm

                If bestCandidates.Count > 1 Then
                    If bestCandidates(1).IsExpandedParamArrayForm Then
                        allowExpandedParamArrayForm = True
                    Else
                        allowUnexpandedParamArrayForm = True
                    End If
                End If
            End If

            If lookupResult = LookupResultKind.Inaccessible Then
                If singleCandidate IsNot Nothing Then
                    ReportDiagnostic(diagnostics, If(groupOpt IsNot Nothing, groupOpt.Syntax, node), GetInaccessibleErrorInfo(singleCandidate.UnderlyingSymbol, useSiteDiagnostics:=Nothing))
                Else
                    If Not queryMode Then
                        ReportDiagnostic(diagnostics, If(groupOpt IsNot Nothing, groupOpt.Syntax, node), ERRID.ERR_NoViableOverloadCandidates1, bestSymbols(0).Name)
                    End If

                    ' Do not report more errors.
                    GoTo ProduceBoundNode
                End If
            Else
                Debug.Assert(lookupResult = LookupResultKind.Good)
            End If

            If diagnosticLocationOpt Is Nothing Then
                diagnosticLocationOpt = GetLocationForOverloadResolutionDiagnostic(node, groupOpt)
            End If

            ' Report diagnostic according to the state of candidates
            Select Case state

                Case VisualBasic.OverloadResolution.CandidateAnalysisResultState.HasUseSiteError, OverloadResolution.CandidateAnalysisResultState.HasUnsupportedMetadata

                    If singleCandidate IsNot Nothing Then
                        ReportOverloadResolutionFailureForASingleCandidate(node, diagnosticLocationOpt, lookupResult, singleCandidateAnalysisResult,
                                                                 boundArguments, argumentNames,
                                                                 allowUnexpandedParamArrayForm,
                                                                 allowExpandedParamArrayForm,
                                                                 True,
                                                                 False,
                                                                 diagnostics,
                                                                 delegateSymbol:=delegateSymbol,
                                                                 queryMode:=queryMode,
                                                                 callerInfoOpt:=callerInfoOpt,
                                                                 representCandidateInDiagnosticsOpt:=representCandidateInDiagnosticsOpt)

                    Else
                        ReportOverloadResolutionFailureForASetOfCandidates(node, diagnosticLocationOpt, lookupResult,
                                                        ERRID.ERR_BadOverloadCandidates2,
                                                        bestCandidates,
                                                        boundArguments,
                                                        argumentNames,
                                                        diagnostics,
                                                        delegateSymbol:=delegateSymbol,
                                                        queryMode:=queryMode,
                                                        callerInfoOpt:=callerInfoOpt)
                    End If

                Case VisualBasic.OverloadResolution.CandidateAnalysisResultState.Ambiguous
                    Dim candidate As Symbol = bestSymbols(0).OriginalDefinition
                    Dim container As Symbol = candidate.ContainingSymbol
                    ReportDiagnostic(diagnostics, diagnosticLocationOpt, ERRID.ERR_MetadataMembersAmbiguous3, candidate.Name, container.GetKindText(), container)

                Case OverloadResolution.CandidateAnalysisResultState.BadGenericArity
                    Debug.Assert(groupOpt IsNot Nothing AndAlso groupOpt.Kind = BoundKind.MethodGroup)
                    Dim mg = DirectCast(groupOpt, BoundMethodGroup)

                    If singleCandidate IsNot Nothing Then
                        Dim typeArguments = If(mg.TypeArgumentsOpt IsNot Nothing, mg.TypeArgumentsOpt.Arguments, ImmutableArray(Of TypeSymbol).Empty)

                        If typeArguments.IsDefault Then
                            typeArguments = ImmutableArray(Of TypeSymbol).Empty
                        End If

                        Dim singleSymbol As Symbol = singleCandidate.UnderlyingSymbol
                        Dim isExtension As Boolean = singleCandidate.IsExtensionMethod

                        If singleCandidate.Arity < typeArguments.Length Then
                            If isExtension Then
                                ReportDiagnostic(diagnostics, mg.TypeArgumentsOpt.Syntax,
                                                 If(singleCandidate.Arity = 0, ERRID.ERR_TypeOrMemberNotGeneric2, ERRID.ERR_TooManyGenericArguments2),
                                                 singleSymbol, singleSymbol.ContainingType)
                            Else
                                ReportDiagnostic(diagnostics, mg.TypeArgumentsOpt.Syntax,
                                                 If(singleCandidate.Arity = 0, ERRID.ERR_TypeOrMemberNotGeneric1, ERRID.ERR_TooManyGenericArguments1),
                                                 singleSymbol)
                            End If
                        Else
                            Debug.Assert(singleCandidate.Arity > typeArguments.Length)

                            If isExtension Then
                                ReportDiagnostic(diagnostics, mg.TypeArgumentsOpt.Syntax,
                                                 ERRID.ERR_TooFewGenericArguments2,
                                                 singleSymbol, singleSymbol.ContainingType)
                            Else
                                ReportDiagnostic(diagnostics, mg.TypeArgumentsOpt.Syntax,
                                                 ERRID.ERR_TooFewGenericArguments1, singleSymbol)
                            End If
                        End If
                    Else
                        ReportDiagnostic(diagnostics, diagnosticLocationOpt, ERRID.ERR_NoTypeArgumentCountOverloadCand1, CustomSymbolDisplayFormatter.ShortErrorName(bestSymbols(0)))
                    End If

                Case OverloadResolution.CandidateAnalysisResultState.ArgumentCountMismatch

                    If node.Kind = SyntaxKind.IdentifierName AndAlso
                        node.Parent IsNot Nothing AndAlso
                        node.Parent.Kind = SyntaxKind.NamedFieldInitializer AndAlso
                        groupOpt IsNot Nothing AndAlso
                        groupOpt.Kind = BoundKind.PropertyGroup Then

                        ' report special diagnostics for a failed overload resolution because all available properties
                        ' require arguments in case this method was called while binding a object member initializer.
                        ReportDiagnostic(diagnostics,
                                         diagnosticLocationOpt,
                                         If(singleCandidate IsNot Nothing,
                                            ERRID.ERR_ParameterizedPropertyInAggrInit1,
                                            ERRID.ERR_NoZeroCountArgumentInitCandidates1),
                                         CustomSymbolDisplayFormatter.ShortErrorName(bestSymbols(0)))
                    Else
                        If singleCandidate IsNot Nothing AndAlso
                           (Not queryMode OrElse singleCandidate.ParameterCount <= boundArguments.Length) Then
                            ReportOverloadResolutionFailureForASingleCandidate(node, diagnosticLocationOpt, lookupResult, singleCandidateAnalysisResult,
                                                                         boundArguments, argumentNames,
                                                                         allowUnexpandedParamArrayForm,
                                                                         allowExpandedParamArrayForm,
                                                                         True,
                                                                         False,
                                                                         diagnostics,
                                                                         delegateSymbol:=delegateSymbol,
                                                                         queryMode:=queryMode,
                                                                         callerInfoOpt:=callerInfoOpt,
                                                                         representCandidateInDiagnosticsOpt:=representCandidateInDiagnosticsOpt)

                        Else
                            ReportDiagnostic(diagnostics, diagnosticLocationOpt, ERRID.ERR_NoArgumentCountOverloadCandidates1, CustomSymbolDisplayFormatter.ShortErrorName(bestSymbols(0)))
                        End If
                    End If

                Case OverloadResolution.CandidateAnalysisResultState.ArgumentMismatch,
                     OverloadResolution.CandidateAnalysisResultState.GenericConstraintsViolated

                    Dim haveBadArgument As Boolean = False

                    For i As Integer = 0 To boundArguments.Length - 1 Step 1
                        Dim type = boundArguments(i).Type

                        If boundArguments(i).HasErrors OrElse (type IsNot Nothing AndAlso type.IsErrorType()) Then
                            haveBadArgument = True
                            Exit For
                        End If
                    Next

                    If Not haveBadArgument Then
                        If singleCandidate IsNot Nothing Then
                            ReportOverloadResolutionFailureForASingleCandidate(node, diagnosticLocationOpt, lookupResult, singleCandidateAnalysisResult,
                                                                     boundArguments, argumentNames,
                                                                     allowUnexpandedParamArrayForm,
                                                                     allowExpandedParamArrayForm,
                                                                     True,
                                                                     False,
                                                                     diagnostics,
                                                                     delegateSymbol:=delegateSymbol,
                                                                     queryMode:=queryMode,
                                                                     callerInfoOpt:=callerInfoOpt,
                                                                     representCandidateInDiagnosticsOpt:=representCandidateInDiagnosticsOpt)

                        Else
                            ReportOverloadResolutionFailureForASetOfCandidates(node, diagnosticLocationOpt, lookupResult,
                                                            If(delegateSymbol Is Nothing,
                                                               ERRID.ERR_NoCallableOverloadCandidates2,
                                                               ERRID.ERR_DelegateBindingFailure3),
                                                            bestCandidates,
                                                            boundArguments,
                                                            argumentNames,
                                                            diagnostics,
                                                            delegateSymbol:=delegateSymbol,
                                                            queryMode:=queryMode,
                                                            callerInfoOpt:=callerInfoOpt)
                        End If
                    End If

                Case OverloadResolution.CandidateAnalysisResultState.TypeInferenceFailed

                    If singleCandidate IsNot Nothing Then
                        ReportOverloadResolutionFailureForASingleCandidate(node, diagnosticLocationOpt, lookupResult, singleCandidateAnalysisResult,
                                                                 boundArguments, argumentNames,
                                                                 allowUnexpandedParamArrayForm,
                                                                 allowExpandedParamArrayForm,
                                                                 True,
                                                                 False,
                                                                 diagnostics,
                                                                 delegateSymbol:=delegateSymbol,
                                                                 queryMode:=queryMode,
                                                                 callerInfoOpt:=callerInfoOpt,
                                                                 representCandidateInDiagnosticsOpt:=representCandidateInDiagnosticsOpt)

                    Else
                        ReportOverloadResolutionFailureForASetOfCandidates(node, diagnosticLocationOpt, lookupResult,
                                                        ERRID.ERR_NoCallableOverloadCandidates2,
                                                        bestCandidates,
                                                        boundArguments,
                                                        argumentNames,
                                                        diagnostics,
                                                        delegateSymbol:=delegateSymbol,
                                                        queryMode:=queryMode,
                                                        callerInfoOpt:=callerInfoOpt)
                    End If

                Case OverloadResolution.CandidateAnalysisResultState.Applicable

                    ' it is only possible to get overloading failure with a single candidate
                    ' if we have a paramarray with equally specific virtual signatures
                    Debug.Assert(singleCandidate Is Nothing OrElse
                                 singleCandidate.ParameterCount <> 0 AndAlso
                                 singleCandidate.Parameters(singleCandidate.ParameterCount - 1).IsParamArray)

                    If bestCandidates(0).RequiresNarrowingConversion Then
                        ReportOverloadResolutionFailureForASetOfCandidates(node, diagnosticLocationOpt, lookupResult,
                                                        If(delegateSymbol Is Nothing,
                                                            ERRID.ERR_NoNonNarrowingOverloadCandidates2,
                                                            ERRID.ERR_DelegateBindingFailure3),
                                                        bestCandidates,
                                                        boundArguments,
                                                        argumentNames,
                                                        diagnostics,
                                                        delegateSymbol:=delegateSymbol,
                                                        queryMode:=queryMode,
                                                        callerInfoOpt:=callerInfoOpt)
                    Else
                        ReportUnspecificProcedures(diagnosticLocationOpt, bestSymbols, diagnostics, (delegateSymbol IsNot Nothing))
                    End If

                Case Else
                    ' Unexpected
                    Throw ExceptionUtilities.UnexpectedValue(state)
            End Select

ProduceBoundNode:
            Dim childBoundNodes As ImmutableArray(Of BoundExpression)

            If boundArguments.IsEmpty AndAlso boundTypeExpression Is Nothing Then
                If groupOpt Is Nothing Then
                    childBoundNodes = ImmutableArray(Of BoundExpression).Empty
                Else
                    childBoundNodes = ImmutableArray.Create(Of BoundExpression)(groupOpt)
                End If
            Else
                Dim builder = ArrayBuilder(Of BoundExpression).GetInstance()

                If groupOpt IsNot Nothing Then
                    builder.Add(groupOpt)
                End If

                If Not boundArguments.IsEmpty Then
                    builder.AddRange(boundArguments)
                End If

                If boundTypeExpression IsNot Nothing Then
                    builder.Add(boundTypeExpression)
                End If

                childBoundNodes = builder.ToImmutableAndFree()
            End If

            Dim resultKind = LookupResultKind.OverloadResolutionFailure
            If lookupResult < resultKind Then
                resultKind = lookupResult
            End If

            Return New BoundBadExpression(node, resultKind, bestSymbols, childBoundNodes, commonReturnType, hasErrors:=True)
        End Function

        ''' <summary>
        '''Figure out the set of best candidates in the following preference order:
        '''  1) Applicable
        '''  2) ArgumentMismatch, GenericConstraintsViolated
        '''  3) TypeInferenceFailed
        '''  4) ArgumentCountMismatch
        '''  5) BadGenericArity
        '''  6) Ambiguous
        '''  7) HasUseSiteError
        '''  8) HasUnsupportedMetadata
        ''' 
        ''' Also return the set of unique symbols behind the set.
        ''' 
        ''' Returns type symbol for the common type, if any.
        ''' Otherwise returns ErrorTypeSymbol.UnknownResultType.
        ''' </summary>
        Private Shared Function GetSetOfTheBestCandidates(
            ByRef results As OverloadResolution.OverloadResolutionResult,
            bestCandidates As ArrayBuilder(Of OverloadResolution.CandidateAnalysisResult),
            ByRef bestSymbols As ImmutableArray(Of Symbol)
        ) As TypeSymbol
            Const Applicable = OverloadResolution.CandidateAnalysisResultState.Applicable
            Const ArgumentMismatch = OverloadResolution.CandidateAnalysisResultState.ArgumentMismatch
            Const GenericConstraintsViolated = OverloadResolution.CandidateAnalysisResultState.GenericConstraintsViolated
            Const TypeInferenceFailed = OverloadResolution.CandidateAnalysisResultState.TypeInferenceFailed
            Const ArgumentCountMismatch = OverloadResolution.CandidateAnalysisResultState.ArgumentCountMismatch
            Const BadGenericArity = OverloadResolution.CandidateAnalysisResultState.BadGenericArity
            Const Ambiguous = OverloadResolution.CandidateAnalysisResultState.Ambiguous
            Const HasUseSiteError = OverloadResolution.CandidateAnalysisResultState.HasUseSiteError
            Const HasUnsupportedMetadata = OverloadResolution.CandidateAnalysisResultState.HasUnsupportedMetadata

            Dim preference(OverloadResolution.CandidateAnalysisResultState.Count - 1) As Integer

            preference(Applicable) = 1
            preference(ArgumentMismatch) = 2
            preference(GenericConstraintsViolated) = 2
            preference(TypeInferenceFailed) = 3
            preference(ArgumentCountMismatch) = 4
            preference(BadGenericArity) = 5
            preference(Ambiguous) = 6
            preference(HasUseSiteError) = 7
            preference(HasUnsupportedMetadata) = 8

            For Each candidate In results.Candidates
                Dim prefNew = preference(candidate.State)

                If prefNew <> 0 Then
                    If bestCandidates.Count = 0 Then
                        bestCandidates.Add(candidate)
                    Else
                        Dim prefOld = preference(bestCandidates(0).State)

                        If prefNew = prefOld Then
                            bestCandidates.Add(candidate)

                        ElseIf prefNew < prefOld Then
                            bestCandidates.Clear()
                            bestCandidates.Add(candidate)
                        End If
                    End If
                End If
            Next

            ' Collect unique best symbols.
            Dim bestSymbolsBuilder = ArrayBuilder(Of Symbol).GetInstance(bestCandidates.Count)
            Dim commonReturnType As TypeSymbol = Nothing

            If bestCandidates.Count = 1 Then
                ' For multiple candidates we never pick common type that refers to method's type parameter
                ' because each method has distinct type parameters. For single candidate case we need to
                ' ensure this explicitly. 
                Dim underlyingSymbol As Symbol = bestCandidates(0).Candidate.UnderlyingSymbol
                bestSymbolsBuilder.Add(underlyingSymbol)
                commonReturnType = bestCandidates(0).Candidate.ReturnType

                If underlyingSymbol.Kind = SymbolKind.Method Then
                    Dim method = DirectCast(underlyingSymbol, MethodSymbol)

                    If method.IsGenericMethod AndAlso commonReturnType.ReferencesMethodsTypeParameter(method) Then
                        Select Case CInt(bestCandidates(0).State)
                            Case TypeInferenceFailed, HasUseSiteError, HasUnsupportedMetadata, BadGenericArity, ArgumentCountMismatch
                                commonReturnType = Nothing
                        End Select
                    End If
                End If
            Else
                For i As Integer = 0 To bestCandidates.Count - 1 Step 1
                    If i = 0 OrElse Not bestSymbolsBuilder(bestSymbolsBuilder.Count - 1).Equals(bestCandidates(i).Candidate.UnderlyingSymbol) Then
                        bestSymbolsBuilder.Add(bestCandidates(i).Candidate.UnderlyingSymbol)

                        Dim returnType = bestCandidates(i).Candidate.ReturnType

                        If commonReturnType Is Nothing Then
                            commonReturnType = returnType

                        ElseIf commonReturnType IsNot ErrorTypeSymbol.UnknownResultType AndAlso
                            Not commonReturnType.IsSameTypeIgnoringAll(returnType) Then
                            commonReturnType = ErrorTypeSymbol.UnknownResultType
                        End If
                    End If
                Next
            End If

            bestSymbols = bestSymbolsBuilder.ToImmutableAndFree()

            Return If(commonReturnType, ErrorTypeSymbol.UnknownResultType)
        End Function


        Private Shared Sub ReportUnspecificProcedures(
            diagnosticLocation As Location,
            bestSymbols As ImmutableArray(Of Symbol),
            diagnostics As DiagnosticBag,
            isDelegateContext As Boolean
        )
            Dim diagnosticInfos = ArrayBuilder(Of DiagnosticInfo).GetInstance(bestSymbols.Length)
            Dim notMostSpecificMessage = ErrorFactory.ErrorInfo(ERRID.ERR_NotMostSpecificOverload)
            Dim withContainingTypeInDiagnostics As Boolean = False

            If Not bestSymbols(0).IsReducedExtensionMethod Then
                Dim container As NamedTypeSymbol = bestSymbols(0).ContainingType

                For i As Integer = 1 To bestSymbols.Length - 1 Step 1
                    If bestSymbols(i).ContainingType <> container Then
                        withContainingTypeInDiagnostics = True
                    End If
                Next
            End If

            For i As Integer = 0 To bestSymbols.Length - 1 Step 1

                ' in delegate context we just output for each candidates
                ' BC30794: No accessible 'goo' is most specific: 
                '     Public Sub goo(p As Integer)
                '     Public Sub goo(p As Integer)
                '
                ' in other contexts we give more information, e.g.
                ' BC30794: No accessible 'goo' is most specific: 
                '     Public Sub goo(p As Integer): <reason>
                '     Public Sub goo(p As Integer): <reason>
                Dim bestSymbol As Symbol = bestSymbols(i)
                Dim bestSymbolIsExtension As Boolean = bestSymbol.IsReducedExtensionMethod

                If isDelegateContext Then
                    If bestSymbolIsExtension Then
                        diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionMethodOverloadCandidate2, bestSymbol, bestSymbol.ContainingType))
                    ElseIf withContainingTypeInDiagnostics Then
                        diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadCandidate1, CustomSymbolDisplayFormatter.WithContainingType(bestSymbol)))
                    Else
                        diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadCandidate1, bestSymbol))
                    End If
                Else
                    If bestSymbolIsExtension Then
                        diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionMethodOverloadCandidate3, bestSymbol, bestSymbol.ContainingType, notMostSpecificMessage))
                    ElseIf withContainingTypeInDiagnostics Then
                        diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadCandidate2, CustomSymbolDisplayFormatter.WithContainingType(bestSymbol), notMostSpecificMessage))
                    Else
                        diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadCandidate2, bestSymbol, notMostSpecificMessage))
                    End If
                End If
            Next

            ReportDiagnostic(diagnostics, diagnosticLocation,
                             ErrorFactory.ErrorInfo(If(isDelegateContext, ERRID.ERR_AmbiguousDelegateBinding2, ERRID.ERR_NoMostSpecificOverload2),
                                                    CustomSymbolDisplayFormatter.ShortErrorName(bestSymbols(0)),
                                                    New CompoundDiagnosticInfo(diagnosticInfos.ToArrayAndFree())
                                                    ))
        End Sub



        Private Sub ReportOverloadResolutionFailureForASetOfCandidates(
            node As SyntaxNode,
            diagnosticLocation As Location,
            lookupResult As LookupResultKind,
            errorNo As ERRID,
            candidates As ArrayBuilder(Of OverloadResolution.CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            diagnostics As DiagnosticBag,
            delegateSymbol As Symbol,
            queryMode As Boolean,
            callerInfoOpt As SyntaxNode
        )
            Dim diagnosticPerSymbol = ArrayBuilder(Of KeyValuePair(Of Symbol, ImmutableArray(Of Diagnostic))).GetInstance(candidates.Count)

            If arguments.IsDefault Then
                arguments = ImmutableArray(Of BoundExpression).Empty
            End If

            For i As Integer = 0 To candidates.Count - 1 Step 1

                ' See if we need to consider both expanded and unexpanded version of the same method.
                ' We want to report only one set of errors in this case.
                ' Note, that, when OverloadResolution collects candidates expanded form always 
                ' immediately follows unexpanded form, if both should be considered.
                Dim allowExpandedParamArrayForm As Boolean = candidates(i).IsExpandedParamArrayForm
                Dim allowUnexpandedParamArrayForm As Boolean = Not allowExpandedParamArrayForm

                If allowUnexpandedParamArrayForm AndAlso i + 1 < candidates.Count AndAlso
                   candidates(i + 1).IsExpandedParamArrayForm AndAlso
                   candidates(i + 1).Candidate.UnderlyingSymbol.Equals(candidates(i).Candidate.UnderlyingSymbol) Then
                    allowExpandedParamArrayForm = True
                    i += 1
                End If

                Dim candidateDiagnostics = DiagnosticBag.GetInstance()

                ' Collect diagnostic for this candidate
                ReportOverloadResolutionFailureForASingleCandidate(node, diagnosticLocation, lookupResult, candidates(i), arguments, argumentNames,
                                                         allowUnexpandedParamArrayForm, allowExpandedParamArrayForm,
                                                         False,
                                                         errorNo = If(delegateSymbol Is Nothing, ERRID.ERR_NoNonNarrowingOverloadCandidates2, ERRID.ERR_DelegateBindingFailure3),
                                                         candidateDiagnostics,
                                                         delegateSymbol:=delegateSymbol,
                                                         queryMode:=queryMode,
                                                         callerInfoOpt:=callerInfoOpt,
                                                         representCandidateInDiagnosticsOpt:=Nothing)

                diagnosticPerSymbol.Add(KeyValuePair.Create(candidates(i).Candidate.UnderlyingSymbol, candidateDiagnostics.ToReadOnlyAndFree()))

            Next

            ' See if there are errors that are reported for each candidate at the same location within a lambda argument.  
            ' Report them and don't report remaining diagnostics for each symbol separately.
            If Not ReportCommonErrorsFromLambdas(diagnosticPerSymbol, arguments, diagnostics) Then
                Dim diagnosticInfos = ArrayBuilder(Of DiagnosticInfo).GetInstance(candidates.Count)

                For i As Integer = 0 To diagnosticPerSymbol.Count - 1
                    Dim symbol = diagnosticPerSymbol(i).Key
                    Dim isExtension As Boolean = symbol.IsReducedExtensionMethod()

                    Dim sealedCandidateDiagnostics = diagnosticPerSymbol(i).Value

                    ' When reporting errors for an AddressOf, Dev 10 shows different error messages depending on how many
                    ' errors there are per candidate.
                    ' One narrowing error will be shown like:
                    '     'Public Sub goo6(p As Integer, p2 As Byte)': Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
                    ' More than one narrowing issues in the parameters are abbreviated with:
                    '     'Public Sub goo6(p As Byte, p2 As Byte)': Method does not have a signature compatible with the delegate.

                    If delegateSymbol Is Nothing OrElse Not sealedCandidateDiagnostics.Skip(1).Any() Then
                        If isExtension Then
                            For Each iDiagnostic In sealedCandidateDiagnostics
                                diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionMethodOverloadCandidate3,
                                                                       symbol, symbol.ContainingType, DirectCast(iDiagnostic, DiagnosticWithInfo).Info))
                            Next
                        Else
                            For Each iDiagnostic In sealedCandidateDiagnostics
                                Dim msg = VisualBasicDiagnosticFormatter.Instance.Format(iDiagnostic.WithLocation(Location.None))
                                diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadCandidate2, symbol, DirectCast(iDiagnostic, DiagnosticWithInfo).Info))
                            Next
                        End If
                    Else
                        If isExtension Then
                            diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionMethodOverloadCandidate3,
                                                                   symbol, symbol.ContainingType,
                                                                   ErrorFactory.ErrorInfo(ERRID.ERR_DelegateBindingMismatch, symbol)))
                        Else
                            diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadCandidate2,
                                                                   symbol,
                                                                   ErrorFactory.ErrorInfo(ERRID.ERR_DelegateBindingMismatch, symbol)))
                        End If
                    End If
                Next

                Dim diagnosticCompoundInfos() As DiagnosticInfo = diagnosticInfos.ToArrayAndFree()
                If delegateSymbol Is Nothing Then
                    ReportDiagnostic(diagnostics, diagnosticLocation,
                                 ErrorFactory.ErrorInfo(errorNo, CustomSymbolDisplayFormatter.ShortErrorName(candidates(0).Candidate.UnderlyingSymbol),
                                                        New CompoundDiagnosticInfo(diagnosticCompoundInfos)))
                Else
                    ReportDiagnostic(diagnostics, diagnosticLocation,
                                 ErrorFactory.ErrorInfo(errorNo, CustomSymbolDisplayFormatter.ShortErrorName(candidates(0).Candidate.UnderlyingSymbol),
                                                        CustomSymbolDisplayFormatter.DelegateSignature(delegateSymbol),
                                                        New CompoundDiagnosticInfo(diagnosticCompoundInfos)))
                End If
            End If

            diagnosticPerSymbol.Free()
        End Sub

        Private Shared Function ReportCommonErrorsFromLambdas(
            diagnosticPerSymbol As ArrayBuilder(Of KeyValuePair(Of Symbol, ImmutableArray(Of Diagnostic))),
            arguments As ImmutableArray(Of BoundExpression),
            diagnostics As DiagnosticBag
        ) As Boolean
            Dim haveCommonErrors As Boolean = False

            For Each diagnostic In diagnosticPerSymbol(0).Value
                If diagnostic.Severity <> DiagnosticSeverity.Error Then
                    Continue For
                End If

                For Each argument In arguments
                    If argument.Syntax.SyntaxTree Is diagnostic.Location.SourceTree AndAlso
                       argument.Kind = BoundKind.UnboundLambda Then
                        If argument.Syntax.Span.Contains(diagnostic.Location.SourceSpan) Then
                            Dim common As Boolean = True
                            For i As Integer = 1 To diagnosticPerSymbol.Count - 1
                                If Not diagnosticPerSymbol(i).Value.Contains(diagnostic) Then
                                    common = False
                                    Exit For
                                End If
                            Next

                            If common Then
                                haveCommonErrors = True
                                diagnostics.Add(diagnostic)
                            End If

                            Exit For
                        End If
                    End If
                Next
            Next

            Return haveCommonErrors
        End Function

        ''' <summary>
        ''' Should be kept in sync with OverloadResolution.MatchArguments. Anything that 
        ''' OverloadResolution.MatchArguments flags as an error should be detected by 
        ''' this function as well. 
        ''' </summary>
        Private Sub ReportOverloadResolutionFailureForASingleCandidate(
            node As SyntaxNode,
            diagnosticLocation As Location,
            lookupResult As LookupResultKind,
            ByRef candidateAnalysisResult As OverloadResolution.CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            allowUnexpandedParamArrayForm As Boolean,
            allowExpandedParamArrayForm As Boolean,
            includeMethodNameInErrorMessages As Boolean,
            reportNarrowingConversions As Boolean,
            diagnostics As DiagnosticBag,
            delegateSymbol As Symbol,
            queryMode As Boolean,
            callerInfoOpt As SyntaxNode,
            representCandidateInDiagnosticsOpt As Symbol
        )
            Dim candidate As OverloadResolution.Candidate = candidateAnalysisResult.Candidate

            If arguments.IsDefault Then
                arguments = ImmutableArray(Of BoundExpression).Empty
            End If

            Debug.Assert(argumentNames.IsDefaultOrEmpty OrElse (argumentNames.Length > 0 AndAlso argumentNames.Length = arguments.Length))
            Debug.Assert(allowUnexpandedParamArrayForm OrElse allowExpandedParamArrayForm)

            If candidateAnalysisResult.State = VisualBasic.OverloadResolution.CandidateAnalysisResultState.HasUseSiteError OrElse
               candidateAnalysisResult.State = VisualBasic.OverloadResolution.CandidateAnalysisResultState.HasUnsupportedMetadata Then
                If lookupResult <> LookupResultKind.Inaccessible Then
                    Debug.Assert(lookupResult = LookupResultKind.Good)
                    ReportDiagnostic(diagnostics, diagnosticLocation, candidate.UnderlyingSymbol.GetUseSiteErrorInfo())
                End If

                Return
            End If

            ' To simplify following code
            If Not argumentNames.IsDefault AndAlso argumentNames.Length = 0 Then
                argumentNames = Nothing
            End If

            Dim parameterToArgumentMap As ArrayBuilder(Of Integer) = ArrayBuilder(Of Integer).GetInstance(candidate.ParameterCount, -1)
            Dim paramArrayItems As ArrayBuilder(Of Integer) = ArrayBuilder(Of Integer).GetInstance()

            Try
                '§11.8.2 Applicable Methods
                '1.	First, match each positional argument in order to the list of method parameters. 
                'If there are more positional arguments than parameters and the last parameter is not a paramarray, the method is not applicable. 
                'Otherwise, the paramarray parameter is expanded with parameters of the paramarray element type to match the number of positional arguments. 
                'If a positional argument is omitted, the method is not applicable.
                ' !!! Not sure about the last sentence: "If a positional argument is omitted, the method is not applicable."
                ' !!! Dev10 allows omitting positional argument as long as the corresponding parameter is optional.

                Dim positionalArguments As Integer = 0
                Dim paramIndex = 0
                Dim someArgumentsBad As Boolean = False
                Dim someParamArrayArgumentsBad As Boolean = False
                Dim seenOutOfPositionNamedArgIndex As Integer = -1

                Dim candidateSymbol As Symbol = candidate.UnderlyingSymbol
                Dim candidateIsExtension As Boolean = candidate.IsExtensionMethod

                For i As Integer = 0 To arguments.Length - 1 Step 1

                    ' A named argument which is used in-position counts as positional
                    If Not argumentNames.IsDefault AndAlso argumentNames(i) IsNot Nothing Then
                        If Not candidate.TryGetNamedParamIndex(argumentNames(i), paramIndex) Then
                            Exit For
                        End If

                        If paramIndex <> i Then
                            ' all remaining arguments must be named
                            seenOutOfPositionNamedArgIndex = i
                            Exit For
                        End If

                        If paramIndex = candidate.ParameterCount - 1 AndAlso candidate.Parameters(paramIndex).IsParamArray Then
                            Exit For
                        End If

                        Debug.Assert(parameterToArgumentMap(paramIndex) = -1)
                    End If

                    If paramIndex = candidate.ParameterCount Then
                        If Not someArgumentsBad Then
                            If Not includeMethodNameInErrorMessages Then
                                ReportDiagnostic(diagnostics, arguments(i).Syntax, ERRID.ERR_TooManyArgs)
                            ElseIf candidateIsExtension Then
                                ReportDiagnostic(diagnostics, arguments(i).Syntax,
                                                 ERRID.ERR_TooManyArgs2,
                                                 candidateSymbol, candidateSymbol.ContainingType)
                            Else
                                ReportDiagnostic(diagnostics, arguments(i).Syntax,
                                                 ERRID.ERR_TooManyArgs1, If(representCandidateInDiagnosticsOpt, candidateSymbol))
                            End If

                            someArgumentsBad = True
                        End If

                    ElseIf paramIndex = candidate.ParameterCount - 1 AndAlso
                           candidate.Parameters(paramIndex).IsParamArray Then

                        ' Collect ParamArray arguments
                        While i < arguments.Length

                            If Not argumentNames.IsDefault AndAlso argumentNames(i) IsNot Nothing Then
                                ' First named argument
                                Continue For
                            End If

                            If arguments(i).Kind = BoundKind.OmittedArgument Then
                                ReportDiagnostic(diagnostics, arguments(i).Syntax, ERRID.ERR_OmittedParamArrayArgument)
                                someParamArrayArgumentsBad = True
                            Else
                                paramArrayItems.Add(i)
                            End If

                            positionalArguments += 1
                            i += 1
                        End While

                        Exit For

                    Else
                        parameterToArgumentMap(paramIndex) = i
                        paramIndex += 1
                    End If

                    positionalArguments += 1
                Next

                Dim skippedSomeArguments As Boolean = False

                '§11.8.2 Applicable Methods
                '2.	Next, match each named argument to a parameter with the given name. 
                'If one of the named arguments fails to match, matches a paramarray parameter, 
                'or matches an argument already matched with another positional or named argument, 
                'the method is not applicable.
                For i As Integer = positionalArguments To arguments.Length - 1 Step 1

                    Debug.Assert(argumentNames(i) Is Nothing OrElse argumentNames(i).Length > 0)

                    If argumentNames(i) Is Nothing Then
                        ' Unnamed argument follows out-of-position named arguments
                        If Not someArgumentsBad Then
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(seenOutOfPositionNamedArgIndex).Syntax),
                                         ERRID.ERR_BadNonTrailingNamedArgument, argumentNames(seenOutOfPositionNamedArgIndex))
                        End If
                        Return
                    End If

                    If Not candidate.TryGetNamedParamIndex(argumentNames(i), paramIndex) Then
                        ' ERRID_NamedParamNotFound1
                        ' ERRID_NamedParamNotFound2
                        If Not includeMethodNameInErrorMessages Then
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax), ERRID.ERR_NamedParamNotFound1, argumentNames(i))
                        ElseIf candidateIsExtension Then
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax),
                                             ERRID.ERR_NamedParamNotFound3, argumentNames(i),
                                             candidateSymbol, candidateSymbol.ContainingType)
                        Else
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax),
                                             ERRID.ERR_NamedParamNotFound2, argumentNames(i), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                        End If

                        someArgumentsBad = True
                        Continue For
                    End If

                    If paramIndex = candidate.ParameterCount - 1 AndAlso
                        candidate.Parameters(paramIndex).IsParamArray Then
                        ' ERRID_NamedParamArrayArgument
                        ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax), ERRID.ERR_NamedParamArrayArgument)
                        someArgumentsBad = True
                        Continue For
                    End If

                    If parameterToArgumentMap(paramIndex) <> -1 AndAlso arguments(parameterToArgumentMap(paramIndex)).Kind <> BoundKind.OmittedArgument Then
                        ' ERRID_NamedArgUsedTwice1
                        ' ERRID_NamedArgUsedTwice2
                        ' ERRID_NamedArgUsedTwice3
                        If Not includeMethodNameInErrorMessages Then
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax), ERRID.ERR_NamedArgUsedTwice1, argumentNames(i))
                        ElseIf candidateIsExtension Then
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax),
                                             ERRID.ERR_NamedArgUsedTwice3, argumentNames(i),
                                             candidateSymbol, candidateSymbol.ContainingType)
                        Else
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax),
                                             ERRID.ERR_NamedArgUsedTwice2, argumentNames(i), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                        End If

                        someArgumentsBad = True
                        Continue For
                    End If

                    ' It is an error for a named argument to specify
                    ' a value for an explicitly omitted positional argument.
                    If paramIndex < positionalArguments Then
                        'ERRID_NamedArgAlsoOmitted1
                        'ERRID_NamedArgAlsoOmitted2
                        'ERRID_NamedArgAlsoOmitted3
                        If Not includeMethodNameInErrorMessages Then
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax), ERRID.ERR_NamedArgAlsoOmitted1, argumentNames(i))
                        ElseIf candidateIsExtension Then
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax),
                                             ERRID.ERR_NamedArgAlsoOmitted3, argumentNames(i),
                                             candidateSymbol, candidateSymbol.ContainingType)
                        Else
                            ReportDiagnostic(diagnostics, GetNamedArgumentIdentifier(arguments(i).Syntax),
                                             ERRID.ERR_NamedArgAlsoOmitted2, argumentNames(i), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                        End If

                        someArgumentsBad = True
                    End If

                    parameterToArgumentMap(paramIndex) = i
                Next

                ' Check whether type inference failed
                If candidateAnalysisResult.TypeArgumentInferenceDiagnosticsOpt IsNot Nothing Then
                    diagnostics.AddRange(candidateAnalysisResult.TypeArgumentInferenceDiagnosticsOpt)
                End If

                If candidate.IsGeneric AndAlso candidateAnalysisResult.State = OverloadResolution.CandidateAnalysisResultState.TypeInferenceFailed Then
                    ' Bug 122092: AddressOf doesn't want detailed info on which parameters could not be
                    ' inferred, just report the general type inference failed message in this case.
                    If delegateSymbol IsNot Nothing Then
                        ReportDiagnostic(diagnostics, diagnosticLocation, ERRID.ERR_DelegateBindingTypeInferenceFails)
                        Return
                    End If

                    If Not candidateAnalysisResult.SomeInferenceFailed Then

                        Dim reportedAnError As Boolean = False

                        For i As Integer = 0 To candidate.Arity - 1 Step 1
                            If candidateAnalysisResult.NotInferredTypeArguments(i) Then
                                If Not includeMethodNameInErrorMessages Then
                                    ReportDiagnostic(diagnostics, diagnosticLocation, ERRID.ERR_UnboundTypeParam1, candidate.TypeParameters(i))
                                ElseIf candidateIsExtension Then
                                    ReportDiagnostic(diagnostics, diagnosticLocation,
                                                     ERRID.ERR_UnboundTypeParam3, candidate.TypeParameters(i),
                                                     candidateSymbol, candidateSymbol.ContainingType)
                                Else
                                    ReportDiagnostic(diagnostics, diagnosticLocation,
                                                     ERRID.ERR_UnboundTypeParam2, candidate.TypeParameters(i), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                                End If

                                reportedAnError = True
                            End If
                        Next

                        If reportedAnError Then
                            Return
                        End If
                    End If

                    Dim inferenceErrorReasons As InferenceErrorReasons = candidateAnalysisResult.InferenceErrorReasons

                    If (inferenceErrorReasons And InferenceErrorReasons.Ambiguous) <> 0 Then
                        If Not includeMethodNameInErrorMessages Then
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicitAmbiguous1, ERRID.ERR_TypeInferenceFailureAmbiguous1))
                        ElseIf candidateIsExtension Then
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicitAmbiguous3, ERRID.ERR_TypeInferenceFailureAmbiguous3), candidateSymbol, candidateSymbol.ContainingType)
                        Else
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicitAmbiguous2, ERRID.ERR_TypeInferenceFailureAmbiguous2), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                        End If
                    ElseIf (inferenceErrorReasons And InferenceErrorReasons.NoBest) <> 0 Then
                        If Not includeMethodNameInErrorMessages Then
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicitNoBest1, ERRID.ERR_TypeInferenceFailureNoBest1))
                        ElseIf candidateIsExtension Then
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicitNoBest3, ERRID.ERR_TypeInferenceFailureNoBest3), candidateSymbol, candidateSymbol.ContainingType)
                        Else
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicitNoBest2, ERRID.ERR_TypeInferenceFailureNoBest2), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                        End If
                    Else
                        If candidateAnalysisResult.TypeArgumentInferenceDiagnosticsOpt IsNot Nothing AndAlso
                           candidateAnalysisResult.TypeArgumentInferenceDiagnosticsOpt.HasAnyResolvedErrors Then
                            ' Already reported some errors, let's not report a general inference error
                            Return
                        End If

                        If Not includeMethodNameInErrorMessages Then
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicit1, ERRID.ERR_TypeInferenceFailure1))
                        ElseIf candidateIsExtension Then
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicit3, ERRID.ERR_TypeInferenceFailure3), candidateSymbol, candidateSymbol.ContainingType)
                        Else
                            ReportDiagnostic(diagnostics, diagnosticLocation, If(queryMode, ERRID.ERR_TypeInferenceFailureNoExplicit2, ERRID.ERR_TypeInferenceFailure2), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                        End If
                    End If

                    Return
                End If

                ' Check generic constraints for method type arguments.
                If candidateAnalysisResult.State = OverloadResolution.CandidateAnalysisResultState.GenericConstraintsViolated Then
                    Debug.Assert(candidate.IsGeneric)
                    Debug.Assert(candidate.UnderlyingSymbol.Kind = SymbolKind.Method)

                    Dim method = DirectCast(candidate.UnderlyingSymbol, MethodSymbol)
                    ' TODO: Dev10 uses the location of the type parameter or argument that
                    ' violated the constraint, rather than  the entire invocation expression.
                    Dim succeeded = method.CheckConstraints(diagnosticLocation, diagnostics)
                    Debug.Assert(Not succeeded)
                    Return
                End If

                If candidateAnalysisResult.TypeArgumentInferenceDiagnosticsOpt IsNot Nothing AndAlso
                   candidateAnalysisResult.TypeArgumentInferenceDiagnosticsOpt.HasAnyErrors Then
                    Return
                End If

                ' Traverse the parameters, converting corresponding arguments
                ' as appropriate.

                Dim argIndex As Integer
                Dim candidateIsAProperty As Boolean = (candidateSymbol.Kind = SymbolKind.Property)

                For paramIndex = 0 To candidate.ParameterCount - 1 Step 1

                    Dim param As ParameterSymbol = candidate.Parameters(paramIndex)
                    Dim isByRef As Boolean = param.IsByRef
                    Dim targetType As TypeSymbol = param.Type

                    Dim argument As BoundExpression = Nothing

                    If param.IsParamArray AndAlso paramIndex = candidate.ParameterCount - 1 Then

                        If targetType.Kind <> SymbolKind.ArrayType Then

                            If targetType.Kind <> SymbolKind.ErrorType Then
                                ' ERRID_ParamArrayWrongType
                                ReportDiagnostic(diagnostics, diagnosticLocation, ERRID.ERR_ParamArrayWrongType)
                            End If

                            someArgumentsBad = True
                            Continue For

                        ElseIf someParamArrayArgumentsBad Then
                            Continue For
                        End If


                        If paramArrayItems.Count = 1 Then
                            Dim paramArrayArgument = arguments(paramArrayItems(0))

                            '§11.8.2 Applicable Methods
                            'If the conversion from the type of the argument expression to the paramarray type is narrowing, 
                            'then the method is only applicable in its expanded form.
                            Dim arrayConversion As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing
                            If allowUnexpandedParamArrayForm AndAlso
                                Not (Not paramArrayArgument.HasErrors AndAlso
                                    OverloadResolution.CanPassToParamArray(paramArrayArgument, targetType, arrayConversion, Me, Nothing)) Then
                                allowUnexpandedParamArrayForm = False
                            End If

                            '§11.8.2 Applicable Methods
                            'If the argument expression is the literal Nothing, then the method is only applicable in its unexpanded form 
                            If allowExpandedParamArrayForm AndAlso
                                paramArrayArgument.IsNothingLiteral() Then
                                allowExpandedParamArrayForm = False
                            End If

                        Else
                            ' Unexpanded form is not applicable: there are either more than one value or no values. 
                            If Not allowExpandedParamArrayForm Then

                                If paramArrayItems.Count = 0 Then
                                    If Not includeMethodNameInErrorMessages Then
                                        ReportDiagnostic(diagnostics, diagnosticLocation, ERRID.ERR_OmittedArgument1, CustomSymbolDisplayFormatter.ShortErrorName(param))
                                    ElseIf candidateIsExtension Then
                                        ReportDiagnostic(diagnostics, diagnosticLocation,
                                                         ERRID.ERR_OmittedArgument3, CustomSymbolDisplayFormatter.ShortErrorName(param),
                                                         candidateSymbol, candidateSymbol.ContainingType)
                                    Else
                                        ReportDiagnostic(diagnostics, diagnosticLocation,
                                                         ERRID.ERR_OmittedArgument2, CustomSymbolDisplayFormatter.ShortErrorName(param),
                                                         If(representCandidateInDiagnosticsOpt, candidateSymbol))
                                    End If
                                Else
                                    If Not includeMethodNameInErrorMessages Then
                                        ReportDiagnostic(diagnostics, diagnosticLocation, ERRID.ERR_TooManyArgs)
                                    ElseIf candidateIsExtension Then
                                        ReportDiagnostic(diagnostics, diagnosticLocation,
                                                           ERRID.ERR_TooManyArgs2, candidateSymbol, candidateSymbol.ContainingType)
                                    Else
                                        ReportDiagnostic(diagnostics, diagnosticLocation,
                                                 ERRID.ERR_TooManyArgs1, If(representCandidateInDiagnosticsOpt, candidateSymbol))
                                    End If
                                End If

                                someArgumentsBad = True
                                Continue For
                            End If

                            allowUnexpandedParamArrayForm = False
                        End If

                        If allowUnexpandedParamArrayForm Then
                            argument = arguments(paramArrayItems(0))
                            ReportByValConversionErrors(param, argument, targetType, reportNarrowingConversions, diagnostics)

                        ElseIf allowExpandedParamArrayForm Then
                            Dim arrayType = DirectCast(targetType, ArrayTypeSymbol)

                            If Not arrayType.IsSZArray Then
                                ' ERRID_ParamArrayWrongType
                                ReportDiagnostic(diagnostics, diagnosticLocation, ERRID.ERR_ParamArrayWrongType)
                                someArgumentsBad = True
                                Continue For
                            End If

                            Dim arrayElementType = arrayType.ElementType

                            For i As Integer = 0 To paramArrayItems.Count - 1 Step 1
                                argument = arguments(paramArrayItems(i))
                                ReportByValConversionErrors(param, argument, arrayElementType, reportNarrowingConversions, diagnostics)
                            Next
                        Else
                            Debug.Assert(paramArrayItems.Count = 1)
                            Dim paramArrayArgument = arguments(paramArrayItems(0))
                            ReportDiagnostic(diagnostics, paramArrayArgument.Syntax, ERRID.ERR_ParamArrayArgumentMismatch)
                        End If

                        Continue For
                    End If

                    argIndex = parameterToArgumentMap(paramIndex)
                    argument = If(argIndex = -1, Nothing, arguments(argIndex))

                    ' Argument nothing when the argument syntax is missing or BoundKind.OmittedArgument when the argument list contains commas
                    ' for the missing syntax so we have to test for both.
                    If argument Is Nothing OrElse argument.Kind = BoundKind.OmittedArgument Then

                        If argument Is Nothing AndAlso skippedSomeArguments Then
                            someArgumentsBad = True
                            Continue For
                        End If

                        'See Section 3 of §11.8.2 Applicable Methods

                        ' Deal with Optional arguments
                        ' Need to handle optional arguments here, there could be conversion errors, etc.

                        argument = GetArgumentForParameterDefaultValue(param, node, diagnostics, callerInfoOpt)

                        If argument Is Nothing Then
                            If Not includeMethodNameInErrorMessages Then
                                ReportDiagnostic(diagnostics, diagnosticLocation, ERRID.ERR_OmittedArgument1, CustomSymbolDisplayFormatter.ShortErrorName(param))
                            ElseIf candidateIsExtension Then
                                ReportDiagnostic(diagnostics, diagnosticLocation,
                                                 ERRID.ERR_OmittedArgument3, CustomSymbolDisplayFormatter.ShortErrorName(param),
                                                 candidateSymbol, candidateSymbol.ContainingType)
                            Else
                                ReportDiagnostic(diagnostics, diagnosticLocation,
                                                 ERRID.ERR_OmittedArgument2, CustomSymbolDisplayFormatter.ShortErrorName(param), If(representCandidateInDiagnosticsOpt, candidateSymbol))
                            End If

                            someArgumentsBad = True
                            Continue For
                        End If
                    End If

                    Debug.Assert(Not isByRef OrElse param.IsExplicitByRef OrElse targetType.IsStringType())

                    ' Arguments for properties are always passed with ByVal semantics. Even if
                    ' parameter in metadata is defined ByRef, we always pass corresponding argument 
                    ' through a temp without copy-back. Unlike with method calls, we rely on CodeGen
                    ' to introduce the temp (easy to do since there is no copy-back around it),
                    ' this allows us to keep the BoundPropertyAccess node simpler and allows to avoid
                    ' A LOT of complexity in UseTwiceRewriter, which we would otherwise have around
                    ' the temps.
                    ' Non-string arguments for implicitly ByRef string parameters of Declare functions
                    ' are passed through a temp without copy-back.
                    If isByRef AndAlso Not candidateIsAProperty AndAlso
                       (param.IsExplicitByRef OrElse (argument.Type IsNot Nothing AndAlso argument.Type.IsStringType())) Then
                        ReportByRefConversionErrors(candidate, param, argument, targetType, reportNarrowingConversions, diagnostics,
                                                    diagnosticNode:=node, delegateSymbol:=delegateSymbol)
                    Else
                        ReportByValConversionErrors(param, argument, targetType, reportNarrowingConversions, diagnostics,
                                                    diagnosticNode:=node, delegateSymbol:=delegateSymbol)
                    End If
                Next

            Finally
                paramArrayItems.Free()
                parameterToArgumentMap.Free()
            End Try
        End Sub

        ''' <summary>
        ''' Should be in sync with OverloadResolution.MatchArgumentToByRefParameter
        ''' </summary>
        Private Sub ReportByRefConversionErrors(
            candidate As OverloadResolution.Candidate,
            param As ParameterSymbol,
            argument As BoundExpression,
            targetType As TypeSymbol,
            reportNarrowingConversions As Boolean,
            diagnostics As DiagnosticBag,
            Optional diagnosticNode As SyntaxNode = Nothing,
            Optional delegateSymbol As Symbol = Nothing
        )

            ' TODO: Do we need to do more thorough check for error types here, i.e. dig into generics, 
            ' arrays, etc., detect types from unreferenced assemblies, ... ?
            If targetType.IsErrorType() OrElse argument.HasErrors Then ' UNDONE: should HasErrors really always cause argument mismatch [petergo, 3/9/2011]
                Return
            End If

            If argument.IsSupportingAssignment() Then

                If Not (argument.IsLValue() AndAlso targetType.IsSameTypeIgnoringAll(argument.Type)) Then

                    If Not ReportByValConversionErrors(param, argument, targetType, reportNarrowingConversions, diagnostics,
                                                       diagnosticNode:=diagnosticNode,
                                                       delegateSymbol:=delegateSymbol) Then

                        ' Check copy back conversion
                        Dim boundTemp = New BoundRValuePlaceholder(argument.Syntax, targetType)
                        Dim copyBackType = argument.GetTypeOfAssignmentTarget()
                        Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(boundTemp, copyBackType, Me, Nothing)

                        If Conversions.NoConversion(conv.Key) Then
                            ' Possible only with user-defined conversions, I think.
                            CreateConversionAndReportDiagnostic(argument.Syntax, boundTemp, conv, False, copyBackType, diagnostics, copybackConversionParamName:=param.Name)
                        ElseIf Conversions.IsNarrowingConversion(conv.Key) Then

                            Debug.Assert((conv.Key And ConversionKind.InvolvesNarrowingFromNumericConstant) = 0)

                            If OptionStrict = VisualBasic.OptionStrict.On Then
                                CreateConversionAndReportDiagnostic(argument.Syntax, boundTemp, conv, False, copyBackType, diagnostics, copybackConversionParamName:=param.Name)
                            ElseIf reportNarrowingConversions Then
                                ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_ArgumentCopyBackNarrowing3,
                                                       CustomSymbolDisplayFormatter.ShortErrorName(param), targetType, copyBackType)
                            End If
                        End If

                    End If

                End If

            Else
                ' No copy back needed

                ' If we are inside a lambda in a constructor and are passing ByRef a non-LValue field, which 
                ' would be an LValue field, if it were referred to in the constructor outside of a lambda, 
                ' we need to report an error because the operation will result in a simulated pass by
                ' ref (through a temp, without a copy back), which might be not the intent.
                If Report_ERRID_ReadOnlyInClosure(argument) Then
                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_ReadOnlyInClosure)
                End If

                ReportByValConversionErrors(param, argument, targetType, reportNarrowingConversions, diagnostics,
                                            diagnosticNode:=diagnosticNode,
                                            delegateSymbol:=delegateSymbol)
            End If

        End Sub


        ''' <summary>
        ''' Should be in sync with OverloadResolution.MatchArgumentToByValParameter.
        ''' </summary>
        Private Function ReportByValConversionErrors(
            param As ParameterSymbol,
            argument As BoundExpression,
            targetType As TypeSymbol,
            reportNarrowingConversions As Boolean,
            diagnostics As DiagnosticBag,
            Optional diagnosticNode As SyntaxNode = Nothing,
            Optional delegateSymbol As Symbol = Nothing
        ) As Boolean

            ' TODO: Do we need to do more thorough check for error types here, i.e. dig into generics, 
            ' arrays, etc., detect types from unreferenced assemblies, ... ?
            If targetType.IsErrorType() OrElse argument.HasErrors Then ' UNDONE: should HasErrors really always cause argument mismatch [petergo, 3/9/2011]
                Return True
            End If

            Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(argument, targetType, Me, Nothing)

            If Conversions.NoConversion(conv.Key) Then
                If delegateSymbol Is Nothing Then
                    CreateConversionAndReportDiagnostic(argument.Syntax, argument, conv, False, targetType, diagnostics)
                Else
                    ' in case of delegates, use the operand of the AddressOf as location for this error
                    CreateConversionAndReportDiagnostic(diagnosticNode, argument, conv, False, targetType, diagnostics)
                End If

                Return True
            End If

            Dim requiresNarrowingConversion As Boolean = False

            If Conversions.IsNarrowingConversion(conv.Key) Then

                If (conv.Key And ConversionKind.InvolvesNarrowingFromNumericConstant) = 0 Then

                    If OptionStrict = VisualBasic.OptionStrict.On Then
                        If delegateSymbol Is Nothing Then
                            CreateConversionAndReportDiagnostic(argument.Syntax, argument, conv, False, targetType, diagnostics)
                        Else
                            ' in case of delegates, use the operand of the AddressOf as location for this error
                            ' because delegates have different error messages in case there is one or more candidates for narrowing
                            ' indicate this as well.
                            CreateConversionAndReportDiagnostic(diagnosticNode, argument, conv, False, targetType, diagnostics)
                        End If

                        Return True
                    End If
                End If

                requiresNarrowingConversion = True

            ElseIf (conv.Key And ConversionKind.InvolvesNarrowingFromNumericConstant) <> 0 Then
                ' Dev10 overload resolution treats conversions that involve narrowing from numeric constant type
                ' as narrowing.
                requiresNarrowingConversion = True
            End If

            If reportNarrowingConversions AndAlso requiresNarrowingConversion Then
                Dim err As ERRID = ERRID.ERR_ArgumentNarrowing3

                Dim targetDelegateType = targetType.DelegateOrExpressionDelegate(Me)
                If argument.Kind = BoundKind.QueryLambda AndAlso targetDelegateType IsNot Nothing Then
                    Dim invoke As MethodSymbol = targetDelegateType.DelegateInvokeMethod

                    If invoke IsNot Nothing AndAlso Not invoke.IsSub Then
                        err = ERRID.ERR_NestedFunctionArgumentNarrowing3
                        argument = DirectCast(argument, BoundQueryLambda).Expression
                        targetType = invoke.ReturnType
                    End If
                End If

                If argument.Type Is Nothing Then
                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_ArgumentNarrowing2,
                                           CustomSymbolDisplayFormatter.ShortErrorName(param), targetType)
                Else
                    ReportDiagnostic(diagnostics, argument.Syntax, err,
                                           CustomSymbolDisplayFormatter.ShortErrorName(param), argument.Type, targetType)
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Should be kept in sync with OverloadResolution.MatchArguments, which populates 
        ''' data this function operates on.
        ''' </summary>
        Private Function PassArguments(
            node As SyntaxNode,
            ByRef candidate As OverloadResolution.CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            diagnostics As DiagnosticBag
        ) As (Arguments As ImmutableArray(Of BoundExpression), DefaultArguments As BitVector)

            Debug.Assert(candidate.State = OverloadResolution.CandidateAnalysisResultState.Applicable)

            If (arguments.IsDefault) Then
                arguments = ImmutableArray(Of BoundExpression).Empty
            End If

            Dim paramCount As Integer = candidate.Candidate.ParameterCount

            Dim parameterToArgumentMap = ArrayBuilder(Of Integer).GetInstance(paramCount, -1)
            Dim argumentsInOrder = ArrayBuilder(Of BoundExpression).GetInstance(paramCount)
            Dim defaultArguments = BitVector.Null

            Dim paramArrayItems As ArrayBuilder(Of Integer) = Nothing

            If candidate.IsExpandedParamArrayForm Then
                paramArrayItems = ArrayBuilder(Of Integer).GetInstance()
            End If

            Dim paramIndex As Integer

            ' For each parameter figure out matching argument.
            If candidate.ArgsToParamsOpt.IsDefaultOrEmpty Then
                Dim regularParamCount As Integer = paramCount

                If candidate.IsExpandedParamArrayForm Then
                    regularParamCount -= 1
                End If

                For i As Integer = 0 To Math.Min(regularParamCount, arguments.Length) - 1 Step 1
                    If arguments(i).Kind <> BoundKind.OmittedArgument Then
                        parameterToArgumentMap(i) = i
                    End If
                Next

                If candidate.IsExpandedParamArrayForm Then
                    For i As Integer = regularParamCount To arguments.Length - 1 Step 1
                        paramArrayItems.Add(i)
                    Next
                End If
            Else
                Dim argsToParams = candidate.ArgsToParamsOpt

                For i As Integer = 0 To argsToParams.Length - 1 Step 1
                    paramIndex = argsToParams(i)

                    If arguments(i).Kind <> BoundKind.OmittedArgument Then
                        If (candidate.IsExpandedParamArrayForm AndAlso
                            paramIndex = candidate.Candidate.ParameterCount - 1) Then

                            paramArrayItems.Add(i)
                        Else
                            parameterToArgumentMap(paramIndex) = i
                        End If
                    End If
                Next
            End If

            ' Traverse the parameters, converting corresponding arguments
            ' as appropriate.
            Dim candidateIsAProperty As Boolean = (candidate.Candidate.UnderlyingSymbol.Kind = SymbolKind.Property)

            For paramIndex = 0 To paramCount - 1 Step 1

                Dim param As ParameterSymbol = candidate.Candidate.Parameters(paramIndex)
                Dim targetType As TypeSymbol = param.Type

                Dim argument As BoundExpression = Nothing
                Dim conversion As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.Identity
                Dim conversionBack As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.Identity

                If candidate.IsExpandedParamArrayForm AndAlso paramIndex = candidate.Candidate.ParameterCount - 1 Then
                    Dim arrayElementType = DirectCast(targetType, ArrayTypeSymbol).ElementType

                    Dim items = ArrayBuilder(Of BoundExpression).GetInstance(paramArrayItems.Count)

                    For i As Integer = 0 To paramArrayItems.Count - 1 Step 1
                        items.Add(PassArgumentByVal(arguments(paramArrayItems(i)),
                                                               If(candidate.ConversionsOpt.IsDefaultOrEmpty,
                                                                  Conversions.Identity,
                                                                  candidate.ConversionsOpt(paramArrayItems(i))),
                                                               arrayElementType, diagnostics))
                    Next

                    ' Create the bound array and ensure that it is marked as compiler generated.
                    argument = New BoundArrayCreation(node, True,
                                    (New BoundExpression() {New BoundLiteral(node,
                                        ConstantValue.Create(items.Count),
                                        GetSpecialType(SpecialType.System_Int32, node, diagnostics)).MakeCompilerGenerated()}).AsImmutableOrNull(),
                                    New BoundArrayInitialization(node, items.ToImmutableAndFree(), targetType).MakeCompilerGenerated(), Nothing, Nothing, targetType).MakeCompilerGenerated()
                Else
                    Dim argIndex As Integer
                    argIndex = parameterToArgumentMap(paramIndex)
                    argument = If(argIndex = -1, Nothing, arguments(argIndex))

                    If argument IsNot Nothing AndAlso paramIndex = candidate.Candidate.ParameterCount - 1 AndAlso
                       param.IsParamArray Then
                        argument = ApplyImplicitConversion(argument.Syntax, targetType, argument, diagnostics)
                        ' Leave both conversions at identity since we already applied the conversion
                    ElseIf argIndex > -1 Then
                        If Not candidate.ConversionsOpt.IsDefaultOrEmpty Then
                            conversion = candidate.ConversionsOpt(argIndex)
                        End If

                        If Not candidate.ConversionsBackOpt.IsDefaultOrEmpty Then
                            conversionBack = candidate.ConversionsBackOpt(argIndex)
                        End If
                    End If
                End If

                Dim argumentIsDefaultValue As Boolean = False

                If argument Is Nothing Then
                    Debug.Assert(Not candidate.OptionalArguments.IsEmpty, "Optional arguments expected")

                    If defaultArguments.IsNull Then
                        defaultArguments = BitVector.Create(paramCount)
                    End If

                    ' Deal with Optional arguments
                    Dim defaultArgument As OverloadResolution.OptionalArgument = candidate.OptionalArguments(paramIndex)
                    argument = defaultArgument.DefaultValue
                    argumentIsDefaultValue = True
                    defaultArguments(paramIndex) = True
                    Debug.Assert(argument IsNot Nothing)
                    conversion = defaultArgument.Conversion

                    Dim argType = argument.Type
                    If argType IsNot Nothing Then
                        ' Report usesiteerror if it exists.
                        Dim useSiteErrorInfo = argType.GetUseSiteErrorInfo
                        If useSiteErrorInfo IsNot Nothing Then
                            ReportDiagnostic(diagnostics, argument.Syntax, useSiteErrorInfo)
                        End If
                    End If
                End If

                ' Arguments for properties are always passed with ByVal semantics. Even if
                ' parameter in metadata is defined ByRef, we always pass corresponding argument 
                ' through a temp without copy-back. Unlike with method calls, we rely on CodeGen
                ' to introduce the temp (easy to do since there is no copy-back around it),
                ' this allows us to keep the BoundPropertyAccess node simpler and allows to avoid
                ' A LOT of complexity in UseTwiceRewriter, which we would otherwise have around
                ' the temps.
                Debug.Assert(Not argumentIsDefaultValue OrElse argument.WasCompilerGenerated)
                Dim adjustedArgument As BoundExpression = PassArgument(argument, conversion, candidateIsAProperty, conversionBack, targetType, param, diagnostics)

                ' Keep SemanticModel happy.
                If argumentIsDefaultValue AndAlso adjustedArgument IsNot argument Then
                    adjustedArgument.SetWasCompilerGenerated()
                End If

                argumentsInOrder.Add(adjustedArgument)
            Next

            If paramArrayItems IsNot Nothing Then
                paramArrayItems.Free()
            End If

            parameterToArgumentMap.Free()
            Return (argumentsInOrder.ToImmutableAndFree(), defaultArguments)
        End Function


        Private Function PassArgument(
            argument As BoundExpression,
            conversionTo As KeyValuePair(Of ConversionKind, MethodSymbol),
            forceByValueSemantics As Boolean,
            conversionFrom As KeyValuePair(Of ConversionKind, MethodSymbol),
            targetType As TypeSymbol,
            param As ParameterSymbol,
            diagnostics As DiagnosticBag
        ) As BoundExpression
            Debug.Assert(Not param.IsByRef OrElse param.IsExplicitByRef OrElse targetType.IsStringType())

            ' Non-string arguments for implicitly ByRef string parameters of Declare functions
            ' are passed through a temp without copy-back.
            If param.IsByRef AndAlso Not forceByValueSemantics AndAlso
               (param.IsExplicitByRef OrElse (argument.Type IsNot Nothing AndAlso argument.Type.IsStringType())) Then
                Return PassArgumentByRef(param.IsOut, argument, conversionTo, conversionFrom, targetType,
                                         param.Name, diagnostics)
            Else
                Return PassArgumentByVal(argument, conversionTo, targetType, diagnostics)
            End If
        End Function

        Private Function PassArgumentByRef(
            isOutParameter As Boolean,
            argument As BoundExpression,
            conversionTo As KeyValuePair(Of ConversionKind, MethodSymbol),
            conversionFrom As KeyValuePair(Of ConversionKind, MethodSymbol),
            targetType As TypeSymbol,
            parameterName As String,
            diagnostics As DiagnosticBag
        ) As BoundExpression

#If DEBUG Then
            Dim checkAgainst As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(argument, targetType, Me, Nothing)
            Debug.Assert(conversionTo.Key = checkAgainst.Key)
            Debug.Assert(Equals(conversionTo.Value, checkAgainst.Value))
#End If

            ' TODO: Fields of MarshalByRef object are passed via temp.

            Dim isLValue As Boolean = argument.IsLValue()

            If isLValue AndAlso argument.Kind = BoundKind.PropertyAccess Then
                argument = argument.SetAccessKind(PropertyAccessKind.Get)
            End If

            If isLValue AndAlso Conversions.IsIdentityConversion(conversionTo.Key) Then
                'Nothing to do
                Debug.Assert(Conversions.IsIdentityConversion(conversionFrom.Key))
                Return argument

            ElseIf isLValue OrElse argument.IsSupportingAssignment() Then
                ' Need to allocate a temp of the target type,
                ' init it with argument's value,
                ' pass it ByRef,
                ' copy value back after the call.

                Dim inPlaceholder = New BoundByRefArgumentPlaceholder(argument.Syntax, isOutParameter, argument.Type, argument.HasErrors).MakeCompilerGenerated()
                Dim inConversion = CreateConversionAndReportDiagnostic(argument.Syntax,
                                                                       inPlaceholder,
                                                                       conversionTo,
                                                                       False, targetType, diagnostics)

                Dim outPlaceholder = New BoundRValuePlaceholder(argument.Syntax, targetType).MakeCompilerGenerated()
                Dim copyBackType = argument.GetTypeOfAssignmentTarget()

#If DEBUG Then
                checkAgainst = Conversions.ClassifyConversion(outPlaceholder, copyBackType, Me, Nothing)
                Debug.Assert(conversionFrom.Key = checkAgainst.Key)
                Debug.Assert(Equals(conversionFrom.Value, checkAgainst.Value))
#End If

                Dim outConversion = CreateConversionAndReportDiagnostic(argument.Syntax, outPlaceholder, conversionFrom,
                                                                      False, copyBackType, diagnostics,
                                                                      copybackConversionParamName:=parameterName).MakeCompilerGenerated()

                ' since we are going to assign to a latebound invocation
                ' force its arguments to be rvalues.
                If argument.Kind = BoundKind.LateInvocation Then
                    argument = MakeArgsRValues(DirectCast(argument, BoundLateInvocation), diagnostics)
                End If

                Dim copyBackExpression = BindAssignment(argument.Syntax, argument, outConversion, diagnostics)

                Debug.Assert(copyBackExpression.HasErrors OrElse
                             (copyBackExpression.Kind = BoundKind.AssignmentOperator AndAlso
                              DirectCast(copyBackExpression, BoundAssignmentOperator).Right Is outConversion))

                If Not isLValue Then
                    If argument.IsLateBound() Then
                        argument = argument.SetLateBoundAccessKind(LateBoundAccessKind.Get Or LateBoundAccessKind.Set)
                    Else
                        ' Diagnostics for PropertyAccessKind.Set case has been reported when we called BindAssignment.
                        WarnOnRecursiveAccess(argument, PropertyAccessKind.Get, diagnostics)
                        argument = argument.SetAccessKind(PropertyAccessKind.Get Or PropertyAccessKind.Set)
                    End If
                End If

                Return New BoundByRefArgumentWithCopyBack(argument.Syntax, argument,
                                                          inConversion, inPlaceholder,
                                                          outConversion, outPlaceholder,
                                                          targetType, copyBackExpression.HasErrors).MakeCompilerGenerated()
            Else
                ' Need to allocate a temp of the target type,
                ' init it with argument's value,
                ' pass it ByRef. Code gen will do this.
                Return PassArgumentByVal(argument, conversionTo, targetType, diagnostics)
            End If
        End Function

        ' when latebound invocation acts as an LHS in an assignment
        ' its arguments are always passed ByVal since property parameters 
        ' are always treated as ByVal
        ' This method is used to force the arguments to be RValues
        Private Function MakeArgsRValues(ByVal invocation As BoundLateInvocation,
                                                  diagnostics As DiagnosticBag) As BoundLateInvocation

            Dim args = invocation.ArgumentsOpt

            If Not args.IsEmpty Then
                Dim argBuilder As ArrayBuilder(Of BoundExpression) = Nothing

                For i As Integer = 0 To args.Length - 1
                    Dim arg = args(i)
                    Dim newArg = MakeRValue(arg, diagnostics)

                    If argBuilder Is Nothing AndAlso arg IsNot newArg Then
                        argBuilder = ArrayBuilder(Of BoundExpression).GetInstance
                        argBuilder.AddRange(args, i)
                    End If

                    If argBuilder IsNot Nothing Then
                        argBuilder.Add(newArg)
                    End If
                Next

                If argBuilder IsNot Nothing Then
                    invocation = invocation.Update(invocation.Member,
                                                    argBuilder.ToImmutableAndFree,
                                                    invocation.ArgumentNamesOpt,
                                                    invocation.AccessKind,
                                                    invocation.MethodOrPropertyGroupOpt,
                                                    invocation.Type)

                End If
            End If
            Return invocation
        End Function

        Friend Function PassArgumentByVal(
            argument As BoundExpression,
            conversion As KeyValuePair(Of ConversionKind, MethodSymbol),
            targetType As TypeSymbol,
            diagnostics As DiagnosticBag
        ) As BoundExpression
#If DEBUG Then
            Dim checkAgainst As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(argument, targetType, Me, Nothing)
            Debug.Assert(conversion.Key = checkAgainst.Key)
            Debug.Assert(Equals(conversion.Value, checkAgainst.Value))
#End If

            argument = CreateConversionAndReportDiagnostic(argument.Syntax, argument, conversion, False, targetType, diagnostics)

            Debug.Assert(Not argument.IsLValue)
            Return argument
        End Function

        ' Given a list of arguments, create arrays of the bound arguments and the names of those arguments.
        Private Sub BindArgumentsAndNames(
            argumentListOpt As ArgumentListSyntax,
            ByRef boundArguments As ImmutableArray(Of BoundExpression),
            ByRef argumentNames As ImmutableArray(Of String),
            ByRef argumentNamesLocations As ImmutableArray(Of Location),
            diagnostics As DiagnosticBag
        )
            Dim args As ImmutableArray(Of ArgumentSyntax) = Nothing

            If argumentListOpt IsNot Nothing Then
                Dim arguments = argumentListOpt.Arguments

                Dim argsArr(arguments.Count - 1) As ArgumentSyntax
                For i = 0 To argsArr.Length - 1
                    argsArr(i) = arguments(i)
                Next

                args = argsArr.AsImmutableOrNull
            End If

            BindArgumentsAndNames(
                args,
                boundArguments,
                argumentNames,
                argumentNamesLocations,
                diagnostics
            )
        End Sub

        ' Given a list of arguments, create arrays of the bound arguments and the names of those arguments.
        Private Sub BindArgumentsAndNames(
            arguments As ImmutableArray(Of ArgumentSyntax),
            ByRef boundArguments As ImmutableArray(Of BoundExpression),
            ByRef argumentNames As ImmutableArray(Of String),
            ByRef argumentNamesLocations As ImmutableArray(Of Location),
            diagnostics As DiagnosticBag
        )

            ' With SeparatedSyntaxList, it is most efficient to iterate with foreach and not to access Count.

            If arguments.IsDefaultOrEmpty Then
                boundArguments = s_noArguments
                argumentNames = Nothing
                argumentNamesLocations = Nothing
            Else

                Dim boundArgumentsBuilder As ArrayBuilder(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance
                Dim argumentNamesBuilder As ArrayBuilder(Of String) = Nothing
                Dim argumentNamesLocationsBuilder As ArrayBuilder(Of Location) = Nothing
                Dim argCount As Integer = 0
                Dim argumentSyntax As ArgumentSyntax

                For Each argumentSyntax In arguments
                    Select Case argumentSyntax.Kind
                        Case SyntaxKind.SimpleArgument
                            Dim simpleArgument = DirectCast(argumentSyntax, SimpleArgumentSyntax)
                            boundArgumentsBuilder.Add(BindValue(simpleArgument.Expression, diagnostics))

                            If simpleArgument.IsNamed Then
                                ' The common case is no named arguments. So we defer all work until the first named argument is seen.
                                If argumentNamesBuilder Is Nothing Then
                                    argumentNamesBuilder = ArrayBuilder(Of String).GetInstance()
                                    argumentNamesLocationsBuilder = ArrayBuilder(Of Location).GetInstance()

                                    For i = 0 To argCount - 1
                                        argumentNamesBuilder.Add(Nothing)
                                        argumentNamesLocationsBuilder.Add(Nothing)
                                    Next i
                                End If

                                Dim id = simpleArgument.NameColonEquals.Name.Identifier
                                If id.ValueText.Length > 0 Then
                                    argumentNamesBuilder.Add(id.ValueText)
                                Else
                                    argumentNamesBuilder.Add(Nothing)
                                End If

                                argumentNamesLocationsBuilder.Add(id.GetLocation())
                            ElseIf argumentNamesBuilder IsNot Nothing Then
                                argumentNamesBuilder.Add(Nothing)
                                argumentNamesLocationsBuilder.Add(Nothing)
                            End If

                        Case SyntaxKind.OmittedArgument
                            boundArgumentsBuilder.Add(New BoundOmittedArgument(argumentSyntax, Nothing))
                            If argumentNamesBuilder IsNot Nothing Then
                                argumentNamesBuilder.Add(Nothing)
                                argumentNamesLocationsBuilder.Add(Nothing)
                            End If

                        Case SyntaxKind.RangeArgument
                            ' NOTE: Redim statement supports range argument, like: Redim x(0 To 3)(0 To 6)
                            '       This behavior is misleading, because the 'range' (0 To 3) is actually 
                            '       being ignored and only upper bound is being used.
                            ' TODO: revise (add warning/error?)
                            Dim rangeArgument = DirectCast(argumentSyntax, RangeArgumentSyntax)
                            CheckRangeArgumentLowerBound(rangeArgument, diagnostics)
                            boundArgumentsBuilder.Add(BindValue(rangeArgument.UpperBound, diagnostics))
                            If argumentNamesBuilder IsNot Nothing Then
                                argumentNamesBuilder.Add(Nothing)
                                argumentNamesLocationsBuilder.Add(Nothing)
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(argumentSyntax.Kind)

                    End Select

                    argCount += 1
                Next

                boundArguments = boundArgumentsBuilder.ToImmutableAndFree
                argumentNames = If(argumentNamesBuilder Is Nothing, Nothing, argumentNamesBuilder.ToImmutableAndFree)
                argumentNamesLocations = If(argumentNamesLocationsBuilder Is Nothing, Nothing, argumentNamesLocationsBuilder.ToImmutableAndFree)
            End If

        End Sub

        Friend Function GetArgumentForParameterDefaultValue(param As ParameterSymbol, syntax As SyntaxNode, diagnostics As DiagnosticBag, callerInfoOpt As SyntaxNode) As BoundExpression
            Dim defaultArgument As BoundExpression = Nothing

            ' See Section 3 of §11.8.2 Applicable Methods
            ' Deal with Optional arguments. HasDefaultValue is true if the parameter is optional and has a default value.
            Dim defaultConstantValue As ConstantValue = If(param.IsOptional, param.ExplicitDefaultConstantValue(DefaultParametersInProgress), Nothing)
            If defaultConstantValue IsNot Nothing Then

                If callerInfoOpt IsNot Nothing AndAlso
                   callerInfoOpt.SyntaxTree IsNot Nothing AndAlso
                   Not callerInfoOpt.SyntaxTree.IsEmbeddedOrMyTemplateTree() AndAlso
                   Not SuppressCallerInfo Then

                    Dim isCallerLineNumber As Boolean = param.IsCallerLineNumber
                    Dim isCallerMemberName As Boolean = param.IsCallerMemberName
                    Dim isCallerFilePath As Boolean = param.IsCallerFilePath

                    If isCallerLineNumber OrElse isCallerMemberName OrElse isCallerFilePath Then
                        Dim callerInfoValue As ConstantValue = Nothing

                        If isCallerLineNumber Then
                            callerInfoValue = ConstantValue.Create(callerInfoOpt.SyntaxTree.GetDisplayLineNumber(GetCallerLocation(callerInfoOpt)))
                        ElseIf isCallerMemberName Then
                            Dim container As Symbol = ContainingMember

                            While container IsNot Nothing
                                Select Case container.Kind
                                    Case SymbolKind.Field, SymbolKind.Property, SymbolKind.Event
                                        Exit While

                                    Case SymbolKind.Method
                                        If container.IsLambdaMethod Then
                                            container = container.ContainingSymbol
                                        Else
                                            Dim propertyOrEvent As Symbol = DirectCast(container, MethodSymbol).AssociatedSymbol

                                            If propertyOrEvent IsNot Nothing Then
                                                container = propertyOrEvent
                                            End If

                                            Exit While
                                        End If

                                    Case Else
                                        container = container.ContainingSymbol
                                End Select
                            End While

                            If container IsNot Nothing AndAlso container.Name IsNot Nothing Then
                                callerInfoValue = ConstantValue.Create(container.Name)
                            End If
                        Else
                            Debug.Assert(isCallerFilePath)
                            callerInfoValue = ConstantValue.Create(callerInfoOpt.SyntaxTree.GetDisplayPath(callerInfoOpt.Span, Me.Compilation.Options.SourceReferenceResolver))
                        End If

                        If callerInfoValue IsNot Nothing Then
                            ' Use the value only if it will not cause errors.
                            Dim ignoreDiagnostics = DiagnosticBag.GetInstance()
                            Dim literal As BoundLiteral

                            If callerInfoValue.Discriminator = ConstantValueTypeDiscriminator.Int32 Then
                                literal = New BoundLiteral(syntax, callerInfoValue, GetSpecialType(SpecialType.System_Int32, syntax, ignoreDiagnostics))
                            Else
                                Debug.Assert(callerInfoValue.Discriminator = ConstantValueTypeDiscriminator.String)
                                literal = New BoundLiteral(syntax, callerInfoValue, GetSpecialType(SpecialType.System_String, syntax, ignoreDiagnostics))
                            End If

                            Dim convertedValue As BoundExpression = ApplyImplicitConversion(syntax, param.Type, literal, ignoreDiagnostics)

                            If Not convertedValue.HasErrors AndAlso Not ignoreDiagnostics.HasAnyErrors Then
                                ' Dev11 #248795: Caller info should be omitted if user defined conversion is involved.
                                If Not (convertedValue.Kind = BoundKind.Conversion AndAlso (DirectCast(convertedValue, BoundConversion).ConversionKind And ConversionKind.UserDefined) <> 0) Then
                                    defaultConstantValue = callerInfoValue
                                End If
                            End If

                            ignoreDiagnostics.Free()
                        End If
                    End If
                End If

                ' For compatibility with the native compiler bad metadata constants should be treated as default(T).  This 
                ' is a possible outcome of running an obfuscator over a valid DLL 
                If defaultConstantValue.IsBad Then
                    defaultConstantValue = ConstantValue.Null
                End If

                Dim defaultSpecialType = defaultConstantValue.SpecialType
                Dim defaultArgumentType As TypeSymbol = Nothing

                ' Constant has a type.
                Dim paramNullableUnderlyingTypeOrSelf As TypeSymbol = param.Type.GetNullableUnderlyingTypeOrSelf()

                If param.HasOptionCompare Then

                    ' If the argument has the OptionCompareAttribute
                    ' then use the setting for Option Compare [Binary|Text]
                    ' Other languages will use the default value specified.

                    If Me.OptionCompareText Then
                        defaultConstantValue = ConstantValue.Create(1)
                    Else
                        defaultConstantValue = ConstantValue.Default(SpecialType.System_Int32)
                    End If

                    If paramNullableUnderlyingTypeOrSelf.GetEnumUnderlyingTypeOrSelf().SpecialType = SpecialType.System_Int32 Then
                        defaultArgumentType = paramNullableUnderlyingTypeOrSelf
                    Else
                        defaultArgumentType = GetSpecialType(SpecialType.System_Int32, syntax, diagnostics)
                    End If

                ElseIf defaultSpecialType <> SpecialType.None Then
                    If paramNullableUnderlyingTypeOrSelf.GetEnumUnderlyingTypeOrSelf().SpecialType = defaultSpecialType Then
                        ' Enum default values are encoded as the underlying primitive type.  If the underlying types match then
                        ' use the parameter's enum type.
                        defaultArgumentType = paramNullableUnderlyingTypeOrSelf
                    Else
                        'Use the primitive type.
                        defaultArgumentType = GetSpecialType(defaultSpecialType, syntax, diagnostics)
                    End If
                Else
                    ' No type in constant.  Constant should be nothing
                    Debug.Assert(defaultConstantValue.IsNothing)
                End If

                defaultArgument = New BoundLiteral(syntax, defaultConstantValue, defaultArgumentType)

            ElseIf param.IsOptional Then

                ' Handle optional object type argument when no default value is specified.
                ' Section 3 of §11.8.2 Applicable Methods

                If param.Type.SpecialType = SpecialType.System_Object Then

                    Dim methodSymbol As MethodSymbol = Nothing
                    If param.IsMarshalAsObject Then
                        ' Nothing
                        defaultArgument = New BoundLiteral(syntax, ConstantValue.Null, Nothing)
                    ElseIf param.IsIDispatchConstant Then
                        ' new DispatchWrapper(nothing)
                        methodSymbol = DirectCast(GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_DispatchWrapper__ctor, syntax, diagnostics), MethodSymbol)
                    ElseIf param.IsIUnknownConstant Then
                        ' new UnknownWrapper(nothing)
                        methodSymbol = DirectCast(GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_UnknownWrapper__ctor, syntax, diagnostics), MethodSymbol)
                    Else
                        defaultArgument = New BoundOmittedArgument(syntax, param.Type)
                    End If

                    If methodSymbol IsNot Nothing Then
                        Dim argument = New BoundLiteral(syntax, ConstantValue.Null, param.Type).MakeCompilerGenerated()
                        defaultArgument = New BoundObjectCreationExpression(syntax, methodSymbol,
                                                                            ImmutableArray.Create(Of BoundExpression)(argument),
                                                                            Nothing,
                                                                            methodSymbol.ContainingType)
                    End If

                Else
                    defaultArgument = New BoundLiteral(syntax, ConstantValue.Null, Nothing)
                End If

            End If

            Return defaultArgument?.MakeCompilerGenerated()
        End Function

        Private Shared Function GetCallerLocation(syntax As SyntaxNode) As TextSpan
            Select Case syntax.Kind
                Case SyntaxKind.SimpleMemberAccessExpression
                    Return DirectCast(syntax, MemberAccessExpressionSyntax).Name.Span
                Case SyntaxKind.DictionaryAccessExpression
                    Return DirectCast(syntax, MemberAccessExpressionSyntax).OperatorToken.Span
                Case Else
                    Return syntax.Span
            End Select
        End Function

        ''' <summary>
        ''' Return true if the node is an immediate child of a call statement.
        ''' </summary>
        Private Shared Function IsCallStatementContext(node As InvocationExpressionSyntax) As Boolean
            Dim parent As VisualBasicSyntaxNode = node.Parent

            ' Dig through conditional access
            If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.ConditionalAccessExpression Then
                Dim conditional = DirectCast(parent, ConditionalAccessExpressionSyntax)

                If conditional.WhenNotNull Is node Then
                    parent = conditional.Parent
                End If
            End If

            Return parent IsNot Nothing AndAlso (parent.Kind = SyntaxKind.CallStatement OrElse parent.Kind = SyntaxKind.ExpressionStatement)
        End Function

    End Class

End Namespace

