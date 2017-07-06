' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
