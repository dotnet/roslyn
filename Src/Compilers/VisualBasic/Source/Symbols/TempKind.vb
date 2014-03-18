' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Enum TempKind As Byte
        None        ' not a temp or a regular nameless temp
        Lock
        [Using]
        ForEachEnumerator
        ForEachArray
        ForEachArrayIndex
        LockTaken
        [With]

        ForLimit
        ForStep
        ForLoopObject
        ForDirection

        'TODO:
        ' degenerate select key (can we EnC when stopped on case?)

        StateMachineReturnValue
        StateMachineException
        StateMachineCachedState

        ' XmlInExpressionLambda locals are always lifted and must have distinct names.
        XmlInExpressionLambda

        ' TODO: I am not sure we need these
        OnErrorActiveHandler
        OnErrorResumeTarget
        OnErrorCurrentStatement
        OnErrorCurrentLine


    End Enum
End Namespace
