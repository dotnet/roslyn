' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a reference to another Visual Basic compilation. 
    ''' </summary>
    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Friend NotInheritable Class VisualBasicCompilationReference
        Inherits CompilationReference

        Private ReadOnly _compilation As VisualBasicCompilation

        ''' <summary>
        ''' Returns the referenced <see cref="Compilation"/>.
        ''' </summary>
        Public Shadows ReadOnly Property Compilation As VisualBasicCompilation
            Get
                Return _compilation
            End Get
        End Property

        Friend Overrides ReadOnly Property CompilationCore As Compilation
            Get
                Return _compilation
            End Get
        End Property

#If Retargeting Then
        Shared retargetingreferenceCount As Integer = 0
#End If

        ''' <summary>
        ''' Create a metadata reference to a compilation.
        ''' </summary>
        ''' <param name="compilation">The compilation to reference.</param>
        ''' <param name="embedInteropTypes">Should interop types be embedded in the created assembly?</param>
        ''' <param name="aliases">Namespace aliases for this reference.</param>
        Public Sub New(compilation As VisualBasicCompilation, Optional aliases As ImmutableArray(Of String) = Nothing, Optional embedInteropTypes As Boolean = False)
            MyBase.New(GetProperties(compilation, aliases, embedInteropTypes))

            Dim newCompilation As VisualBasicCompilation = Nothing
            'This retargeting code should only be enabled to verify all compilation references used in unit tests continue to work correctly
            ' when the mscorlib of the referenced assembly is changed to an earlier mscorlib causing retargeting to occur.
            ' Only enable this code if this retargeting functionality is required to be tested.    
#If Retargeting Then
            retargetingreferenceCount += 1
            Console.WriteLine("Created Compilation Reference :" & retargetingreferenceCount .ToString)

            Dim OldReference As MetadataReference = Nothing
            Dim OldVBReference As MetadataReference = Nothing

            'For Retargeting - I want to ensure that if mscorlib v4 is detected then I will retarget to V2.
            'This should apply to mscorlib, microsoft.visualbasic, system

            'Exist V2 reference
            Dim bAbleToRetargetToV2 As Boolean = False
            For Each r In compilation.References
                Dim Item As String = r.Display
                If r.Display.Contains("mscorlib") And r.Display.Contains("v4") Then
                    bAbleToRetargetToV2 = True
                End If
            Next

            'Verify is mscorlib/Microsoft.VisualBasic and System references are present that are v4       
            If bAbleToRetargetToV2 Then
                Dim AssembliesToRetarget As Integer = 0
                For Each r In compilation.References
                    Dim Item As String = r.Display
                    If r.Display.Contains("mscorlib") And r.Display.Contains("v4") Then
                        OldReference = r
                        AssembliesToRetarget = AssembliesToRetarget + 1
                    ElseIf r.Display.Contains("Microsoft.VisualBasic") And r.Display.Contains("v4") Then
                        OldVBReference = r
                        AssembliesToRetarget = AssembliesToRetarget + 2
                    'ElseIf r.Display.Contains("System") And r.Display.Contains("v4") Then
                        'bfound = bfound + 4
                    End If
                Next

                If AssembliesToRetarget = 0 Then
                    NewCompilation = compilation
                Else
                    'Retarget to use v2.0 assemblies - these are hardcode file reference and dependent upon 2.0 framework being present because
                    'I didn't want for compiler binaries to have dependence on test resources which have fixed versions of these assemblies.
                    Dim x = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                    Dim Parts() = x.Split(System.IO.Path.DirectorySeparatorChar)
                    Dim version = ""
                    If Parts.Count > 2 Then version = Parts(Parts.Count - 2)                
                    Dim Netfx2Path = x.Replace(version, "V2.0.50727")

                    If AssembliesToRetarget = 1 Then
                        NewCompilation = compilation.ReplaceReference(oldReference:=OldReference, newReference:=New MetadataFileReference(System.IO.Path.Combine(Netfx2Path, "mscorlib.dll")))
                    ElseIf AssembliesToRetarget = 2 Then
                        NewCompilation = compilation.ReplaceReference(oldReference:=OldVBReference, newReference:=New MetadataFileReference(System.IO.Path.Combine(Netfx2Path, "Microsoft.VisualBasic.dll")))
                    ElseIf AssembliesToRetarget = 3 Then
                        NewCompilation = compilation.ReplaceReference(oldReference:=OldReference, newReference:=New MetadataFileReference(AssemblyPaths.NetFx.v2_0_50727.mscorlib.dll)).ReplaceReference(oldReference:=OldVBReference, newReference:=New MetadataFileReference(AssemblyPaths.NetFx.v2_0_50727.Microsoft_VisualBasic.dll))
                    End If
                    NewCompilation = compilation
                End If
            Else
                'No retargeting
                NewCompilation = compilation
            End If
#Else
            newCompilation = compilation
#End If
            _compilation = newCompilation
        End Sub

        Private Sub New(compilation As VisualBasicCompilation, properties As MetadataReferenceProperties)
            MyBase.New(properties)
            _compilation = compilation
        End Sub

        Friend Overrides Function WithPropertiesImpl(properties As MetadataReferenceProperties) As CompilationReference
            Return New VisualBasicCompilationReference(_compilation, properties)
        End Function

        Private Function GetDebuggerDisplay() As String
            Return VBResources.CompilationVisualBasic + _compilation.AssemblyName
        End Function
    End Class
End Namespace
