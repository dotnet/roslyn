' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class EETypeNameDecoder
        Inherits TypeNameDecoder(Of PEModuleSymbol, TypeSymbol)

        Private ReadOnly _compilation As VisualBasicCompilation

        Friend Sub New(compilation As VisualBasicCompilation, moduleSymbol As PEModuleSymbol)
            MyBase.New(SymbolFactory.Instance, moduleSymbol)
            _compilation = compilation
        End Sub

        Protected Overrides Function GetIndexOfReferencedAssembly(identity As AssemblyIdentity) As Integer
            Dim assemblyIdentities = Me.Module.GetReferencedAssemblies()
            ' Find assembly matching identity.
            Dim index = assemblyIdentities.IndexOf(identity)
            If index >= 0 Then
                Return index
            End If
            If identity.IsWindowsComponent() Then
                ' Find placeholder Windows.winmd assembly (created
                ' in MetadataUtilities.MakeAssemblyReferences).
                Dim assemblies = Me.Module.GetReferencedAssemblySymbols()
                index = assemblies.IndexOf(Function(assembly, unused) assembly.Identity.IsWindowsRuntime(), DirectCast(Nothing, Object))
                If index >= 0 Then
                    ' Find module in Windows.winmd matching identity.
                    Dim modules = assemblies(index).Modules
                    Dim moduleIndex = modules.IndexOf(Function(m, id) id.Equals(GetComponentAssemblyIdentity(m)), identity)
                    If moduleIndex >= 0 Then
                        Return index
                    End If
                End If
            End If
            Return -1
        End Function

        Protected Overrides Function IsContainingAssembly(identity As AssemblyIdentity) As Boolean
            Return False
        End Function

        Protected Overrides Function LookupNestedTypeDefSymbol(container As TypeSymbol, ByRef emittedName As MetadataTypeName) As TypeSymbol
            Dim result As NamedTypeSymbol = container.LookupMetadataType(emittedName)
            Debug.Assert(If(Not result?.IsErrorType(), True))

            Return If(result, New MissingMetadataTypeSymbol.Nested(DirectCast(container, NamedTypeSymbol), emittedName))
        End Function

        Protected Overrides Function LookupTopLevelTypeDefSymbol(referencedAssemblyIndex As Integer, ByRef emittedName As MetadataTypeName) As TypeSymbol
            Dim assembly As AssemblySymbol = Me.Module.GetReferencedAssemblySymbol(referencedAssemblyIndex)
            ' GetReferencedAssemblySymbol should not return Nothing since referencedAssemblyIndex
            ' was obtained from GetIndexOfReferencedAssembly above.
            Return assembly.LookupDeclaredOrForwardedTopLevelMetadataType(emittedName, visitedAssemblies:=Nothing)
        End Function

        Protected Overrides Function LookupTopLevelTypeDefSymbol(ByRef emittedName As MetadataTypeName, ByRef isNoPiaLocalType As Boolean) As TypeSymbol
            Return moduleSymbol.LookupTopLevelMetadataType(emittedName, isNoPiaLocalType)
        End Function

        Private Shared Function GetComponentAssemblyIdentity([module] As ModuleSymbol) As AssemblyIdentity
            Return DirectCast([module], PEModuleSymbol).Module.ReadAssemblyIdentityOrThrow()
        End Function

        Protected Overrides Function GetGenericTypeParamSymbol(index As Integer) As TypeSymbol
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function GetGenericMethodTypeParamSymbol(index As Integer) As TypeSymbol
            Throw ExceptionUtilities.Unreachable()
        End Function

        Private ReadOnly Property [Module] As ModuleSymbol
            Get
                Return _compilation.Assembly.Modules.Single()
            End Get
        End Property

    End Class

End Namespace
