' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Public Module Options
    Public ReadOnly OptionsScript As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public ReadOnly OptionsInteractive As New VisualBasicParseOptions(kind:=SourceCodeKind.Interactive)
    Public ReadOnly OptionsRegular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)

    Public ReadOnly OptionsDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)
    Public ReadOnly OptionsExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)

    Public ReadOnly OptionsDebuggableReleaseDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize:=True, debugInformationKind:=DebugInformationKind.Full)
    Public ReadOnly OptionsDebuggableReleaseExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimize:=True, debugInformationKind:=DebugInformationKind.Full)

    Public ReadOnly OptionsDebugDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize:=False, debugInformationKind:=DebugInformationKind.Full)
    Public ReadOnly OptionsDebugExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimize:=False, debugInformationKind:=DebugInformationKind.Full)

    Public ReadOnly OptionsNetModule As New VisualBasicCompilationOptions(OutputKind.NetModule, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)
    Public ReadOnly OptionsWinMDObj As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimize:=True, debugInformationKind:=DebugInformationKind.PdbOnly)

    Public ReadOnly OptionsDllAlwaysImportInternals As VisualBasicCompilationOptions = OptionsDll.WithMetadataImportOptions(MetadataImportOptions.Internal)
    Public ReadOnly OptionsExeAlwaysImportInternals As VisualBasicCompilationOptions = OptionsExe.WithMetadataImportOptions(MetadataImportOptions.Internal)

    ' TODO: remove
    Public ReadOnly UnoptimizedDll As New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize:=False, concurrentBuild:=False)
    Public ReadOnly UnoptimizedExe As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimize:=False, concurrentBuild:=False)
End Module
