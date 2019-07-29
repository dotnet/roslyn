// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum GeneratedNameKind
    {
        None = 0,

        // Used by EE:
        ThisProxyField = '4',
        HoistedLocalField = '5',
        DisplayClassLocalOrField = '8',
        LambdaMethod = 'b',
        LambdaDisplayClass = 'c',
        StateMachineType = 'd',
        LocalFunction = 'g', // note collision with Deprecated_InitializerLocal, however this one is only used for method names

        // Used by EnC:
        AwaiterField = 'u',
        HoistedSynthesizedLocalField = 's',

        // Currently not parsed:
        StateMachineStateField = '1',
        IteratorCurrentBackingField = '2',
        StateMachineParameterProxyField = '3',
        ReusableHoistedLocalField = '7',
        LambdaCacheField = '9',
        FixedBufferField = 'e',
        AnonymousType = 'f',
        TransparentIdentifier = 'h',
        AnonymousTypeField = 'i',
        AutoPropertyBackingField = 'k',
        IteratorCurrentThreadIdField = 'l',
        IteratorFinallyMethod = 'm',
        BaseMethodWrapper = 'n',
        AsyncBuilderField = 't',
        DynamicCallSiteContainerType = 'o',
        DynamicCallSiteField = 'p',
        AsyncIteratorPromiseOfValueOrEndBackingField = 'v',
        DisposeModeField = 'w',
        CombinedTokensField = 'x', // last

        // Deprecated - emitted by Dev12, but not by Roslyn.
        // Don't reuse the values because the debugger might encounter them when consuming old binaries.
        [Obsolete]
        Deprecated_OuterscopeLocals = '6',
        [Obsolete]
        Deprecated_IteratorInstance = 'a',
        [Obsolete]
        Deprecated_InitializerLocal = 'g',
        [Obsolete]
        Deprecated_AnonymousTypeTypeParameter = 'j',
        [Obsolete]
        Deprecated_DynamicDelegate = 'q',
        [Obsolete]
        Deprecated_ComrefCallLocal = 'r',
    }

    internal static class GeneratedNameKindExtensions
    {
        internal static bool IsTypeName(this GeneratedNameKind kind)
        {
            switch (kind)
            {
                case GeneratedNameKind.LambdaDisplayClass:
                case GeneratedNameKind.StateMachineType:
                case GeneratedNameKind.DynamicCallSiteContainerType:
                    return true;

                default:
                    return false;
            }
        }
    }
}
