' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <Guid("45171DA0-7824-11d2-99C3-00C04F86DC69"), ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IVbBuildStatusCallback
        ''' <summary>
        ''' Notification that the build is beginning. If the build needs to be stopped, then the
        ''' callee should set pfContinue to false.
        ''' </summary>
        Sub BuildBegin(ByRef pfContinue As Boolean)

        ''' <summary>
        ''' If a build or clean has completed, fSuccess specifies if it completed successfully.
        ''' </summary>
        Sub BuildEnd(fSuccess As Boolean)

        ''' <summary>
        ''' Called every once in a while between BuildBegin and BuildEnd.
        ''' </summary>
        Sub TickEx(ByRef pfContinue As Boolean,
                   cItemsLeft As UInteger,
                   cItemsTotal As UInteger)

        ''' <summary>
        ''' Called every time the project reaches bound state.
        ''' </summary>
        Sub ProjectBound()
    End Interface
End Namespace
