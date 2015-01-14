' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Enum ClosureKind
        ''' <summary>
        ''' The closure doesn't declare any variables. 
        ''' Display class Is a singleton And may be shared with other top-level methods.
        ''' </summary>
        [Static]

        ''' <summary>
        ''' The closure only contains a reference to the containing class instance ("Me").
        ''' We don't emit a display class, lambdas are emitted directly to the containing class as its instance methods.
        ''' </summary>
        ThisOnly

        '''  <summary>
        '''  General closure.
        '''  Display class may only contain lambdas defined in the same top-level method.
        '''  </summary>
        General
    End Enum
End Namespace
