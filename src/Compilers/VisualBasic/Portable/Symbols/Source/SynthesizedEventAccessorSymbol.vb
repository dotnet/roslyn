' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend MustInherit Class SynthesizedEventAccessorSymbol
        Inherits SynthesizedAccessor(Of SourceEventSymbol)

        Private _lazyReturnType As TypeSymbol
        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)
        Private _lazyExplicitImplementations As ImmutableArray(Of MethodSymbol) ' lazily populated with explicit implementations

        Protected Sub New(container As SourceMemberContainerTypeSymbol,
                          [event] As SourceEventSymbol)
            MyBase.New(container, [event])
            ' TODO: custom modifiers
        End Sub

        Private ReadOnly Property SourceEvent As SourceEventSymbol
            Get
                Return m_propertyOrEvent
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If _lazyExplicitImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(
                        _lazyExplicitImplementations,
                        SourceEvent.GetAccessorImplementations(Me.MethodKind))
                End If

                Return _lazyExplicitImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If _lazyParameters.IsDefault Then
                    Dim diagnostics = BindingDiagnosticBag.GetInstance()

                    Dim parameterType As TypeSymbol
                    If Me.MethodKind = MethodKind.EventRemove AndAlso m_propertyOrEvent.IsWindowsRuntimeEvent Then
                        parameterType = Me.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)
                        diagnostics.Add(Binder.GetUseSiteInfoForWellKnownType(parameterType), Me.GetFirstLocation())
                    Else
                        parameterType = SourceEvent.Type
                    End If

                    Dim parameter = New SynthesizedParameterSymbol(Me, parameterType, 0, False, "obj")
                    Dim parameterList = ImmutableArray.Create(Of ParameterSymbol)(parameter)

                    DirectCast(Me.ContainingModule, SourceModuleSymbol).AtomicStoreArrayAndDiagnostics(_lazyParameters, parameterList, diagnostics)

                    diagnostics.Free()
                End If

                Return _lazyParameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                If _lazyReturnType Is Nothing Then
                    Dim diagnostics = BindingDiagnosticBag.GetInstance()

                    Dim compilation = Me.DeclaringCompilation
                    Dim type As TypeSymbol
                    Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol)
                    If Me.IsSub Then
                        type = compilation.GetSpecialType(SpecialType.System_Void)
                        ' Don't report on add, because it will be the same for remove.
                        useSiteInfo = If(Me.MethodKind = MethodKind.EventRemove, Binder.GetUseSiteInfoForSpecialType(type), Nothing)
                    Else
                        type = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)
                        useSiteInfo = Binder.GetUseSiteInfoForWellKnownType(type)
                    End If

                    diagnostics.Add(useSiteInfo, Me.GetFirstLocation())

                    DirectCast(Me.ContainingModule, SourceModuleSymbol).AtomicStoreReferenceAndDiagnostics(_lazyReturnType, type, diagnostics)

                    diagnostics.Free()
                End If

                Debug.Assert(_lazyReturnType IsNot Nothing)
                Return _lazyReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return Not (Me.MethodKind = MethodKind.EventAdd AndAlso m_propertyOrEvent.IsWindowsRuntimeEvent)
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Dim compilation = Me.DeclaringCompilation
            Return ConstructFieldLikeEventAccessorBody(Me.m_propertyOrEvent, Me.MethodKind = MethodKind.EventAdd, compilation, diagnostics)
        End Function

        Protected Shared Function ConstructFieldLikeEventAccessorBody(eventSymbol As SourceEventSymbol,
                                                           isAddMethod As Boolean,
                                                           compilation As VisualBasicCompilation,
                                                           diagnostics As BindingDiagnosticBag) As BoundBlock
            Debug.Assert(eventSymbol.HasAssociatedField)
            Dim result As BoundBlock = If(eventSymbol.IsWindowsRuntimeEvent,
                       ConstructFieldLikeEventAccessorBody_WinRT(eventSymbol, isAddMethod, compilation, diagnostics),
                       ConstructFieldLikeEventAccessorBody_Regular(eventSymbol, isAddMethod, compilation, diagnostics))

            ' Contract guarantees non-nothing return.
            Return If(result,
                      New BoundBlock(
                        DirectCast(eventSymbol.SyntaxReference.GetSyntax(), VisualBasicSyntaxNode),
                        Nothing,
                        ImmutableArray(Of LocalSymbol).Empty,
                        ImmutableArray(Of BoundStatement).Empty,
                        hasErrors:=True))
        End Function

        Private Shared Function ConstructFieldLikeEventAccessorBody_WinRT(eventSymbol As SourceEventSymbol,
                                                           isAddMethod As Boolean,
                                                           compilation As VisualBasicCompilation,
                                                           diagnostics As BindingDiagnosticBag) As BoundBlock
            Dim syntax = eventSymbol.SyntaxReference.GetVisualBasicSyntax()

            Dim accessor As MethodSymbol = If(isAddMethod, eventSymbol.AddMethod, eventSymbol.RemoveMethod)
            Debug.Assert(accessor IsNot Nothing)

            Dim field As FieldSymbol = eventSymbol.AssociatedField
            Debug.Assert(field IsNot Nothing)

            Dim fieldType As NamedTypeSymbol = DirectCast(field.Type, NamedTypeSymbol)
            Debug.Assert(fieldType.Name = "EventRegistrationTokenTable")

            ' Don't cascade.
            If fieldType.IsErrorType Then
                Return Nothing
            End If

            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing

            Dim getOrCreateMethod As MethodSymbol = DirectCast(Binder.GetWellKnownTypeMember(
                compilation,
                WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__GetOrCreateEventRegistrationTokenTable,
                useSiteInfo), MethodSymbol)

            diagnostics.Add(useSiteInfo, syntax.GetLocation())
            Debug.Assert(getOrCreateMethod IsNot Nothing OrElse useSiteInfo.DiagnosticInfo IsNot Nothing)

            If getOrCreateMethod Is Nothing Then
                Return Nothing
            End If

            getOrCreateMethod = getOrCreateMethod.AsMember(fieldType)

            Dim processHandlerMember As WellKnownMember = If(isAddMethod,
                WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__AddEventHandler,
                WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__RemoveEventHandler)

            useSiteInfo = Nothing
            Dim processHandlerMethod As MethodSymbol = DirectCast(Binder.GetWellKnownTypeMember(
                compilation,
                processHandlerMember,
                useSiteInfo), MethodSymbol)

            diagnostics.Add(useSiteInfo, syntax.GetLocation())
            Debug.Assert(processHandlerMethod IsNot Nothing OrElse useSiteInfo.DiagnosticInfo IsNot Nothing)

            If processHandlerMethod Is Nothing Then
                Return Nothing
            End If

            processHandlerMethod = processHandlerMethod.AsMember(fieldType)

            ' _tokenTable
            Dim fieldAccess = New BoundFieldAccess(
                syntax,
                If(field.IsShared, Nothing, New BoundMeReference(syntax, accessor.MeParameter.Type)),
                field,
                isLValue:=True,
                type:=field.Type).MakeCompilerGenerated()

            ' EventRegistrationTokenTable(Of Event).GetOrCreateEventRegistrationTokenTable(_tokenTable)
            Dim getOrCreateCall = New BoundCall(
                syntax:=syntax,
                method:=getOrCreateMethod,
                methodGroupOpt:=Nothing,
                receiverOpt:=Nothing,
                arguments:=ImmutableArray.Create(Of BoundExpression)(fieldAccess),
                constantValueOpt:=Nothing,
                type:=getOrCreateMethod.ReturnType).MakeCompilerGenerated()

            Dim parameterSymbol As ParameterSymbol = accessor.Parameters.Single()

            ' value
            Dim parameterAccess = New BoundParameter(
                syntax,
                parameterSymbol,
                isLValue:=False,
                type:=parameterSymbol.Type).MakeCompilerGenerated()

            ' EventRegistrationTokenTable(Of Event).GetOrCreateEventRegistrationTokenTable(_tokenTable).AddHandler(value) ' or RemoveHandler
            Dim processHandlerCall = New BoundCall(
                syntax:=syntax,
                method:=processHandlerMethod,
                methodGroupOpt:=Nothing,
                receiverOpt:=getOrCreateCall,
                arguments:=ImmutableArray.Create(Of BoundExpression)(parameterAccess),
                constantValueOpt:=Nothing,
                type:=processHandlerMethod.ReturnType).MakeCompilerGenerated()

            If isAddMethod Then
                ' {
                '     return EventRegistrationTokenTable(Of Event).GetOrCreateEventRegistrationTokenTable(_tokenTable).AddHandler(value)
                ' }   
                Dim returnStatement = New BoundReturnStatement(syntax, processHandlerCall, functionLocalOpt:=Nothing, exitLabelOpt:=Nothing)
                Return New BoundBlock(
                    syntax,
                    statementListSyntax:=Nothing,
                    locals:=ImmutableArray(Of LocalSymbol).Empty,
                    statements:=ImmutableArray.Create(Of BoundStatement)(returnStatement)).MakeCompilerGenerated()
            Else
                ' {
                '     EventRegistrationTokenTable(Of Event).GetOrCreateEventRegistrationTokenTable(_tokenTable).RemoveHandler(value)
                '     return
                ' }  
                Dim callStatement = New BoundExpressionStatement(syntax, processHandlerCall).MakeCompilerGenerated()
                Dim returnStatement = New BoundReturnStatement(syntax, expressionOpt:=Nothing, functionLocalOpt:=Nothing, exitLabelOpt:=Nothing)
                Return New BoundBlock(
                    syntax,
                    statementListSyntax:=Nothing,
                    locals:=ImmutableArray(Of LocalSymbol).Empty,
                    statements:=ImmutableArray.Create(Of BoundStatement)(callStatement, returnStatement)).MakeCompilerGenerated()
            End If

        End Function

        ''' <summary>
        ''' Generate a thread-safe accessor for a field-like event.
        ''' 
        ''' DelegateType tmp0 = _event; //backing field
        ''' DelegateType tmp1;
        ''' DelegateType tmp2;
        ''' do {
        '''     tmp1 = tmp0;
        '''     tmp2 = (DelegateType)Delegate.Combine(tmp1, value); //Remove for -=
        '''     tmp0 = Interlocked.CompareExchange&lt; DelegateType&gt; (ref _event, tmp2, tmp1);
        ''' } while ((object)tmp0 != (object)tmp1);
        ''' 
        ''' Note, if System.Threading.Interlocked.CompareExchange&lt;T&gt; Is Not available,
        ''' we emit the following code And mark the method Synchronized (unless it Is a struct).
        ''' 
        ''' _event = (DelegateType)Delegate.Combine(_event, value); //Remove for -=
        ''' 
        ''' </summary>
        Private Shared Function ConstructFieldLikeEventAccessorBody_Regular(eventSymbol As SourceEventSymbol,
                                                                   isAddMethod As Boolean,
                                                                   compilation As VisualBasicCompilation,
                                                                   diagnostics As BindingDiagnosticBag) As BoundBlock

            If Not eventSymbol.Type.IsDelegateType() Then
                Return Nothing
            End If

            Dim syntax = eventSymbol.SyntaxReference.GetVisualBasicSyntax
            Dim delegateType As TypeSymbol = eventSymbol.Type
            Dim accessor As MethodSymbol = If(isAddMethod, eventSymbol.AddMethod, eventSymbol.RemoveMethod)
            Dim meParameter As ParameterSymbol = accessor.MeParameter
            Dim boolType As TypeSymbol = compilation.GetSpecialType(SpecialType.System_Boolean)

            Dim updateMethodId As SpecialMember = If(isAddMethod, SpecialMember.System_Delegate__Combine, SpecialMember.System_Delegate__Remove)

            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing
            Dim updateMethod As MethodSymbol = DirectCast(Binder.GetSpecialTypeMember(compilation.Assembly, updateMethodId, useSiteInfo), MethodSymbol)

            diagnostics.Add(useSiteInfo, syntax.GetLocation())

            Dim [return] As BoundStatement = New BoundReturnStatement(syntax,
                                                                      Nothing,
                                                                      Nothing,
                                                                      Nothing).MakeCompilerGenerated

            If updateMethod Is Nothing Then
                Return New BoundBlock(syntax,
                                  Nothing,
                                  ImmutableArray(Of LocalSymbol).Empty,
                                  ImmutableArray.Create(Of BoundStatement)([return])
                                  ).MakeCompilerGenerated
            End If

            useSiteInfo = Nothing
            Dim compareExchangeMethod As MethodSymbol = DirectCast(Binder.GetWellKnownTypeMember(compilation, WellKnownMember.System_Threading_Interlocked__CompareExchange_T, useSiteInfo), MethodSymbol)

            Dim fieldReceiver As BoundMeReference = If(eventSymbol.IsShared,
                                                       Nothing,
                                                       New BoundMeReference(syntax, meParameter.Type).MakeCompilerGenerated)

            Dim fieldSymbol = eventSymbol.AssociatedField
            Dim boundBackingField As BoundFieldAccess = New BoundFieldAccess(syntax,
                                                                             fieldReceiver,
                                                                             fieldSymbol,
                                                                             True,
                                                                             fieldSymbol.Type).MakeCompilerGenerated

            Dim parameterSymbol = accessor.Parameters(0)
            Dim boundParameter As BoundParameter = New BoundParameter(syntax,
                                                                      parameterSymbol,
                                                                      isLValue:=False,
                                                                      type:=parameterSymbol.Type).MakeCompilerGenerated

            Dim delegateUpdate As BoundExpression
            Dim conversionsUseSiteInfo As New CompoundUseSiteInfo(Of AssemblySymbol)(diagnostics, compilation.Assembly)
            Dim conversionKind1 As ConversionKind
            Dim conversionKind2 As ConversionKind

            If compareExchangeMethod Is Nothing Then

                ' (DelegateType)Delegate.Combine(_event, value)
                conversionKind1 = Conversions.ClassifyDirectCastConversion(fieldSymbol.Type, updateMethod.Parameters(0).Type, conversionsUseSiteInfo)
                conversionKind2 = Conversions.ClassifyDirectCastConversion(boundParameter.Type, updateMethod.Parameters(1).Type, conversionsUseSiteInfo)
                Debug.Assert(conversionKind1 = ConversionKind.WideningReference)
                Debug.Assert(conversionKind2 = ConversionKind.WideningReference)

                diagnostics.Add(syntax.GetLocation(), conversionsUseSiteInfo)

                delegateUpdate = New BoundDirectCast(syntax,
                                                     New BoundCall(syntax,
                                                                   updateMethod,
                                                                   Nothing,
                                                                   Nothing,
                                                                   ImmutableArray.Create(Of BoundExpression)(
                                                                       New BoundDirectCast(syntax, boundBackingField.MakeRValue(), ConversionKind.WideningReference, updateMethod.Parameters(0).Type),
                                                                       New BoundDirectCast(syntax, boundParameter, ConversionKind.WideningReference, updateMethod.Parameters(1).Type)),
                                                                   Nothing,
                                                                   updateMethod.ReturnType),
                                                               ConversionKind.NarrowingReference,
                                                               delegateType,
                                                               delegateType.IsErrorType).MakeCompilerGenerated

                ' _event = (DelegateType)Delegate.Combine(_event, value);
                Dim eventUpdate As BoundStatement = New BoundExpressionStatement(syntax,
                                                                            New BoundAssignmentOperator(syntax,
                                                                                                        boundBackingField,
                                                                                                        delegateUpdate,
                                                                                                        True,
                                                                                                        delegateType).MakeCompilerGenerated
                                                                                                    ).MakeCompilerGenerated

                Return New BoundBlock(syntax,
                                  Nothing,
                                  ImmutableArray(Of LocalSymbol).Empty,
                                  ImmutableArray.Create(Of BoundStatement)(
                                      eventUpdate,
                                      [return])
                                  ).MakeCompilerGenerated
            End If

            diagnostics.Add(useSiteInfo, syntax.GetLocation())

            compareExchangeMethod = compareExchangeMethod.Construct(ImmutableArray.Create(Of TypeSymbol)(delegateType))

            Dim loopLabel As GeneratedLabelSymbol = New GeneratedLabelSymbol("LOOP")
            Const numTemps As Integer = 3
            Dim tmps As LocalSymbol() = New LocalSymbol(numTemps - 1) {}
            Dim boundTmps As BoundLocal() = New BoundLocal(numTemps - 1) {}

            Dim i As Integer = 0
            While i < tmps.Length
                tmps(i) = New SynthesizedLocal(accessor, delegateType, SynthesizedLocalKind.LoweringTemp)
                boundTmps(i) = New BoundLocal(syntax, tmps(i), delegateType)
                i = i + 1
            End While

            ' tmp0 = _event;
            Dim tmp0Init As BoundStatement = New BoundExpressionStatement(syntax,
                                                                          New BoundAssignmentOperator(syntax,
                                                                                                      boundTmps(0),
                                                                                                      boundBackingField.MakeRValue(),
                                                                                                      True,
                                                                                                      delegateType).MakeCompilerGenerated
                                                                                                  ).MakeCompilerGenerated

            ' LOOP:
            Dim loopStart As BoundStatement = New BoundLabelStatement(syntax, loopLabel).MakeCompilerGenerated

            ' tmp1 = tmp0;
            Dim tmp1Update As BoundStatement = New BoundExpressionStatement(syntax,
                                                                            New BoundAssignmentOperator(syntax,
                                                                                                        boundTmps(1),
                                                                                                        boundTmps(0).MakeRValue(),
                                                                                                        True,
                                                                                                        delegateType).MakeCompilerGenerated
                                                                                                    ).MakeCompilerGenerated

            ' (DelegateType)Delegate.Combine(tmp1, value)
            conversionKind1 = Conversions.ClassifyDirectCastConversion(boundTmps(1).Type, updateMethod.Parameters(0).Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
            conversionKind2 = Conversions.ClassifyDirectCastConversion(boundParameter.Type, updateMethod.Parameters(1).Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
            Debug.Assert(conversionKind1 = ConversionKind.WideningReference)
            Debug.Assert(conversionKind2 = ConversionKind.WideningReference)

            diagnostics.Add(syntax.GetLocation(), conversionsUseSiteInfo)

            delegateUpdate = New BoundDirectCast(syntax,
                                                 New BoundCall(syntax,
                                                               updateMethod,
                                                               Nothing,
                                                               Nothing,
                                                               ImmutableArray.Create(Of BoundExpression)(
                                                                   New BoundDirectCast(syntax, boundTmps(1).MakeRValue(), ConversionKind.WideningReference, updateMethod.Parameters(0).Type),
                                                                   New BoundDirectCast(syntax, boundParameter, ConversionKind.WideningReference, updateMethod.Parameters(1).Type)),
                                                               Nothing,
                                                               updateMethod.ReturnType),
                                                           ConversionKind.NarrowingReference,
                                                           delegateType,
                                                           delegateType.IsErrorType).MakeCompilerGenerated

            ' tmp2 = (DelegateType)Delegate.Combine(tmp1, value);
            Dim tmp2Update As BoundStatement = New BoundExpressionStatement(syntax,
                                                                            New BoundAssignmentOperator(syntax,
                                                                                                        boundTmps(2),
                                                                                                        delegateUpdate,
                                                                                                        True,
                                                                                                        delegateType).MakeCompilerGenerated
                                                                                                    ).MakeCompilerGenerated

            ' Interlocked.CompareExchange<DelegateType>(ref _event, tmp2, tmp1)
            Dim compareExchange As BoundExpression = New BoundCall(syntax,
                                                                   compareExchangeMethod,
                                                                   Nothing,
                                                                   Nothing,
                                                                   ImmutableArray.Create(Of BoundExpression)(boundBackingField, boundTmps(2).MakeRValue(), boundTmps(1).MakeRValue()),
                                                                   Nothing,
                                                                   compareExchangeMethod.ReturnType)

            ' tmp0 = Interlocked.CompareExchange<DelegateType>(ref _event, tmp2, tmp1);
            Dim tmp0Update As BoundStatement = New BoundExpressionStatement(syntax,
                                                                            New BoundAssignmentOperator(syntax,
                                                                                                        boundTmps(0),
                                                                                                        compareExchange,
                                                                                                        True,
                                                                                                        delegateType).MakeCompilerGenerated
                                                                                                    ).MakeCompilerGenerated

            ' tmp[0] == tmp[1] // i.e. exit when they are equal, jump to start otherwise
            Dim loopExitCondition As BoundExpression = New BoundBinaryOperator(syntax,
                                                                               BinaryOperatorKind.Is,
                                                                               boundTmps(0).MakeRValue(),
                                                                               boundTmps(1).MakeRValue(),
                                                                               False,
                                                                               boolType).MakeCompilerGenerated

            ' branchfalse (tmp[0] == tmp[1]) LOOP
            Dim loopEnd As BoundStatement = New BoundConditionalGoto(syntax,
                                                                     loopExitCondition,
                                                                     False,
                                                                     loopLabel).MakeCompilerGenerated

            Return New BoundBlock(syntax,
                                  Nothing,
                                  tmps.AsImmutable(),
                                  ImmutableArray.Create(Of BoundStatement)(
                                      tmp0Init,
                                      loopStart,
                                      tmp1Update,
                                      tmp2Update,
                                      tmp0Update,
                                      loopEnd,
                                      [return])
                                  ).MakeCompilerGenerated
        End Function

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            MyBase.GenerateDeclarationErrors(cancellationToken)

            cancellationToken.ThrowIfCancellationRequested()
            Dim unusedParameters = Me.Parameters
            Dim unusedReturnType = Me.ReturnType
        End Sub

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of VisualBasicAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            Debug.Assert(Not ContainingType.IsImplicitlyDeclared)
            Dim compilation = Me.DeclaringCompilation
            AddSynthesizedAttribute(attributes,
                                    compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

            ' Dev11 adds DebuggerNonUserCode; there is no reason to do so since:
            ' - we emit no debug info for the body
            ' - the code doesn't call any user code that could inspect the stack and find the accessor's frame
            ' - the code doesn't throw exceptions whose stack frames we would need to hide
            ' 
            ' C# also doesn't add DebuggerHidden nor DebuggerNonUserCode attributes.
        End Sub

        Friend NotOverridable Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property ImplementationAttributes As MethodImplAttributes
            Get
                Dim result = MyBase.ImplementationAttributes

                If Not IsMustOverride AndAlso Not SourceEvent.IsWindowsRuntimeEvent AndAlso Not ContainingType.IsStructureType() AndAlso
                   DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange_T) Is Nothing Then
                    ' Under these conditions, this method needs to be synchronized.
                    result = result Or MethodImplAttributes.Synchronized
                End If

                Return result
            End Get
        End Property
    End Class

    Friend NotInheritable Class SynthesizedAddAccessorSymbol
        Inherits SynthesizedEventAccessorSymbol

        Public Sub New(container As SourceMemberContainerTypeSymbol,
                          [event] As SourceEventSymbol)
            MyBase.New(container, [event])
        End Sub

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.EventAdd
            End Get
        End Property
    End Class

    Friend NotInheritable Class SynthesizedRemoveAccessorSymbol
        Inherits SynthesizedEventAccessorSymbol

        Public Sub New(container As SourceMemberContainerTypeSymbol,
                          [event] As SourceEventSymbol)
            MyBase.New(container, [event])
        End Sub

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.EventRemove
            End Get
        End Property
    End Class

End Namespace
