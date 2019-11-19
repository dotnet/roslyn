' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Module SymbolExtensions

        Friend Const NamespaceKindNamespaceGroup As NamespaceKind = Nothing

        ''' <summary>
        ''' Does the compilation this symbol belongs to output to a winmdobj?
        ''' </summary>
        <Extension()>
        Friend Function IsCompilationOutputWinMdObj(symbol As Symbol) As Boolean
            Dim comp = symbol.DeclaringCompilation
            Return comp IsNot Nothing And comp.Options.OutputKind = OutputKind.WindowsRuntimeMetadata
        End Function

        ''' <summary>
        ''' String such as 'class', 'interface' etc that can be used in error messages.
        ''' </summary>
        <Extension()>
        Friend Function GetKindText(target As Symbol) As String
            Select Case target.Kind

                Case SymbolKind.Namespace
                    Return "namespace"

                Case SymbolKind.NamedType
                    Select Case DirectCast(target, TypeSymbol).TypeKind
                        Case TypeKind.Class
                            Return "class"
                        Case TypeKind.Enum
                            Return "enum"
                        Case TypeKind.Interface
                            Return "interface"
                        Case TypeKind.Structure
                            Return "structure"
                        Case TypeKind.Module
                            Return "module"
                        Case TypeKind.Delegate
                            ' Dev10 error message format "... delegate Class goo ..." instead of "... delegate goo ..."               
                            Return "delegate Class"
                        Case Else
                            'TODO: do we need string s for ByRef, Array, TypeParameter etc?
                            Return "type"
                    End Select

                Case SymbolKind.Field, SymbolKind.Local, SymbolKind.Parameter, SymbolKind.RangeVariable
                    Return "variable"

                Case SymbolKind.Method
                    Dim methodSymbol = DirectCast(target, MethodSymbol)
                    Select Case methodSymbol.MethodKind
                        Case MethodKind.Conversion, MethodKind.UserDefinedOperator, MethodKind.BuiltinOperator
                            Return "operator"
                        Case Else
                            If methodSymbol.IsSub Then
                                Return "sub"
                            Else
                                Return "function"
                            End If
                    End Select

                Case SymbolKind.Property
                    If DirectCast(target, PropertySymbol).IsWithEvents Then
                        Return "WithEvents variable"
                    Else
                        Return "property"
                    End If

                Case SymbolKind.Event
                    Return "event"

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(target.Kind)
            End Select
        End Function

        ''' <summary>
        ''' String "ReadOnly", "WriteOnly", or "" describing the kind of property.
        ''' </summary>
        <Extension()>
        Friend Function GetPropertyKindText(target As PropertySymbol) As String
            If target.IsWriteOnly Then
                Return SyntaxFacts.GetText(SyntaxKind.WriteOnlyKeyword)
            ElseIf target.IsReadOnly Then
                Return SyntaxFacts.GetText(SyntaxKind.ReadOnlyKeyword)
            Else
                Return ""
            End If
        End Function

        <Extension()>
        Friend Function ToErrorMessageArgument(target As Symbol, Optional errorCode As ERRID = ERRID.ERR_None) As Object
            If target.Kind = SymbolKind.Namespace Then
                Dim ns As NamespaceSymbol = DirectCast(target, NamespaceSymbol)
                If ns.IsGlobalNamespace Then
                    Return StringConstants.UnnamedNamespaceErrName
                End If
            End If

            If errorCode = ERRID.ERR_TypeConflict6 Then
                Return CustomSymbolDisplayFormatter.DefaultErrorFormat(target)
            End If

            Return target
        End Function

        ''' <summary>
        ''' Checks if there is a name match with any type parameter.
        ''' </summary>
        ''' <param name="this"></param>
        ''' <param name="name"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <Extension()>
        Friend Function MatchesAnyName(this As ImmutableArray(Of TypeParameterSymbol), name As String) As Boolean
            For Each tp In this
                If IdentifierComparison.Comparer.Compare(name, tp.Name) = 0 Then
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Returns true if this symbols can overload another of the same kind.
        ''' </summary>
        <Extension()>
        Public Function IsOverloadable(symbol As Symbol) As Boolean
            Dim kind = symbol.Kind
            If kind = SymbolKind.Method Then
                Return True
            ElseIf kind <> SymbolKind.Property Then
                Return False
            End If

            ' uncommon case - WithEvents property do not overload. They behave like fields when OHI is concerned.
            Return DirectCast(symbol, PropertySymbol).IsOverloadable
        End Function

        ''' <summary>
        ''' Returns true if this property can overload another.
        ''' </summary>
        <Extension()>
        Public Function IsOverloadable(propertySymbol As PropertySymbol) As Boolean
            Return Not propertySymbol.IsWithEvents
        End Function

        ''' <summary>
        ''' Helper that tells if symbol has Overloads (hidebysig) on it
        ''' </summary>
        <Extension()>
        Friend Function IsOverloads(sym As Symbol) As Boolean
            Debug.Assert(sym IsNot Nothing)

            ' methods and properties can overload.
            Select Case sym.Kind
                Case SymbolKind.Method
                    Dim method = DirectCast(sym, MethodSymbol)
                    If method.IsOverloads Then
                        Return True
                    End If

                Case SymbolKind.Property
                    Dim prop = DirectCast(sym, PropertySymbol)
                    If prop.IsOverloads Then
                        Return True
                    End If
            End Select

            Return False
        End Function

        ''' <summary>
        ''' Member that does not have Overloads, is considered Shadows (hidebyname)
        ''' </summary>
        <Extension()>
        Friend Function IsShadows(sym As Symbol) As Boolean
            ' everything that does not overload, shadows
            Return Not sym.IsOverloads
        End Function

        ''' <summary>
        ''' Is the symbol an instance member (i.e. access requires a receiver)
        ''' </summary>
        <Extension()>
        Friend Function IsInstanceMember(sym As Symbol) As Boolean
            Select Case sym.Kind
                Case SymbolKind.Field, SymbolKind.Property, SymbolKind.Method, SymbolKind.Event
                    Return Not sym.IsShared
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Is this a member of a interface that requires implementation?
        ''' </summary>
        <Extension()>
        Friend Function RequiresImplementation(sym As Symbol) As Boolean
            Select Case sym.Kind
                Case SymbolKind.Method, SymbolKind.Property, SymbolKind.Event
                    ' Note that in metadata, you can encounter methods that are static (shared) or non-virtual 
                    ' (for example TLBIMP VtblGap members), even though you can't define those in source.
                    Return sym.ContainingType.IsInterfaceType() AndAlso
                           Not sym.IsShared AndAlso Not sym.IsNotOverridable AndAlso
                           (sym.IsMustOverride OrElse sym.IsOverridable)
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Friend Function IsMetadataVirtual(method As MethodSymbol) As Boolean
            If method.IsOverridable OrElse
                    method.IsOverrides OrElse
                    method.IsMustOverride OrElse
                    Not method.ExplicitInterfaceImplementations.IsEmpty Then
                Return True
            End If

            Dim definition As MethodSymbol = method.OriginalDefinition
            Dim containingSourceType = TryCast(definition.ContainingSymbol, SourceNamedTypeSymbol)

            Return containingSourceType IsNot Nothing AndAlso
                   containingSourceType.GetCorrespondingComClassInterfaceMethod(definition) IsNot Nothing
        End Function

        <Extension()>
        Public Function IsAccessor(methodSymbol As MethodSymbol) As Boolean
            Return methodSymbol.AssociatedSymbol IsNot Nothing
        End Function

        <Extension()>
        Public Function IsAccessor(symbol As Symbol) As Boolean
            Return symbol.Kind = SymbolKind.Method AndAlso IsAccessor(DirectCast(symbol, MethodSymbol))
        End Function

        <Extension()>
        Public Function IsWithEventsProperty(symbol As Symbol) As Boolean
            Return symbol.Kind = SymbolKind.Property AndAlso DirectCast(symbol, PropertySymbol).IsWithEvents
        End Function

        ''' <summary>
        ''' Returns True for "regular" properties (those that are not WithEvents.
        ''' Typically used for OHI diagnostics where WithEvents properties are treated as variables.
        ''' </summary>
        <Extension()>
        Public Function IsPropertyAndNotWithEvents(symbol As Symbol) As Boolean
            Return symbol.Kind = SymbolKind.Property AndAlso Not DirectCast(symbol, PropertySymbol).IsWithEvents
        End Function

        <Extension()>
        Friend Function IsAnyConstructor(method As MethodSymbol) As Boolean
            Dim kind = method.MethodKind
            Return kind = MethodKind.Constructor OrElse kind = MethodKind.SharedConstructor
        End Function

        ''' <summary>
        ''' default zero-init constructor symbol is added to a struct when it does not define 
        ''' its own parameterless public constructor.
        ''' We do not emit this constructor and do not call it 
        ''' </summary>
        <Extension()>
        Friend Function IsDefaultValueTypeConstructor(method As MethodSymbol) As Boolean
            Return method.IsImplicitlyDeclared AndAlso
                   method.ContainingType.IsValueType AndAlso
                   method.IsParameterlessConstructor()
        End Function

        <Extension()>
        Friend Function IsReducedExtensionMethod(this As Symbol) As Boolean
            Return this.Kind = SymbolKind.Method AndAlso DirectCast(this, MethodSymbol).IsReducedExtensionMethod
        End Function

        ''' <summary>
        ''' Return the overridden symbol for either a method or property.
        ''' </summary>
        <Extension()>
        Friend Function OverriddenMember(sym As Symbol) As Symbol
            Select Case sym.Kind
                Case SymbolKind.Method
                    Return DirectCast(sym, MethodSymbol).OverriddenMethod
                Case SymbolKind.Property
                    Return DirectCast(sym, PropertySymbol).OverriddenProperty
                Case SymbolKind.Event
                    Return DirectCast(sym, EventSymbol).OverriddenEvent
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary> 
        ''' Return the arity of a member. 
        ''' </summary>
        <Extension()>
        Friend Function GetArity(symbol As Symbol) As Integer
            Select Case symbol.Kind
                Case SymbolKind.Method
                    Return (DirectCast(symbol, MethodSymbol)).Arity
                Case SymbolKind.NamedType, SymbolKind.ErrorType
                    Return (DirectCast(symbol, NamedTypeSymbol)).Arity
                Case Else
                    Return 0
            End Select
        End Function

        ''' <summary> 
        ''' Returns the Me symbol associated with a member. 
        ''' sym must be a member (method, field or property)
        ''' </summary>
        <Extension()>
        Friend Function GetMeParameter(sym As Symbol) As ParameterSymbol
            Select Case sym.Kind
                Case SymbolKind.Method
                    Return DirectCast(sym, MethodSymbol).MeParameter
                Case SymbolKind.Field
                    Return DirectCast(sym, FieldSymbol).MeParameter
                Case SymbolKind.Property
                    Return DirectCast(sym, PropertySymbol).MeParameter
                Case SymbolKind.Parameter
                    Return Nothing
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(sym.Kind)
            End Select
        End Function

        <Extension()>
        Friend Function OfMinimalArity(symbols As IEnumerable(Of NamespaceOrTypeSymbol)) As NamespaceOrTypeSymbol
            Dim minAritySymbol As NamespaceOrTypeSymbol = Nothing
            Dim minArity As Integer = Int32.MaxValue
            For Each symbol In symbols
                Dim arity As Integer = GetArity(symbol)
                If arity < minArity Then
                    minArity = arity
                    minAritySymbol = symbol
                End If
            Next

            Return minAritySymbol
        End Function

        <Extension()>
        Friend Function UnwrapAlias(symbol As Symbol) As Symbol
            Dim aliasSym = TryCast(symbol, AliasSymbol)
            Return If(aliasSym Is Nothing, symbol, aliasSym.Target)
        End Function

        ''' <summary>
        ''' Is symbol a user-defined operator method.
        ''' </summary>
        <Extension()>
        Friend Function IsUserDefinedOperator(symbol As Symbol) As Boolean
            Return symbol.Kind = SymbolKind.Method AndAlso DirectCast(symbol, MethodSymbol).IsUserDefinedOperator()
        End Function

        ''' <summary>
        ''' Does symbol or its containing type have Microsoft.CodeAnalysis.Embedded() attribute
        ''' </summary>
        <Extension()>
        Friend Function IsHiddenByCodeAnalysisEmbeddedAttribute(symbol As Symbol) As Boolean
            ' Only upper-level types should be checked 
            Dim upperLevelType = GetUpperLevelNamedTypeSymbol(symbol)
            Return upperLevelType IsNot Nothing AndAlso upperLevelType.HasCodeAnalysisEmbeddedAttribute
        End Function

        ''' <summary>
        ''' Does symbol or its containing type have Microsoft.VisualBasic.Embedded() attribute
        ''' </summary>
        <Extension()>
        Friend Function IsHiddenByVisualBasicEmbeddedAttribute(symbol As Symbol) As Boolean
            ' Only upper-level types should be checked 
            Dim upperLevelType = GetUpperLevelNamedTypeSymbol(symbol)
            Return upperLevelType IsNot Nothing AndAlso upperLevelType.HasVisualBasicEmbeddedAttribute
        End Function

        ''' <summary>
        ''' Gets the upper-level named type symbol, or returns Nothing if it does not exist.
        ''' </summary>
        <Extension()>
        Friend Function GetUpperLevelNamedTypeSymbol(symbol As Symbol) As NamedTypeSymbol
            Dim upperLevelType = If(symbol.Kind = SymbolKind.NamedType, DirectCast(symbol, NamedTypeSymbol), symbol.ContainingType)
            If upperLevelType Is Nothing Then
                Return Nothing
            End If

            While upperLevelType.ContainingType IsNot Nothing
                upperLevelType = upperLevelType.ContainingType
            End While

            Return upperLevelType
        End Function

        <Extension>
        Friend Function GetDeclaringSyntaxNode(Of T As VisualBasicSyntaxNode)(this As Symbol) As T

            For Each node In this.DeclaringSyntaxReferences.Select(Function(d) d.GetSyntax())
                Dim node_T = TryCast(node, T)
                If node_T IsNot Nothing Then
                    Return node_T
                End If
            Next

            Debug.Assert(this.IsImplicitlyDeclared)
            Return DirectCast(Symbol.GetDeclaringSyntaxNodeHelper(Of T)(this.Locations).FirstOrDefault, T)
        End Function

        <Extension>
        Friend Function AsMember(Of T As Symbol)(origMember As T, type As NamedTypeSymbol) As T
            Debug.Assert(origMember.IsDefinition)

            ' ContainingType is always a definition here which is a type definition so we can use "Is"
            If type Is origMember.ContainingType Then
                Return origMember
            End If

            Dim substituted = DirectCast(type, SubstitutedNamedType)
            Return DirectCast(substituted.GetMemberForDefinition(origMember), T)
        End Function

        <Extension>
        Friend Function EnsureVbSymbolOrNothing(Of TSource As ISymbol, TDestination As {Symbol, TSource})(symbol As TSource, paramName As String) As TDestination
            Dim vbSymbol = TryCast(symbol, TDestination)

            If vbSymbol Is Nothing AndAlso symbol IsNot Nothing Then
                Throw New ArgumentException(VBResources.NotAVbSymbol, paramName)
            End If

            Return vbSymbol
        End Function

        <Extension>
        Friend Function ContainingNonLambdaMember(member As Symbol) As Symbol
            While (member?.Kind = SymbolKind.Method).GetValueOrDefault() AndAlso DirectCast(member, MethodSymbol).MethodKind = MethodKind.AnonymousFunction
                member = member.ContainingSymbol
            End While

            Return member
        End Function

        <Extension>
        Friend Function ContainsTupleNames(member As Symbol) As Boolean
            Select Case member.Kind
                Case SymbolKind.Method
                    Dim method = DirectCast(member, MethodSymbol)
                    Return method.ReturnType.ContainsTupleNames() OrElse ContainsTupleNames(method.Parameters)
                Case SymbolKind.Property
                    Dim [property] = DirectCast(member, PropertySymbol)
                    Return [property].Type.ContainsTupleNames() OrElse ContainsTupleNames([property].Parameters)
                Case SymbolKind.Event
                    ' We don't check the event Type directly because materializing it requires checking the tuple names in the type (to validate interface implementations)
                    Return ContainsTupleNames(DirectCast(member, EventSymbol).DelegateParameters)
                Case Else
                    '  We currently don't need to use this method for other kinds of symbols
                    Throw ExceptionUtilities.UnexpectedValue(member.Kind)
            End Select
        End Function

        Private Function ContainsTupleNames(parameters As ImmutableArray(Of ParameterSymbol)) As Boolean
            Return parameters.Any(Function(p) p.Type.ContainsTupleNames())
        End Function
    End Module
End Namespace
