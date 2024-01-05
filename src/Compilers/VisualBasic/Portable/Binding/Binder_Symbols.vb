' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ' Handler the parts of binding for binding types.
    Partial Friend Class Binder
        ''' <summary>
        ''' Bind a type name using the given binder. Returns a type symbol if the binding bound to something,
        ''' or an error symbol if the binding failed. In either case, errors may be reported via the
        ''' context. For example, if an inaccessible type or type with the wrong arity was found, the best possible
        ''' type is returned, but an error is also generated.
        ''' </summary>
        ''' <param name="typeSyntax">The syntax to bind.</param>
        ''' <param name="diagBag">Place to put diagnostics. If no reasonable type was found, an undefined type
        ''' diagnostic is placed in here. Other diagnostics (both related to the type being bound, or
        ''' type arguments thereof) can be placed here also. </param>
        ''' <returns>The best type that can be found, or and ErrorTypeSymbol if no reasonable type can be found.</returns>
        Public Function BindTypeSyntax(typeSyntax As TypeSyntax,
                                       diagBag As BindingDiagnosticBag,
                                       Optional suppressUseSiteError As Boolean = False,
                                       Optional inGetTypeContext As Boolean = False,
                                       Optional resolvingBaseType As Boolean = False) As TypeSymbol

            Debug.Assert(Not inGetTypeContext OrElse Not resolvingBaseType)

            Dim type = TypeBinder.BindTypeSyntax(typeSyntax, Me, diagBag, suppressUseSiteError, resolvingBaseType:=resolvingBaseType)

            ' GetType(void) and GetType(<Modulename>) are legal, only report diagnostics if it's not
            ' in a GetType context.
            If Not inGetTypeContext Then
                ReportUseOfModuleOrVoidType(typeSyntax, type, diagBag)
            End If

            Return type
        End Function

        Friend Function BindTypeOrAliasSyntax(typeSyntax As TypeSyntax,
                               diagBag As BindingDiagnosticBag,
                               Optional suppressUseSiteError As Boolean = False) As Symbol

            Dim sym As Symbol = TypeBinder.BindTypeOrAliasSyntax(typeSyntax, Me, diagBag, suppressUseSiteError,
                                                                 inGetTypeContext:=False, resolvingBaseType:=False)

            Dim type = TryCast(sym, TypeSymbol)
            If type IsNot Nothing Then
                ReportUseOfModuleOrVoidType(typeSyntax, type, diagBag)
            End If

            Return sym
        End Function

        Private Shared Sub ReportUseOfModuleOrVoidType(typeSyntax As TypeSyntax, type As TypeSymbol, diagBag As BindingDiagnosticBag)
            If type.SpecialType = SpecialType.System_Void Then
                Dim diagInfo = New BadSymbolDiagnostic(type, ERRID.ERR_BadUseOfVoid)
                ReportDiagnostic(diagBag, typeSyntax, diagInfo)
            ElseIf type.IsModuleType Then
                Dim diagInfo = New BadSymbolDiagnostic(type, ERRID.ERR_ModuleAsType1)
                ReportDiagnostic(diagBag, typeSyntax, diagInfo)
            End If
        End Sub

        ''' <summary>
        ''' Bind a type or namespace using the given binder. 
        ''' </summary>
        ''' <param name="typeSyntax">The syntax to bind.</param>
        ''' <returns>The best type or namespace that can be found, or and ErrorTypeSymbol if no reasonable type can be found.</returns>
        Public Function BindNamespaceOrTypeSyntax(typeSyntax As TypeSyntax,
                                              diagBag As BindingDiagnosticBag,
                                              Optional suppressUseSiteError As Boolean = False) As NamespaceOrTypeSymbol
            Return TypeBinder.BindNamespaceOrTypeSyntax(typeSyntax, Me, diagBag, suppressUseSiteError)
        End Function

        Public Function BindNamespaceOrTypeOrAliasSyntax(typeSyntax As TypeSyntax,
                                      diagBag As BindingDiagnosticBag,
                                      Optional suppressUseSiteError As Boolean = False) As Symbol
            Return TypeBinder.BindNamespaceOrTypeOrAliasSyntax(typeSyntax, Me, diagBag, suppressUseSiteError)
        End Function

        ''' <summary>
        ''' Apply generic type arguments, returning the constructed type. Produces errors for constraints
        ''' that aren't validated. If the wrong number of type arguments are supplied, the set of types
        ''' is silently truncated or extended with the type parameters.
        ''' </summary>
        ''' <param name="genericType">The type to construct from</param>
        ''' <param name="typeArguments">The types to apply</param>
        ''' <param name="syntaxWhole">The place to report errors for the generic type as a whole</param>
        ''' <param name="syntaxArguments">The place to report errors for each generic type argument.</param>
        ''' <param name="diagnostics">The diagnostics collection.</param>
        ''' <returns>The constructed generic type.</returns>
        Public Function ConstructAndValidateConstraints(genericType As NamedTypeSymbol,
                                                       typeArguments As ImmutableArray(Of TypeSymbol),
                                                       syntaxWhole As VisualBasicSyntaxNode,
                                                       syntaxArguments As SeparatedSyntaxList(Of TypeSyntax),
                                                       diagnostics As BindingDiagnosticBag) As NamedTypeSymbol

            Debug.Assert(genericType IsNot Nothing)
            Debug.Assert(Not typeArguments.IsDefault)
            Debug.Assert(syntaxWhole IsNot Nothing)
            Debug.Assert(typeArguments.Length = syntaxArguments.Count)

            If genericType.Arity = 0 Then
                ' TODO: Why do we get here with non-generic type and, 
                '       if we do, why don't we report any error?
                Return genericType ' nothing to construct.
            End If

            Dim checkConstraints As Boolean

            If genericType.Arity <> typeArguments.Length Then
                ' Fix type arguments to be of the right length.
                Dim newTypeArguments(0 To genericType.Arity - 1) As TypeSymbol
                For i = 0 To genericType.Arity - 1
                    If i < typeArguments.Length Then
                        newTypeArguments(i) = typeArguments(i)
                    Else
                        newTypeArguments(i) = genericType.TypeParameters(i).OriginalDefinition
                    End If
                Next
                typeArguments = newTypeArguments.AsImmutableOrNull

                ' Skip constraint checking since we may not
                ' have syntax for all type arguments.
                checkConstraints = False
            Else
                ' The number of arguments match. However, they may be missing.
                ' Check for unbound generic type
                Dim genericName As GenericNameSyntax = Nothing

                Select Case syntaxWhole.Kind
                    Case SyntaxKind.GenericName
                        genericName = DirectCast(syntaxWhole, GenericNameSyntax)

                    Case SyntaxKind.QualifiedName
                        genericName = DirectCast(DirectCast(syntaxWhole, QualifiedNameSyntax).Right, GenericNameSyntax)
                End Select

                If genericName IsNot Nothing Then
                    Dim isUnboundTypeExpr = syntaxArguments.AllAreMissingIdentifierName

                    If isUnboundTypeExpr Then
                        If IsUnboundTypeAllowed(genericName) Then
                            Return genericType.AsUnboundGenericType()
                        Else
                            ' For now, errors are reported during parsing so there is nothing to report here.
                            ' If the parser detects an open generic type outside the scope of a GetType it reports either
                            ' ERRID.ERR_UnrecognizedTypeKeyword or ERRID.ERR_ArrayOfRawGenericInvalid for array's of open generic types. 
                            ' Consider moving error reporting to here because these seem more like semantic errors.
                            ' C# reports the more logical error - "Unexpected use of an unbound generic type".
                        End If
                    End If
                End If

                checkConstraints = True
            End If

            Dim constructedType = genericType.Construct(typeArguments)

            ' Check generic constraints unless the type is used as part of a declaration.
            ' In those cases, constraints checking is handled by the caller, and is delayed
            ' to avoid cycles where resolving constraints requires the containing symbol
            ' to be bound. For instance, in the following, checking the constraint on "IA(Of T)"
            ' in "Inherits IA(Of T)" will trigger lookup of "C" (in "IB(Of T As C)") in all scopes,
            ' including in the interfaces of IB which are in the process of being bound.
            ' Interface IA(Of T As C) : End Interface
            ' Interface IB(Of T As C) : Inherits IA(Of T) : End Interface
            If checkConstraints AndAlso ShouldCheckConstraints Then
                constructedType.CheckConstraintsForNonTuple(syntaxArguments, diagnostics, template:=GetNewCompoundUseSiteInfo(diagnostics))
            End If

            constructedType = DirectCast(TupleTypeSymbol.TransformToTupleIfCompatible(constructedType), NamedTypeSymbol)

            Return constructedType
        End Function

        Friend Shared Function ReportUseSite(diagBag As BindingDiagnosticBag, syntax As SyntaxNodeOrToken, symbol As Symbol) As Boolean
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = symbol.GetUseSiteInfo()
            Return ReportUseSite(diagBag, syntax, useSiteInfo)
        End Function

        Friend Function GetAccessibleConstructors(type As NamedTypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of MethodSymbol)
            Dim ctors = type.InstanceConstructors
            If ctors.IsEmpty Then
                Return ctors
            End If

            Dim builder = ArrayBuilder(Of MethodSymbol).GetInstance()
            For Each constructor In ctors
                If IsAccessible(constructor, useSiteInfo) Then
                    builder.Add(constructor)
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' The type binder class handles binding of type names.
        ''' </summary>
        Private Class TypeBinder
            ''' <summary>
            ''' Bind a type name using the given binder. Returns a type symbol if the binding bound
            ''' to something, or an error symbol if the binding failed. In either case, errors may
            ''' be reported via the context. For example, if an inaccessible type or type with the
            ''' wrong arity was found, the best possible type is returned, but an error is also
            ''' generated.
            ''' </summary>
            ''' <param name="typeSyntax">The syntax to bind.</param>
            ''' <param name="binder">The binder to bind within. This binder is used for looking up
            ''' unqualified names, accessibility checking, reporting errors, and probably other
            ''' stuff too.</param>
            ''' <returns>The best type that can be found, or and ErrorTypeSymbol if no reasonable type can be found.</returns>
            Public Shared Function BindTypeSyntax(typeSyntax As TypeSyntax,
                                                  binder As Binder,
                                                  diagBag As BindingDiagnosticBag,
                                                  suppressUseSiteError As Boolean,
                                                  resolvingBaseType As Boolean) As TypeSymbol
                Dim symbol = BindTypeOrAliasSyntax(typeSyntax, binder, diagBag, suppressUseSiteError, False, resolvingBaseType:=resolvingBaseType)
                Debug.Assert(symbol Is Nothing OrElse TypeOf symbol Is TypeSymbol OrElse TypeOf symbol Is AliasSymbol, "unexpected symbol from BindTypeOrAliasSyntax")

                If symbol IsNot Nothing AndAlso symbol.Kind = SymbolKind.Alias Then
                    symbol = DirectCast(symbol, AliasSymbol).Target
                End If

                Return DirectCast(symbol, TypeSymbol)
            End Function

            Friend Shared Function BindTypeOrAliasSyntax(typeSyntax As TypeSyntax,
                                       binder As Binder,
                                       diagBag As BindingDiagnosticBag,
                                       suppressUseSiteError As Boolean,
                                       inGetTypeContext As Boolean,
                                       resolvingBaseType As Boolean) As Symbol

                Debug.Assert(Not inGetTypeContext OrElse Not resolvingBaseType)

                Dim lookupResult As LookupResult = LookupResult.GetInstance()
                Try
                    Dim reportedAnError As Boolean = False
                    LookupTypeOrNamespaceSyntax(lookupResult, typeSyntax, binder, diagBag, reportedAnError,
                                                unwrapAliases:=False, suppressUseSiteError:=suppressUseSiteError,
                                                inGetTypeContext:=inGetTypeContext, resolvingBaseType:=resolvingBaseType)

                    ' Report appropriate errors from the result.
                    Dim diagInfo As DiagnosticInfo = Nothing
                    If Not lookupResult.HasSymbol Then
                        Dim diagName = GetBaseNamesForDiagnostic(typeSyntax)
                        ' Don't report more errors if one is already reported
                        diagInfo = NotFound(typeSyntax, diagName, binder, If(reportedAnError, Nothing, diagBag))

                        Return Binder.GetErrorSymbol(diagName, diagInfo)
                    Else
                        If lookupResult.HasDiagnostic Then
                            Dim diagName = GetBaseNamesForDiagnostic(typeSyntax)
                            diagInfo = lookupResult.Diagnostic

                            If Not reportedAnError Then
                                Binder.ReportDiagnostic(diagBag, typeSyntax, lookupResult.Diagnostic)
                            End If

                            Return ErrorTypeFromLookupResult(diagName, lookupResult, binder)
                        End If

                        ' LookupTypeOrNamespaceSyntax can't return more than one symbol.
                        Dim sym = lookupResult.SingleSymbol
                        Dim typeSymbol As TypeSymbol = Nothing

                        If sym.Kind = SymbolKind.Alias Then
                            typeSymbol = TryCast(DirectCast(sym, AliasSymbol).Target, TypeSymbol)
                        Else
                            typeSymbol = TryCast(sym, TypeSymbol)
                        End If

                        If typeSymbol Is Nothing Then  ' i.e., lookupResult.SingleSymbol was a namespace instead of a type. 
                            diagInfo = New BadSymbolDiagnostic(lookupResult.SingleSymbol, ERRID.ERR_UnrecognizedType)

                            If Not reportedAnError Then
                                Binder.ReportDiagnostic(diagBag, typeSyntax, diagInfo)
                            End If

                            Return Binder.GetErrorSymbol(GetBaseNamesForDiagnostic(typeSyntax), diagInfo, ImmutableArray.Create(Of Symbol)(sym), LookupResultKind.NotATypeOrNamespace)
                        Else
                            ' When we bind generic type reference, we pass through here with symbols for 
                            ' the generic type definition, each type argument and for the final constructed 
                            ' symbol. To avoid reporting duplicate diagnostics in this scenario, report use
                            ' site errors only on a definition.
                            If Not reportedAnError AndAlso Not suppressUseSiteError AndAlso
                               Not typeSymbol.IsArrayType() AndAlso Not typeSymbol.IsTupleType AndAlso typeSymbol.IsDefinition Then
                                ReportUseSite(diagBag, typeSyntax, typeSymbol)
                            ElseIf typeSymbol Is sym Then
                                binder.AddTypesAssemblyAsDependency(TryCast(typeSymbol, NamedTypeSymbol), diagBag)
                            End If

                            If diagBag.AccumulatesDiagnostics AndAlso typeSymbol.Kind = SymbolKind.NamedType AndAlso binder.SourceModule.AnyReferencedAssembliesAreLinked Then
                                Emit.NoPia.EmbeddedTypesManager.IsValidEmbeddableType(DirectCast(typeSymbol, NamedTypeSymbol), typeSyntax, diagBag.DiagnosticBag)
                            End If

                            binder.ReportDiagnosticsIfObsoleteOrNotSupported(diagBag, sym, typeSyntax)

                            Return sym
                        End If
                    End If
                Finally
                    lookupResult.Free()
                End Try
            End Function

            Private Shared Function NotFound(typeSyntax As TypeSyntax, diagName As String, binder As Binder, diagBag As BindingDiagnosticBag) As DiagnosticInfo
                Dim diagInfo As DiagnosticInfo

                If diagName = "Any" AndAlso IsParameterTypeOfDeclareMethod(typeSyntax) Then
                    diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_ObsoleteAsAny, diagName)

                    If diagBag IsNot Nothing Then
                        Binder.ReportDiagnostic(diagBag, typeSyntax, diagInfo)
                    End If

                    Return diagInfo
                End If

                Dim forwardedToAssembly As AssemblySymbol = Nothing

                If diagName.Length > 0 Then
                    CheckForForwardedType(binder.Compilation.Assembly, typeSyntax, diagName, forwardedToAssembly, diagBag)
                Else
                    Debug.Assert(typeSyntax.IsMissing OrElse typeSyntax.HasErrors)
                    Return Nothing
                End If

                If forwardedToAssembly Is Nothing Then
                    diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UndefinedType1, diagName)
                Else
                    diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_ForwardedTypeUnavailable3, diagName, binder.Compilation.Assembly, forwardedToAssembly)
                End If

                If diagBag IsNot Nothing Then
                    Binder.ReportDiagnostic(diagBag, typeSyntax, diagInfo)
                End If

                Return diagInfo
            End Function

            ''' <summary>
            ''' If lookup failed for a qualified name, we don't know which part of the lookup failed.  Therefore, we have
            ''' to check for a type forwarder for each prefix of the name.
            ''' </summary>
            ''' <param name="containingAssembly">Starting assembly.</param>
            ''' <param name="typeSyntax">Full name of type that failed lookup.  Shortened as different prefixes are checked.</param>
            ''' <param name="diagName">GetBaseNamesForDiagnostic(typeSyntax) (basically dot-delimited list of names).  Shortened as different prefixes are checked.</param>
            ''' <param name="forwardedToAssembly">Set if some prefix matches a forwarded type.</param>
            ''' <param name="diagBag">Diagnostics bag (Nothing if errors should not be reported).</param>
            Private Shared Sub CheckForForwardedType(containingAssembly As AssemblySymbol, ByRef typeSyntax As TypeSyntax, ByRef diagName As String, ByRef forwardedToAssembly As AssemblySymbol, diagBag As BindingDiagnosticBag)
                Dim currTypeSyntax As TypeSyntax = typeSyntax
                Dim currDiagName As String = diagName

                ' Each iteration of this loop strips the right part off a qualified name (in both currTypeSyntax and currDiagName).
                While True
                    Dim typeIsQualifiedName = currTypeSyntax.Kind = SyntaxKind.QualifiedName

                    Dim arity As Integer = 0
                    Dim fullName As String = currDiagName

                    Dim rightPart = If(typeIsQualifiedName, DirectCast(currTypeSyntax, QualifiedNameSyntax).Right, currTypeSyntax)

                    If rightPart.Kind = SyntaxKind.GenericName Then
                        arity = DirectCast(rightPart, GenericNameSyntax).Arity
                        fullName = MetadataHelpers.ComposeAritySuffixedMetadataName(currDiagName, arity, associatedFileIdentifier:=Nothing)
                    End If

                    forwardedToAssembly = GetForwardedToAssembly(containingAssembly, fullName, arity, typeSyntax, diagBag)

                    If forwardedToAssembly IsNot Nothing Then
                        typeSyntax = currTypeSyntax
                        diagName = currDiagName
                        Exit While
                    ElseIf typeIsQualifiedName Then
                        currTypeSyntax = DirectCast(currTypeSyntax, QualifiedNameSyntax).Left
                        currDiagName = currDiagName.Substring(0, currDiagName.LastIndexOf("."c))
                    Else
                        Exit While
                    End If
                End While
            End Sub

            ''' <summary>
            ''' Look for a type forwarder for the given type in the containing assembly and any referenced assemblies.
            ''' If one is found, search again in the target assembly.  Return the last assembly in the chain.
            ''' </summary>
            ''' <param name="containingAssembly">The assembly in which to look for the type forwarder.</param>
            ''' <param name="fullName">The metadata name of the (potentially) forwarded type, including the arity (if non-zero).</param>
            ''' <param name="arity">The arity of the forwarded type.</param>
            ''' <param name="typeSyntax">The syntax to report types on (if any).</param>
            ''' <param name="diagBag">The diagnostics bag (Nothing if errors should not be reported).</param>
            ''' <returns></returns>
            ''' <remarks>
            ''' Since this method is intended to be used for error reporting, it stops as soon as it finds
            ''' any type forwarder (or an error to report). It does not check other assemblies for consistency or better results.
            ''' 
            ''' NOTE: unlike in C#, this method searches for type forwarders case-insensitively.
            ''' </remarks>
            Private Shared Function GetForwardedToAssembly(containingAssembly As AssemblySymbol, fullName As String, arity As Integer, typeSyntax As TypeSyntax, diagBag As BindingDiagnosticBag) As AssemblySymbol
                Debug.Assert(arity = 0 OrElse fullName.EndsWith("`" & arity, StringComparison.Ordinal))

                ' NOTE: This won't work if the type isn't using CLS-style generic naming (i.e. `arity), but this code is
                ' only intended to improve diagnostic messages, so false negatives in corner cases aren't a big deal.
                Dim metadataName = MetadataTypeName.FromFullName(fullName, useCLSCompliantNameArityEncoding:=True, forcedArity:=arity)

                Dim forwardedType As NamedTypeSymbol = Nothing

                For Each referencedAssembly In containingAssembly.Modules(0).GetReferencedAssemblySymbols()
                    forwardedType = referencedAssembly.TryLookupForwardedMetadataType(metadataName, ignoreCase:=True)
                    If forwardedType IsNot Nothing Then
                        Exit For
                    End If
                Next

                If forwardedType IsNot Nothing Then
                    If diagBag IsNot Nothing AndAlso forwardedType.IsErrorType Then
                        Dim errorInfo = DirectCast(forwardedType, ErrorTypeSymbol).ErrorInfo

                        If errorInfo.Code = ERRID.ERR_TypeFwdCycle2 Then
                            Debug.Assert(forwardedType.ContainingAssembly IsNot Nothing, "How did we find a cycle if there is no forwarding?")
                            Binder.ReportDiagnostic(diagBag, typeSyntax, ERRID.ERR_TypeFwdCycle2, fullName, forwardedType.ContainingAssembly)
                        ElseIf errorInfo.Code = ERRID.ERR_TypeForwardedToMultipleAssemblies Then
                            Binder.ReportDiagnostic(diagBag, typeSyntax, errorInfo)
                            Return Nothing ' Cannot determine a suitable forwarding assembly
                        End If
                    End If

                    Return forwardedType.ContainingAssembly
                End If

                Return Nothing
            End Function

            Private Shared Function IsParameterTypeOfDeclareMethod(typeSyntax As TypeSyntax) As Boolean
                Dim p = typeSyntax.Parent
                If p IsNot Nothing AndAlso p.Kind = SyntaxKind.SimpleAsClause Then
                    p = p.Parent
                    If p.Kind = SyntaxKind.Parameter Then
                        p = p.Parent
                        If p.Kind = SyntaxKind.ParameterList Then
                            p = p.Parent
                            Return p.Kind = SyntaxKind.DeclareFunctionStatement OrElse p.Kind = SyntaxKind.DeclareSubStatement
                        End If
                    End If
                End If
                Return False
            End Function

            ''' <summary>
            ''' Bind a type or namespace using the given binder. 
            ''' </summary>
            ''' <param name="typeSyntax">The syntax to bind.</param>
            ''' <param name="binder">The binder to bind within. This binder is used for looking up
            ''' unqualified names, accessibility checking, reporting errors, and probably other stuff too.</param>
            ''' <returns>The best type or namespace that can be found, or and ErrorTypeSymbol if no reasonable type can be found.</returns>
            Public Shared Function BindNamespaceOrTypeSyntax(typeSyntax As TypeSyntax,
                                                  binder As Binder,
                                                  diagBag As BindingDiagnosticBag,
                                                  suppressUseSiteError As Boolean) As NamespaceOrTypeSymbol

                Return DirectCast(BindNamespaceOrTypeSyntax(typeSyntax, binder, diagBag, unwrapAliases:=True,
                                                            suppressUseSiteError:=suppressUseSiteError), NamespaceOrTypeSymbol)
            End Function

            Public Shared Function BindNamespaceOrTypeOrAliasSyntax(typeSyntax As TypeSyntax,
                                      binder As Binder,
                                      diagBag As BindingDiagnosticBag,
                                      suppressUseSiteError As Boolean) As Symbol

                Return BindNamespaceOrTypeSyntax(typeSyntax, binder, diagBag, unwrapAliases:=False, suppressUseSiteError:=suppressUseSiteError)
            End Function

            Private Shared Function BindNamespaceOrTypeSyntax(typeSyntax As TypeSyntax,
                                      binder As Binder,
                                      diagBag As BindingDiagnosticBag,
                                      unwrapAliases As Boolean,
                                      suppressUseSiteError As Boolean) As Symbol

                Dim lookupResult As LookupResult = LookupResult.GetInstance()
                Try
                    Dim reportedAnError As Boolean = False
                    LookupTypeOrNamespaceSyntax(lookupResult, typeSyntax, binder, diagBag, reportedAnError,
                                                unwrapAliases, suppressUseSiteError, inGetTypeContext:=False, resolvingBaseType:=False)

                    ' Report appropriate errors from the result.
                    If Not lookupResult.HasSymbol Then
                        Dim diagInfo As DiagnosticInfo = Nothing

                        Dim diagName = GetBaseNamesForDiagnostic(typeSyntax)
                        ' In Imports clauses, a missing namespace or type is just a warning.
                        diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UndefinedTypeOrNamespace1, diagName)
                        Dim reportErrorWhenReferenced = False

                        If typeSyntax.Parent?.Kind = SyntaxKind.SimpleImportsClause Then
                            If DirectCast(typeSyntax.Parent, SimpleImportsClauseSyntax).Alias IsNot Nothing Then
                                reportErrorWhenReferenced = True
                            End If

                            If Not reportedAnError Then
                                Binder.ReportDiagnostic(diagBag, typeSyntax, ErrorFactory.ErrorInfo(ERRID.WRN_UndefinedOrEmptyNamespaceOrClass1, diagName))
                                reportedAnError = True
                            End If
                        End If

                        If Not reportedAnError Then
                            Binder.ReportDiagnostic(diagBag, typeSyntax, diagInfo)
                        End If

                        Return Binder.GetErrorSymbol(diagName, diagInfo, reportErrorWhenReferenced)
                    Else
                        If lookupResult.HasDiagnostic Then
                            If Not reportedAnError Then
                                Binder.ReportDiagnostic(diagBag, typeSyntax, lookupResult.Diagnostic)
                            End If

                            Return ErrorTypeFromLookupResult(lookupResult.SingleSymbol.Name, lookupResult, binder)
                        End If

                        ' LookupTypeOrNamespaceSyntax can't return more than one symbol.
                        Dim result = lookupResult.SingleSymbol
                        binder.ReportDiagnosticsIfObsoleteOrNotSupported(diagBag, result, typeSyntax)
                        binder.AddTypesAssemblyAsDependency(TryCast(result, NamedTypeSymbol), diagBag)
                        Return result
                    End If
                Finally
                    lookupResult.Free()
                End Try
            End Function

            ''' <summary>
            ''' Lookup a typeSyntax, confining the lookup to namespaces or types. Returns a LookupResult
            ''' that summarizes the results of the lookup, which might contain a Diagnostic associated with the lookup.
            ''' However, other diagnostics associated with parts of the binding process (i.e., binding type arguments) 
            ''' will be emitted via the diagnostic bag.
            ''' 
            ''' The LookupResult will always have at most one symbol in it, since types and namespaces are not overloadable symbols.
            ''' </summary>
            Private Shared Sub LookupTypeOrNamespaceSyntax(lookupResult As LookupResult,
                                                          typeSyntax As TypeSyntax,
                                                          binder As Binder,
                                                          diagBag As BindingDiagnosticBag,
                                                          ByRef reportedAnError As Boolean,
                                                          unwrapAliases As Boolean,
                                                          suppressUseSiteError As Boolean,
                                                          inGetTypeContext As Boolean,
                                                          resolvingBaseType As Boolean)
                Debug.Assert(lookupResult.IsClear)

                Select Case typeSyntax.Kind
                    Case SyntaxKind.IdentifierName
                        LookupBasicName(lookupResult, DirectCast(typeSyntax, IdentifierNameSyntax), binder, diagBag, reportedAnError)

                        If unwrapAliases AndAlso
                            lookupResult.IsGood AndAlso
                            lookupResult.SingleSymbol.Kind = SymbolKind.Alias Then

                            Debug.Assert(lookupResult.HasSingleSymbol)
                            lookupResult.ReplaceSymbol(DirectCast(lookupResult.SingleSymbol, AliasSymbol).Target)
                        End If

                    Case SyntaxKind.GenericName
                        LookupGenericName(lookupResult, DirectCast(typeSyntax, GenericNameSyntax), binder, diagBag, reportedAnError, suppressUseSiteError)

                    Case SyntaxKind.QualifiedName
                        LookupDottedName(lookupResult, DirectCast(typeSyntax, QualifiedNameSyntax), binder, diagBag, reportedAnError, suppressUseSiteError, resolvingBaseType:=resolvingBaseType)

                    Case SyntaxKind.GlobalName
                        lookupResult.SetFrom(LookupGlobalName(DirectCast(typeSyntax, GlobalNameSyntax), binder))

                    Case SyntaxKind.PredefinedType
                        lookupResult.SetFrom(LookupPredefinedTypeName(DirectCast(typeSyntax, PredefinedTypeSyntax), binder, diagBag, reportedAnError, suppressUseSiteError))

                    Case SyntaxKind.ArrayType
                        lookupResult.SetFrom(LookupArrayType(DirectCast(typeSyntax, ArrayTypeSyntax), binder, diagBag, suppressUseSiteError, inGetTypeContext:=inGetTypeContext))

                    Case SyntaxKind.NullableType
                        lookupResult.SetFrom(LookupNullableType(DirectCast(typeSyntax, NullableTypeSyntax), binder, diagBag, suppressUseSiteError))

                    Case SyntaxKind.TupleType
                        lookupResult.SetFrom(LookupTupleType(DirectCast(typeSyntax, TupleTypeSyntax), binder, diagBag, suppressUseSiteError, inGetTypeContext, resolvingBaseType))

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(typeSyntax.Kind)
                End Select

                If resolvingBaseType AndAlso lookupResult.IsGood Then
                    Debug.Assert(binder.BasesBeingResolved.InheritsBeingResolvedOpt IsNot Nothing AndAlso binder.BasesBeingResolved.InheritsBeingResolvedOpt.Any)
                    AnalyzeLookupResultForIllegalBaseTypeReferences(lookupResult, typeSyntax, binder, diagBag, reportedAnError)
                End If
            End Sub

            Private Shared Function LookupTupleType(syntax As TupleTypeSyntax,
                                                    binder As Binder,
                                                    diagnostics As BindingDiagnosticBag,
                                                    suppressUseSiteError As Boolean,
                                                    inGetTypeContext As Boolean,
                                                    resolvingBaseType As Boolean) As TypeSymbol

                Dim numElements As Integer = syntax.Elements.Count
                Dim types = ArrayBuilder(Of TypeSymbol).GetInstance(numElements)
                Dim locations = ArrayBuilder(Of Location).GetInstance(numElements)
                Dim elementNames As ArrayBuilder(Of String) = Nothing

                ' set of names already used
                Dim uniqueFieldNames = New HashSet(Of String)(IdentifierComparison.Comparer)
                Dim hasExplicitNames = False

                For i As Integer = 0 To numElements - 1
                    Dim argumentSyntax = syntax.Elements(i)

                    Dim argumentType As TypeSymbol = Nothing
                    Dim name As String = Nothing
                    Dim nameSyntax As SyntaxToken = Nothing

                    If argumentSyntax.Kind = SyntaxKind.TypedTupleElement Then
                        Dim typedElement = DirectCast(argumentSyntax, TypedTupleElementSyntax)
                        argumentType = binder.BindTypeSyntax(typedElement.Type, diagnostics, suppressUseSiteError, inGetTypeContext, resolvingBaseType)

                    Else
                        Dim namedElement = DirectCast(argumentSyntax, NamedTupleElementSyntax)
                        nameSyntax = namedElement.Identifier
                        name = nameSyntax.GetIdentifierText()

                        argumentType = binder.DecodeIdentifierType(nameSyntax, namedElement.AsClause, getRequireTypeDiagnosticInfoFunc:=Nothing, diagBag:=diagnostics)
                    End If

                    types.Add(argumentType)

                    If nameSyntax.Kind() = SyntaxKind.IdentifierToken Then
                        ' validate name if we have one
                        hasExplicitNames = True
                        Binder.CheckTupleMemberName(name, i, nameSyntax, diagnostics, uniqueFieldNames)
                        locations.Add(nameSyntax.GetLocation)
                    Else
                        locations.Add(argumentSyntax.GetLocation)
                    End If

                    Binder.CollectTupleFieldMemberName(name, i, numElements, elementNames)
                Next

                If hasExplicitNames Then
                    ' If the tuple type with names is bound then we must have the TupleElementNamesAttribute to emit
                    ' it is typically there though, if we have ValueTuple at all
                    ' and we need System.String as well

                    Dim constructorSymbol = TryCast(binder.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames), MethodSymbol)
                    If constructorSymbol Is Nothing Then
                        Binder.ReportDiagnostic(diagnostics, syntax, ERRID.ERR_TupleElementNamesAttributeMissing, AttributeDescription.TupleElementNamesAttribute.FullName)
                    Else
                        diagnostics.Add(GetUseSiteInfoForWellKnownTypeMember(constructorSymbol, WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames, embedVBRuntimeUsed:=False),
                                        syntax)
                    End If
                End If

                Dim typesArray As ImmutableArray(Of TypeSymbol) = types.ToImmutableAndFree()
                Dim locationsArray As ImmutableArray(Of Location) = locations.ToImmutableAndFree()

                If typesArray.Length < 2 Then
                    Throw ExceptionUtilities.UnexpectedValue(typesArray.Length)
                End If

                Return TupleTypeSymbol.Create(syntax.GetLocation,
                                                typesArray,
                                                locationsArray,
                                                If(elementNames Is Nothing, Nothing, elementNames.ToImmutableAndFree()),
                                                binder.Compilation,
                                                binder.ShouldCheckConstraints,
                                                errorPositions:=Nothing,
                                                syntax:=syntax,
                                                diagnostics:=diagnostics)
            End Function

            Private Shared Sub AnalyzeLookupResultForIllegalBaseTypeReferences(lookupResult As LookupResult,
                                                                               typeSyntax As TypeSyntax,
                                                                               binder As Binder,
                                                                               diagBag As BindingDiagnosticBag,
                                                                               ByRef reportedAnError As Boolean)

                Debug.Assert(lookupResult.IsGood AndAlso lookupResult.HasSingleSymbol)

                ' In case we are binding the base type we need to check if the symbol we found is a nested 
                ' type of the type which base we are being binding. Note that we need to do the check at 
                ' all lookup phases instead of only checking the final type because in the following case, 
                ' for example, the final type will be A.X with any relation to B already wiped out:
                '
                '   Class A
                '       Public Class X
                '       End Class
                '   End Class
                '   Class B  <-- 'typeWithBaseBeingResolved' variable below
                '       Inherits B.C.X   <-- binding this base, still need to report BC31446
                '       Public Class C
                '           Inherits A
                '       End Class
                '   End Class
                '

                ' The current containing type is the one whose base is being resolved; as we 
                '   currently bind this class' Inherits clause, we need to check all the 
                '   types we go through for being nested in it or to be inherited from it 
                '   or its nested types
                Dim typeWithBaseBeingResolved As NamedTypeSymbol = binder.ContainingType

                Dim currentSymbol As Symbol = lookupResult.SingleSymbol
                While currentSymbol IsNot Nothing AndAlso currentSymbol.Kind = SymbolKind.NamedType

                    If typeWithBaseBeingResolved.Equals(currentSymbol.OriginalDefinition) Then

                        If currentSymbol Is lookupResult.SingleSymbol Then
                            Binder.ReportDiagnostic(diagBag, typeSyntax, ERRID.ERR_TypeInItsInheritsClause1, typeWithBaseBeingResolved)

                        Else
                            Binder.ReportDiagnostic(diagBag, typeSyntax,
                                                    ERRID.ERR_NestedTypeInInheritsClause2, typeWithBaseBeingResolved, lookupResult.SingleSymbol)

                        End If

                        ' and clear lookup result
                        lookupResult.Clear()
                        reportedAnError = True
                        Exit While
                    End If

                    currentSymbol = currentSymbol.ContainingSymbol
                End While
            End Sub

            Private Shared Function ErrorTypeFromLookupResult(name As String, result As LookupResult, binder As Binder) As ErrorTypeSymbol
                If result.Kind = LookupResultKind.Ambiguous AndAlso result.HasSingleSymbol AndAlso TypeOf result.Diagnostic Is AmbiguousSymbolDiagnostic Then
                    ' Special case: set of ambiguous symbols is stored in the diagnostics.
                    Return Binder.GetErrorSymbol(name, result.Diagnostic, DirectCast(result.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols, result.Kind)
                End If
                Return Binder.GetErrorSymbol(name, result.Diagnostic, result.Symbols.ToImmutable(), result.Kind)
            End Function

            ''' <summary>
            ''' Bind a built in type name to the correct type symbol.
            ''' </summary>
            Private Shared Function LookupPredefinedTypeName(predefinedTypeSyntax As PredefinedTypeSyntax,
                                                             binder As Binder,
                                                             diagBag As BindingDiagnosticBag,
                                                             ByRef reportedAnError As Boolean,
                                                             suppressUseSiteError As Boolean) As SingleLookupResult
                Return LookupPredefinedTypeName(predefinedTypeSyntax, predefinedTypeSyntax.Keyword.Kind, binder, diagBag, reportedAnError, suppressUseSiteError)
            End Function

            Public Shared Function LookupPredefinedTypeName(node As VisualBasicSyntaxNode,
                                                            predefinedType As SyntaxKind,
                                                            binder As Binder,
                                                            diagBag As BindingDiagnosticBag,
                                                            ByRef reportedAnError As Boolean,
                                                            suppressUseSiteError As Boolean) As SingleLookupResult
                Dim type As SpecialType
                Select Case predefinedType
                    Case SyntaxKind.ObjectKeyword
                        type = SpecialType.System_Object
                    Case SyntaxKind.BooleanKeyword
                        type = SpecialType.System_Boolean
                    Case SyntaxKind.DateKeyword
                        type = SpecialType.System_DateTime
                    Case SyntaxKind.CharKeyword
                        type = SpecialType.System_Char
                    Case SyntaxKind.StringKeyword
                        type = SpecialType.System_String
                    Case SyntaxKind.DecimalKeyword
                        type = SpecialType.System_Decimal
                    Case SyntaxKind.ByteKeyword
                        type = SpecialType.System_Byte
                    Case SyntaxKind.SByteKeyword
                        type = SpecialType.System_SByte
                    Case SyntaxKind.UShortKeyword
                        type = SpecialType.System_UInt16
                    Case SyntaxKind.ShortKeyword
                        type = SpecialType.System_Int16
                    Case SyntaxKind.UIntegerKeyword
                        type = SpecialType.System_UInt32
                    Case SyntaxKind.IntegerKeyword
                        type = SpecialType.System_Int32
                    Case SyntaxKind.ULongKeyword
                        type = SpecialType.System_UInt64
                    Case SyntaxKind.LongKeyword
                        type = SpecialType.System_Int64
                    Case SyntaxKind.SingleKeyword
                        type = SpecialType.System_Single
                    Case SyntaxKind.DoubleKeyword
                        type = SpecialType.System_Double
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(predefinedType)
                End Select

                Dim sym = binder.GetSpecialType(type, node, diagBag, reportedAnError, suppressUseSiteError)
                Return SingleLookupResult.Good(sym)
            End Function

            ''' <summary>
            ''' Bind array type syntax to the correct type symbol.
            ''' </summary>
            Private Shared Function LookupArrayType(arrayTypeSyntax As ArrayTypeSyntax,
                                                    binder As Binder,
                                                    diagBag As BindingDiagnosticBag,
                                                    suppressUseSiteError As Boolean,
                                                    inGetTypeContext As Boolean) As SingleLookupResult
                Dim elementType As TypeSymbol = binder.BindTypeSyntax(arrayTypeSyntax.ElementType,
                                                                      diagBag,
                                                                      suppressUseSiteError:=suppressUseSiteError,
                                                                      inGetTypeContext:=inGetTypeContext)
                Return SingleLookupResult.Good(binder.ApplyArrayRankSpecifiersToType(elementType, arrayTypeSyntax.RankSpecifiers, diagBag))
            End Function

            ''' <summary>
            ''' Bind Nullable (?) type syntax to the correct type symbol.
            ''' </summary>
            Private Shared Function LookupNullableType(nullableTypeSyntax As NullableTypeSyntax,
                                                       binder As Binder,
                                                       diagBag As BindingDiagnosticBag,
                                                       suppressUseSiteError As Boolean) As SingleLookupResult
                Dim elementType As TypeSymbol = binder.BindTypeSyntax(nullableTypeSyntax.ElementType, diagBag, suppressUseSiteError)
                Return SingleLookupResult.Good(binder.CreateNullableOf(elementType, nullableTypeSyntax, nullableTypeSyntax.ElementType, diagBag))
            End Function

            ''' <summary>
            ''' Bind a basic name to a type or namespace.
            ''' </summary>
            Private Shared Sub LookupBasicName(lookupResult As LookupResult,
                                               basicNameSyntax As IdentifierNameSyntax,
                                               binder As Binder,
                                               diagBag As BindingDiagnosticBag,
                                               ByRef reportedAnError As Boolean)
                ' Get the identifier to look up.
                Dim idSyntax As SyntaxToken = basicNameSyntax.Identifier
                Dim idText As String = idSyntax.ValueText
                Binder.DisallowTypeCharacter(idSyntax, diagBag)

                ' Lookup the text in the current binder.
                If String.IsNullOrEmpty(idText) Then
                    ' Syntax error.
                    reportedAnError = True
                    Debug.Assert(lookupResult.IsClear)
                Else
                    Dim useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagBag)

                    If SyntaxFacts.IsAttributeName(basicNameSyntax) Then
                        binder.LookupAttributeType(lookupResult, Nothing, idText, LookupOptions.AttributeTypeOnly, useSiteInfo)
                    Else
                        binder.Lookup(lookupResult, idText, 0, LookupOptions.NamespacesOrTypesOnly, useSiteInfo)
                    End If

                    diagBag.Add(basicNameSyntax, useSiteInfo)
                End If
            End Sub

            ''' <summary>
            ''' Bind a generic name to a type.
            ''' </summary>
            Private Shared Sub LookupGenericName(lookupResult As LookupResult,
                                                 genericNameSyntax As GenericNameSyntax,
                                                 binder As Binder,
                                                 diagBag As BindingDiagnosticBag,
                                                 ByRef reportedAnError As Boolean,
                                                 suppressUseSiteError As Boolean)
                Debug.Assert(lookupResult.IsClear)

                ' Get the identifier to look up.
                Dim idSyntax As SyntaxToken = genericNameSyntax.Identifier
                Dim idText As String = idSyntax.ValueText
                Binder.DisallowTypeCharacter(idSyntax, diagBag)

                ' Get type arguments and arity we're looking up
                Dim typeArgumentsSyntax = genericNameSyntax.TypeArgumentList
                Dim arity As Integer = typeArgumentsSyntax.Arguments.Count

                ' Bind the generic symbol with the current binder.
                Dim useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagBag)
                binder.Lookup(lookupResult, idText, arity, LookupOptions.NamespacesOrTypesOnly, useSiteInfo)

                diagBag.Add(genericNameSyntax, useSiteInfo)

                ' Bind the type arguments and report errors in the current context. 
                Dim typeArguments As ImmutableArray(Of TypeSymbol) = BindTypeArguments(typeArgumentsSyntax, binder, diagBag, suppressUseSiteError)

                If lookupResult.Kind = LookupResultKind.Empty Then
                    Return
                End If

                Dim genericType = TryCast(lookupResult.SingleSymbol, NamedTypeSymbol)
                If genericType Is Nothing OrElse
                   Not genericType.IsGenericType OrElse
                   Not genericType.CanConstruct Then
                    ' not generic symbol or symbol is a namespace
                    If lookupResult.IsGood Then
                        ' TODO: Dev10 squiggles type arguments for this error, but Roslyn will squiggle the whole type name.
                        '       If we want to preserve Dev10 behavior, it should be possible to provide optional location/syntax node 
                        '       for the diagnostic attached to LookupResult.
                        lookupResult.SetFrom(SingleLookupResult.WrongArity(lookupResult.SingleSymbol,
                                New BadSymbolDiagnostic(lookupResult.SingleSymbol, ERRID.ERR_TypeOrMemberNotGeneric1, lookupResult.SingleSymbol)))
                    End If
                Else
                    If Not suppressUseSiteError Then
                        If ReportUseSite(diagBag, genericNameSyntax, genericType) Then
                            reportedAnError = True
                        End If
                    End If

                    ' Construct the type and validate constraints.
                    Dim constructedType = binder.ConstructAndValidateConstraints(
                        genericType, typeArguments, genericNameSyntax, typeArgumentsSyntax.Arguments, diagBag)

                    ' Put the constructed type in. Note that this preserves any error associated with the lookupResult.
                    lookupResult.ReplaceSymbol(constructedType)
                End If
            End Sub

            ''' <summary>
            ''' Bind a dotted name to a type or namespace.
            ''' </summary>
            Private Shared Sub LookupDottedName(lookupResult As LookupResult,
                                                dottedNameSyntax As QualifiedNameSyntax,
                                                binder As Binder,
                                                diagBag As BindingDiagnosticBag,
                                                ByRef reportedAnError As Boolean,
                                                suppressUseSiteError As Boolean,
                                                resolvingBaseType As Boolean)
                Debug.Assert(lookupResult.IsClear)
                Dim right = TryCast(dottedNameSyntax.Right, GenericNameSyntax)

                If right IsNot Nothing Then
                    LookupGenericDottedName(lookupResult, dottedNameSyntax, binder, diagBag,
                                            reportedAnError, suppressUseSiteError, resolvingBaseType)
                    Return
                End If

                Dim rightIdentSyntax As SimpleNameSyntax = dottedNameSyntax.Right
                Dim rightIdentToken As SyntaxToken = rightIdentSyntax.Identifier
                Dim leftNameSyntax As NameSyntax = dottedNameSyntax.Left

                Binder.DisallowTypeCharacter(rightIdentToken, diagBag)

                LookupTypeOrNamespaceSyntax(lookupResult, leftNameSyntax, binder, diagBag, reportedAnError,
                                            unwrapAliases:=True, suppressUseSiteError:=suppressUseSiteError,
                                            inGetTypeContext:=False, resolvingBaseType:=resolvingBaseType)

                If Not lookupResult.HasSymbol Then
                    Return
                ElseIf lookupResult.HasDiagnostic AndAlso Not reportedAnError Then
                    Binder.ReportDiagnostic(diagBag, leftNameSyntax, lookupResult.Diagnostic)
                    reportedAnError = (lookupResult.Diagnostic.Severity = DiagnosticSeverity.Error)
                End If

                Dim leftSymbol As NamespaceOrTypeSymbol = DirectCast(lookupResult.SingleSymbol, NamespaceOrTypeSymbol)

                binder.ReportDiagnosticsIfObsoleteOrNotSupported(diagBag, leftSymbol, leftNameSyntax)
                binder.AddTypesAssemblyAsDependency(leftSymbol, diagBag)

                lookupResult.Clear()
                Dim useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagBag)

                If SyntaxFacts.IsAttributeName(rightIdentSyntax) Then
                    binder.LookupAttributeType(lookupResult,
                                leftSymbol,
                                rightIdentToken.ValueText,
                                LookupOptions.AttributeTypeOnly,
                                useSiteInfo)
                Else
                    Dim isLeftUnboundGenericType As Boolean = leftSymbol.Kind = SymbolKind.NamedType AndAlso DirectCast(leftSymbol, NamedTypeSymbol).IsUnboundGenericType

                    If isLeftUnboundGenericType Then
                        ' If left name bound to an unbound generic type,
                        ' we want to perform right name lookup within
                        ' left's original named type definition.
                        leftSymbol = DirectCast(leftSymbol, NamedTypeSymbol).OriginalDefinition
                    End If

                    binder.LookupMember(lookupResult,
                                        leftSymbol,
                                        rightIdentToken.ValueText,
                                        0,
                                        LookupOptions.NamespacesOrTypesOnly,
                                        useSiteInfo)

                    If lookupResult.HasSingleSymbol AndAlso lookupResult.SingleSymbol.Kind = SymbolKind.NamedType Then
                        Dim namedType = DirectCast(lookupResult.SingleSymbol, NamedTypeSymbol)

                        ' If left name bound to an unbound generic type
                        ' and right name bound to a generic type, we must
                        ' convert right to an unbound generic type.
                        If isLeftUnboundGenericType AndAlso namedType.IsGenericType Then
                            lookupResult.ReplaceSymbol(namedType.AsUnboundGenericType())

                        ElseIf namedType.Arity > 0 AndAlso Not namedType.IsDefinition AndAlso namedType Is namedType.ConstructedFrom Then
                            Debug.Assert(lookupResult.HasDiagnostic)

                            ' Note: this preserves any error associated with the generic type, which is what we want.
                            lookupResult.ReplaceSymbol(namedType.Construct(
                                                           StaticCast(Of TypeSymbol).From(namedType.OriginalDefinition.TypeParameters)))
                        End If
                    End If
                End If

                diagBag.Add(leftNameSyntax, useSiteInfo)
            End Sub

            ''' <summary>
            ''' Bind a generic dotted name to a type or namespace.
            ''' </summary>
            Private Shared Sub LookupGenericDottedName(lookupResult As LookupResult,
                                                       genDottedNameSyntax As QualifiedNameSyntax,
                                                       binder As Binder,
                                                       diagBag As BindingDiagnosticBag,
                                                       ByRef reportedAnError As Boolean,
                                                       suppressUseSiteError As Boolean,
                                                       resolvingBaseType As Boolean)
                Debug.Assert(lookupResult.IsClear)
                Debug.Assert(TryCast(genDottedNameSyntax.Right, GenericNameSyntax) IsNot Nothing)

                Dim right = DirectCast(genDottedNameSyntax.Right, GenericNameSyntax)
                Dim rightIdentSyntax As SyntaxToken = right.Identifier
                Dim leftNameSyntax As NameSyntax = genDottedNameSyntax.Left

                Binder.DisallowTypeCharacter(rightIdentSyntax, diagBag)

                ' Get type arguments and arity we're looking up
                Dim typeArgumentsSyntax = right.TypeArgumentList
                Dim arity As Integer = typeArgumentsSyntax.Arguments.Count

                ' Get the symbol on the left.
                LookupTypeOrNamespaceSyntax(lookupResult, leftNameSyntax, binder, diagBag, reportedAnError,
                                            unwrapAliases:=True, suppressUseSiteError:=suppressUseSiteError,
                                            inGetTypeContext:=False, resolvingBaseType:=resolvingBaseType)

                ' Bind the type arguments and report errors in the current context. 
                Dim typeArguments As ImmutableArray(Of TypeSymbol) = BindTypeArguments(typeArgumentsSyntax, binder, diagBag, suppressUseSiteError)

                If Not lookupResult.HasSymbol Then
                    Return
                ElseIf lookupResult.HasDiagnostic AndAlso Not reportedAnError Then
                    Binder.ReportDiagnostic(diagBag, leftNameSyntax, lookupResult.Diagnostic)
                    reportedAnError = (lookupResult.Diagnostic.Severity = DiagnosticSeverity.Error)
                End If

                Dim leftSymbol As NamespaceOrTypeSymbol = DirectCast(lookupResult.SingleSymbol, NamespaceOrTypeSymbol)
                Dim isLeftUnboundGenericType As Boolean = leftSymbol.Kind = SymbolKind.NamedType AndAlso DirectCast(leftSymbol, NamedTypeSymbol).IsUnboundGenericType

                If isLeftUnboundGenericType Then
                    ' If left name bound to an unbound generic type,
                    ' we want to perform right name lookup within
                    ' left's original named type definition.
                    leftSymbol = DirectCast(leftSymbol, NamedTypeSymbol).OriginalDefinition
                End If

                binder.ReportDiagnosticsIfObsoleteOrNotSupported(diagBag, leftSymbol, leftNameSyntax)
                binder.AddTypesAssemblyAsDependency(leftSymbol, diagBag)

                ' Lookup the generic type.
                lookupResult.Clear()
                Dim useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagBag)
                binder.LookupMember(lookupResult,
                                    leftSymbol,
                                    rightIdentSyntax.ValueText,
                                    arity,
                                    LookupOptions.NamespacesOrTypesOnly,
                                    useSiteInfo)

                diagBag.Add(leftNameSyntax, useSiteInfo)

                If lookupResult.Kind = LookupResultKind.Empty Then
                    Return
                End If

                ' Doing a Lookup with NamespaceAndTypesOnly can never result in more than one symbols, because
                ' namespace and types are not overloadable.

                Dim genericType = TryCast(lookupResult.SingleSymbol, NamedTypeSymbol)
                If genericType Is Nothing Then
                    ' no symbol or symbol is a namespace
                    Return

                ElseIf isLeftUnboundGenericType AndAlso genericType.IsGenericType Then
                    ' If left name bound to an unbound generic type
                    ' and right name bound to a generic type, we must
                    ' convert right to an unbound generic type.
                    lookupResult.ReplaceSymbol(genericType.AsUnboundGenericType())

                Else
                    ' Construct the type and validate constraints.
                    Dim constructedType = binder.ConstructAndValidateConstraints(
                        genericType, typeArguments, genDottedNameSyntax, typeArgumentsSyntax.Arguments, diagBag)

                    ' Note: this preserves any error associated with the generic type, which is what we want.
                    lookupResult.ReplaceSymbol(constructedType)
                End If
            End Sub

            ''' <summary>
            ''' Bind to the global namespace.
            ''' </summary>
            Private Shared Function LookupGlobalName(syntax As GlobalNameSyntax,
                                                     binder As Binder) As SingleLookupResult
                Return SingleLookupResult.Good(binder.Compilation.GlobalNamespace)
            End Function

            ''' <summary>
            ''' Bind a list of type arguments to their types.
            ''' </summary>
            Private Shared Function BindTypeArguments(typeArgumentsSyntax As TypeArgumentListSyntax,
                                                      binder As Binder,
                                                      diagBag As BindingDiagnosticBag,
                                                      suppressUseSiteError As Boolean) As ImmutableArray(Of TypeSymbol)
                Dim arity As Integer = typeArgumentsSyntax.Arguments.Count
                Dim types As TypeSymbol() = New TypeSymbol(0 To arity - 1) {}

                For i As Integer = 0 To arity - 1
                    types(i) = binder.BindTypeSyntax(typeArgumentsSyntax.Arguments(i), diagBag, suppressUseSiteError)
                Next

                Return types.AsImmutableOrNull()
            End Function

            ''' <summary>
            ''' Given a type syntax, strip out ?, (), (of xxx) stuff and return a string of the form
            ''' x.y.z, for use in an error message.
            ''' </summary>
            Private Shared Function GetBaseNamesForDiagnostic(typeSyntax As TypeSyntax) As String
                Select Case typeSyntax.Kind
                    Case SyntaxKind.IdentifierName
                        Return DirectCast(typeSyntax, IdentifierNameSyntax).Identifier.ValueText

                    Case SyntaxKind.TupleType
                        Return typeSyntax.ToString

                    Case SyntaxKind.GenericName
                        Return DirectCast(typeSyntax, GenericNameSyntax).Identifier.ValueText

                    Case SyntaxKind.QualifiedName
                        Return GetBaseNamesForDiagnostic(DirectCast(typeSyntax, QualifiedNameSyntax).Left) +
                            "." +
                            DirectCast(typeSyntax, QualifiedNameSyntax).Right.Identifier.ValueText

                    Case SyntaxKind.ArrayType
                        Return GetBaseNamesForDiagnostic(DirectCast(typeSyntax, ArrayTypeSyntax).ElementType)

                    Case SyntaxKind.NullableType
                        Return GetBaseNamesForDiagnostic(DirectCast(typeSyntax, NullableTypeSyntax).ElementType)

                    Case SyntaxKind.GlobalName, SyntaxKind.PredefinedType
                        Return typeSyntax.ToString

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(typeSyntax.Kind)
                End Select

            End Function
        End Class
    End Class
End Namespace
