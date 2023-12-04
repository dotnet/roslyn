' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A <see cref="NonMissingModuleSymbol"/> is a special kind of <see cref="ModuleSymbol"/> that represents
    ''' a module that is not missing, i.e. the "real" thing.
    ''' </summary>
    Friend MustInherit Class NonMissingModuleSymbol
        Inherits ModuleSymbol

        ''' <summary>
        ''' An array of <see cref="AssemblySymbol"/> objects corresponding to assemblies directly referenced by this module.
        ''' </summary>
        ''' <remarks>
        ''' The contents are provided by ReferenceManager and may not be modified.
        ''' </remarks>
        Private _moduleReferences As ModuleReferences(Of AssemblySymbol)

        ''' <summary>
        ''' Does this symbol represent a missing Module.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property IsMissing As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns an array of assembly identities for assemblies referenced by this module.
        ''' Items at the same position from GetReferencedAssemblies and from GetReferencedAssemblySymbols 
        ''' should correspond to each other.
        ''' 
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Friend NotOverridable Overrides Function GetReferencedAssemblies() As ImmutableArray(Of AssemblyIdentity)
            AssertReferencesInitialized()
            Return _moduleReferences.Identities
        End Function

        ''' <summary>
        ''' Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        ''' by this module. Items at the same position from GetReferencedAssemblies and 
        ''' from GetReferencedAssemblySymbols should correspond to each other. If reference is 
        ''' not resolved by compiler, GetReferencedAssemblySymbols returns MissingAssemblySymbol in the
        ''' corresponding item.
        ''' 
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Friend NotOverridable Overrides Function GetReferencedAssemblySymbols() As ImmutableArray(Of AssemblySymbol)
            AssertReferencesInitialized()
            Return _moduleReferences.Symbols
        End Function

        Friend Function GetUnifiedAssemblies() As ImmutableArray(Of UnifiedAssembly(Of AssemblySymbol))
            AssertReferencesInitialized()
            Return _moduleReferences.UnifiedAssemblies
        End Function

        Friend Overrides ReadOnly Property HasUnifiedReferences As Boolean
            Get
                Return GetUnifiedAssemblies().Length > 0
            End Get
        End Property

        Friend Overrides Function GetUnificationUseSiteErrorInfo(dependentType As TypeSymbol) As DiagnosticInfo
            AssertReferencesInitialized()

            If Not HasUnifiedReferences Then
                Return Nothing
            End If

            Dim ownerModule = Me
            Dim ownerAssembly = ownerModule.ContainingAssembly
            Dim dependentAssembly = dependentType.ContainingAssembly
            If ownerAssembly Is dependentAssembly Then
                Return Nothing
            End If

            ' TODO (tomat): we should report an error for all unified references, not just the first one.

            For Each unifiedAssembly In GetUnifiedAssemblies()
                If unifiedAssembly.TargetAssembly IsNot dependentAssembly Then
                    Continue For
                End If

                Dim referenceId = unifiedAssembly.OriginalReference
                Dim definitionId = dependentAssembly.Identity

                If definitionId.Version < referenceId.Version Then
                    ' unified with a definition whose version is lower than the reference
                    Return ErrorFactory.ErrorInfo(
                        ERRID.ERR_SxSIndirectRefHigherThanDirectRef3,
                        definitionId.Name,
                        referenceId.Version.ToString(),
                        definitionId.Version.ToString())
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' A helper method for ReferenceManager to set assembly identities for assemblies 
        ''' referenced by this module and corresponding AssemblySymbols.
        ''' </summary>
        Friend Overrides Sub SetReferences(
            moduleReferences As ModuleReferences(Of AssemblySymbol),
            Optional originatingSourceAssemblyDebugOnly As SourceAssemblySymbol = Nothing)

            Debug.Assert(moduleReferences IsNot Nothing)

            AssertReferencesUninitialized()

            _moduleReferences = moduleReferences
        End Sub

        <Conditional("DEBUG")>
        Friend Sub AssertReferencesUninitialized()
            Debug.Assert(_moduleReferences Is Nothing)
        End Sub

        <Conditional("DEBUG")>
        Friend Sub AssertReferencesInitialized()
            Debug.Assert(_moduleReferences IsNot Nothing)
        End Sub

        ''' <summary>
        ''' Lookup a top level type referenced from metadata, names should be
        ''' compared case-sensitively.
        ''' </summary>
        ''' <param name="emittedName">
        ''' Full type name, possibly with generic name mangling.
        ''' </param>
        ''' <returns>
        ''' Symbol for the type, or Nothing if the type isn't found.
        ''' </returns>
        ''' <remarks></remarks>
        Friend NotOverridable Overrides Function LookupTopLevelMetadataType(
            ByRef emittedName As MetadataTypeName
        ) As NamedTypeSymbol

            Dim result As NamedTypeSymbol
            Dim scope As NamespaceSymbol

            scope = Me.GlobalNamespace.LookupNestedNamespace(emittedName.NamespaceSegments)

            If scope Is Nothing Then
                ' We failed to locate the namespace
                result = Nothing
            Else
                result = scope.LookupMetadataType(emittedName)
            End If

            Debug.Assert(If(Not result?.IsErrorType(), True))
            Return result
        End Function

        Friend Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                ' Only primary module of an assembly marked with an Extension attribute
                ' can contain extension methods recognized by the language (Dev10 behavior).
                Dim assembly As AssemblySymbol = Me.ContainingAssembly
                Return assembly.Modules(0) Is Me AndAlso assembly.MightContainExtensionMethods
            End Get
        End Property

    End Class

End Namespace

