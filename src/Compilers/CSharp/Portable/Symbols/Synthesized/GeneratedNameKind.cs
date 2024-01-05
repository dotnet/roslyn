// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        FileType = 'F',
        AnonymousType = 'f',
        TransparentIdentifier = 'h',
        AnonymousTypeField = 'i',
        StateMachineStateIdField = 'I',
        AnonymousTypeTypeParameter = 'j',
        AutoPropertyBackingField = 'k',
        IteratorCurrentThreadIdField = 'l',
        IteratorFinallyMethod = 'm',
        BaseMethodWrapper = 'n',
        AsyncBuilderField = 't',
        DelegateCacheContainerType = 'O',
        DynamicCallSiteContainerType = 'o',
        PrimaryConstructorParameter = 'P',
        DynamicCallSiteField = 'p',
        AsyncIteratorPromiseOfValueOrEndBackingField = 'v',
        DisposeModeField = 'w',
        CombinedTokensField = 'x',
        InlineArrayType = 'y',
        ReadOnlyListType = 'z', // last

        // Deprecated - emitted by Dev12, but not by Roslyn.
        // Don't reuse the values because the debugger might encounter them when consuming old binaries.
        [Obsolete]
        Deprecated_OuterscopeLocals = '6',
        [Obsolete]
        Deprecated_IteratorInstance = 'a',
        [Obsolete]
        Deprecated_InitializerLocal = 'g',
        [Obsolete]
        Deprecated_DynamicDelegate = 'q',
        [Obsolete]
        Deprecated_ComrefCallLocal = 'r',
    }

    internal static class GeneratedNameKindExtensions
    {
        internal static bool IsTypeName(this GeneratedNameKind kind)
            => kind is GeneratedNameKind.LambdaDisplayClass
                    or GeneratedNameKind.StateMachineType
                    or GeneratedNameKind.DynamicCallSiteContainerType
                    or GeneratedNameKind.DelegateCacheContainerType
                    ;
    }
}
