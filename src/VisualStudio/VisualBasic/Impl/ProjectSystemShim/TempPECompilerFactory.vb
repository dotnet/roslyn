' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Friend Class TempPECompilerFactory
        Implements IVbTempPECompilerFactory

        Private ReadOnly _workspace As VisualStudioWorkspaceImpl

        Public Sub New(workspace As VisualStudioWorkspaceImpl)
            Me._workspace = workspace
        End Sub

        Public Function CreateCompiler() As IVbCompiler Implements IVbTempPECompilerFactory.CreateCompiler
            Return New TempPECompiler(_workspace)
        End Function
    End Class
End Namespace
