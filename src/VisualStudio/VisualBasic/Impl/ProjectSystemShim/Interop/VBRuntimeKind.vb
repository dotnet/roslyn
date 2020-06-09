' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    ''' <summary>
    ''' What version of the VB runtime to use.
    ''' </summary>
    Friend Enum VBRuntimeKind
        ''' <summary>
        ''' corresponds to /vbruntime+
        ''' </summary>
        DefaultRuntime

        ''' <summary>
        ''' corresponds to /vbruntime-
        ''' </summary>
        NoRuntime

        ''' <summary>
        ''' corresponds to /vbruntime:[path]
        ''' </summary>
        SpecifiedRuntime

        ''' <summary>
        ''' corresponds to /vbruntime*
        ''' </summary>
        EmbeddedRuntime
    End Enum
End Namespace
