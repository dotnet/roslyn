' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Module VisualBasicFileSystemExtensions
        ''' <summary>
        ''' Emit the IL for the compilation into the specified stream.
        ''' </summary>
        ''' <param name="compilation">Compilation.</param>
        ''' <param name="outputPath">Path of the file to which the compilation will be written.</param>
        ''' <param name="pdbPath">
        ''' Path of the file to which the compilation's debug info will be written.
        ''' Also embedded in the output file. <c>Nothing</c> to forego PDB generation.
        ''' </param>
        ''' <param name="xmlDocPath">Path of the file to which the compilation's XML documentation will be written. <c>Nothing</c> to forego XML generation.</param>
        ''' <param name="win32ResourcesPath">Path of the file from which the compilation's Win32 resources will be read (in RES format).  
        ''' Null to indicate that there are none.</param>
        ''' <param name="manifestResources">List of the compilation's managed resources. <c>Nothing</c> to indicate that there are none.</param>
        ''' <param name="cancellationToken">To cancel the emit process.</param>
        ''' <exception cref="ArgumentNullException">Compilation or path is null.</exception>
        ''' <exception cref="ArgumentException">Path is empty or invalid.</exception>
        ''' <exception cref="IOException">An error occurred while reading or writing a file.</exception>
        <Extension>
        Public Function Emit(compilation As VisualBasicCompilation,
                      outputPath As String,
                      Optional pdbPath As String = Nothing,
                      Optional xmlDocPath As String = Nothing,
                      Optional win32ResourcesPath As String = Nothing,
                      Optional manifestResources As IEnumerable(Of ResourceDescription) = Nothing,
                      Optional cancellationToken As CancellationToken = Nothing) As EmitResult

            Return FileSystemExtensions.Emit(compilation, outputPath, pdbPath, xmlDocPath, win32ResourcesPath, manifestResources, cancellationToken)
        End Function
    End Module
End Namespace
