'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Collections.Concurrent
Imports Roslyn.Compilers.Internal.Contract
Imports Roslyn.Compilers.MetadataReader
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' Represents source or metadata module.
    ''' </summary>
    ''' <remarks></remarks>
    Friend MustInherit Class MetadataOrSourceModuleSymbol
        Inherits ModuleSymbol

        ''' <summary>
        ''' An array of assembly identities for assemblies referenced by this module.
        ''' The array and its content is provided by AssemblyManager and must not be modified.
        ''' Items at the same position from this array and from ModuleSymbol.m_ReferencedAssemblySymbols
        ''' should correspond to each other. This array is returned by GetReferencedAssemblies() method.
        ''' </summary>
        ''' <remarks></remarks>
        Private m_ReferencedAssemblies() As System.Reflection.AssemblyName

        ''' <summary>
        ''' The system assembly, which provides primitive types like Object, String, etc., think mscorlib.dll. 
        ''' The value is provided by AssemblyManager and must not be modified. The AssemblySymbol must match
        ''' one of the referenced assemblies returned by GetReferencedAssemblySymbols() method or the owning
        ''' assembly. If none of the candidate assemblies can be used as a source for the primitive types, 
        ''' the value is a null reference. 
        ''' </summary>
        ''' <remarks></remarks>
        Private m_CorLibrary As AssemblySymbol

        ''' <summary>
        ''' Returns an array of assembly identities for assemblies referenced by this module.
        ''' Items at the same position from GetReferencedAssemblies and from GetReferencedAssemblySymbols 
        ''' should correspond to each other.
        ''' 
        ''' The array and its content is provided by AssemblyManager and must not be modified.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overrides Function GetReferencedAssemblies() As System.Reflection.AssemblyName()
            System.Diagnostics.Debug.Assert(m_ReferencedAssemblies IsNot Nothing AndAlso
                                            m_ReferencedAssemblySymbols IsNot Nothing AndAlso
                                            m_ReferencedAssemblies.Length =
                                            m_ReferencedAssemblySymbols.Length)
            Return m_ReferencedAssemblies
        End Function


        ''' <summary>
        ''' The system assembly, which provides primitive types like Object, String, etc., think mscorlib.dll. 
        ''' The value is a null reference if none of the referenced assemblies can be used as a source for the 
        ''' primitive types and the owning assembly cannot be used as the source too. Otherwise, it is one of 
        ''' the referenced assemblies returned by GetReferencedAssemblySymbols() method or the owning assembly.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend ReadOnly Property CorLibrary As AssemblySymbol
            Get
                System.Diagnostics.Debug.Assert(m_ReferencedAssemblies IsNot Nothing AndAlso
                                                m_ReferencedAssemblySymbols IsNot Nothing AndAlso
                                                m_ReferencedAssemblies.Length =
                                                m_ReferencedAssemblySymbols.Length)
                Return m_CorLibrary
            End Get
        End Property


        ''' <summary>
        ''' A helper method for AssemblyManager to set assembly identities for assemblies 
        ''' referenced by this module and corresponding AssemblySymbols.
        ''' </summary>
        ''' <param name="names"></param>
        ''' <param name="symbols"></param>
        ''' <remarks></remarks>
        Friend Sub SetReferences(
            ByVal names() As Reflection.AssemblyName,
            ByVal symbols() As AssemblySymbol
        )
            ThrowIfFalse(names IsNot Nothing AndAlso
                         symbols IsNot Nothing AndAlso
                         names.Length = symbols.Length)

            Debug.Assert(m_ReferencedAssemblies Is Nothing)
            Debug.Assert(m_ReferencedAssemblySymbols Is Nothing)
            Debug.Assert(m_CorLibrary Is Nothing)

            m_ReferencedAssemblies = names
            m_ReferencedAssemblySymbols = symbols
            m_CorLibrary = Nothing
        End Sub


        ''' <summary>
        ''' A helper method for AssemblyManager to set the system assembly, which provides primitive 
        ''' types like Object, String, etc., think mscorlib.dll. 
        ''' </summary>
        ''' <param name="corLibrary"></param>
        ''' <remarks></remarks>
        Friend Sub SetCorLibrary(
            ByVal corLibrary As AssemblySymbol
        )
            ThrowIfNull(m_ReferencedAssemblies)
            ThrowIfNull(m_ReferencedAssemblySymbols)

            If corLibrary IsNot Nothing AndAlso corLibrary IsNot Me.ContainingAssembly Then
                Dim corLibraryIsOk As Boolean = False

                For Each asm In m_ReferencedAssemblySymbols
                    If asm Is corLibrary Then
                        corLibraryIsOk = True
                        Exit For
                    End If
                Next

                ThrowIfFalse(corLibraryIsOk)
            End If

            Debug.Assert(m_CorLibrary Is Nothing)
            m_CorLibrary = corLibrary
        End Sub


        ''' <summary>
        ''' Get symbol for predefined type from Cor Library referenced by this module.
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function GetCorLibType(ByVal type As CorLibTypes.TypeId) As NamedTypeSymbol

            Dim mscorlibAssembly = CorLibrary

            If mscorlibAssembly Is Nothing Then

                Dim arity As Integer = 0
                Dim actualName = Utilities.GetActualTypeNameFromEmittedTypeName(CorLibTypes.GetEmittedName(type), -1, arity)
                Return New MissingMetadataTypeSymbol(New System.Reflection.AssemblyName("mscorlib"),
                                                      actualName, arity, Me)
            Else
                Return mscorlibAssembly.GetDeclaredCorType(type)
            End If

        End Function

    End Class

End Namespace
