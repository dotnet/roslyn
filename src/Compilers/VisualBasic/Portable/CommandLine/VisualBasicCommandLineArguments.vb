' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' The CommandLineArguments class provides members to Set and Get Visual Basic compilation and parse options.
    ''' </summary>
    Public NotInheritable Class VisualBasicCommandLineArguments
        Inherits CommandLineArguments
        ''' <summary>
        ''' Set and Get the Visual Basic compilation options.
        ''' </summary>
        ''' <returns>The currently set Visual Basic compilation options.</returns>
        Public Overloads Property CompilationOptions As VisualBasicCompilationOptions

        ''' <summary>
        ''' Set and Get the Visual Basic parse options.
        ''' </summary>
        ''' <returns>The currently set Visual Basic parse options.</returns>
        Public Overloads Property ParseOptions As VisualBasicParseOptions

        Friend OutputLevel As OutputLevel

        ''' <summary>
        ''' Gets the core Parse options.
        ''' </summary>
        ''' <returns>The currently set core parse options.</returns>
        Protected Overrides ReadOnly Property ParseOptionsCore As ParseOptions
            Get
                Return ParseOptions
            End Get
        End Property

        ''' <summary>
        ''' Gets the core compilation options.
        ''' </summary>
        ''' <returns>The currently set core compilation options.</returns>
        Protected Overrides ReadOnly Property CompilationOptionsCore As CompilationOptions
            Get
                Return CompilationOptions
            End Get
        End Property

        Friend Property DefaultCoreLibraryReference As CommandLineReference?

        Friend Sub New()
        End Sub

        Friend Overrides Function ResolveMetadataReferences(
            metadataResolver As MetadataReferenceResolver,
            diagnostics As List(Of DiagnosticInfo),
            messageProvider As CommonMessageProvider,
            resolved As List(Of MetadataReference)
            ) As Boolean

            Dim result = MyBase.ResolveMetadataReferences(metadataResolver, diagnostics, messageProvider, resolved)

            ' If there were no references, don't try to add default Cor library reference.
            If Me.DefaultCoreLibraryReference IsNot Nothing AndAlso resolved.Count > 0 Then
                ' All references from arguments were resolved successfully. Let's see if we have a reference that can be used as a Cor library.
                For Each reference In resolved
                    If reference.IsUnresolved Then
                        Continue For
                    End If

                    Dim refProps = reference.Properties

                    ' The logic about deciding what assembly is a candidate for being a Cor library here and in
                    ' CommonReferenceManager<TCompilation, TAssemblySymbol>.IndexOfCorLibrary
                    ' should be equivalent.
                    If Not refProps.EmbedInteropTypes AndAlso refProps.Kind = MetadataImageKind.Assembly Then
                        Try
                            Dim assemblyMetadata = TryCast(DirectCast(reference, PortableExecutableReference).GetMetadataNoCopy(), AssemblyMetadata)

                            If assemblyMetadata Is Nothing OrElse Not assemblyMetadata.IsValidAssembly() Then
                                ' There will be some errors reported later.
                                Return result
                            End If

                            Dim assembly As PEAssembly = assemblyMetadata.GetAssembly()

                            If assembly.AssemblyReferences.Length = 0 AndAlso Not assembly.ContainsNoPiaLocalTypes AndAlso assembly.DeclaresTheObjectClass Then
                                ' This reference looks like a valid Cor library candidate, bail out.
                                Return result
                            End If

                        Catch e As BadImageFormatException
                            ' error reported later
                            Return result
                        Catch e As IOException
                            ' error reported later
                            Return result
                        End Try
                    End If
                Next

                ' None of the supplied references could be used as a Cor library. Let's add a default one.
                Dim defaultCorLibrary = ResolveMetadataReference(Me.DefaultCoreLibraryReference.Value, metadataResolver, diagnostics, messageProvider).FirstOrDefault()

                If defaultCorLibrary Is Nothing OrElse defaultCorLibrary.IsUnresolved Then
                    Debug.Assert(diagnostics Is Nothing OrElse diagnostics.Any())
                    Return False
                Else
                    resolved.Insert(0, defaultCorLibrary)
                    Return result
                End If
            End If

            Return result
        End Function

    End Class

    Friend Enum OutputLevel
        Quiet
        Normal
        Verbose
    End Enum

End Namespace

