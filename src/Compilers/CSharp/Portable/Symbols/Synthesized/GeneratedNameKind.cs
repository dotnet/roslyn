// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        LocalFunction = 'g',

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
