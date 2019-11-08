// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationEventInfo
    {
        private static readonly ConditionalWeakTable<IEventSymbol, CodeGenerationEventInfo> s_eventToInfoMap =
            new ConditionalWeakTable<IEventSymbol, CodeGenerationEventInfo>();

        private readonly bool _isUnsafe;
        private CodeGenerationEventInfo(bool isUnsafe)
        {
            _isUnsafe = isUnsafe;
        }

        public static void Attach(IEventSymbol @event, bool isUnsafe)
        {
            var info = new CodeGenerationEventInfo(isUnsafe);
            s_eventToInfoMap.Add(@event, info);
        }

        private static CodeGenerationEventInfo GetInfo(IEventSymbol @event)
        {
            s_eventToInfoMap.TryGetValue(@event, out var info);
            return info;
        }

        public static bool GetIsUnsafe(IEventSymbol @event)
        {
            return GetIsUnsafe(GetInfo(@event));
        }

        private static bool GetIsUnsafe(CodeGenerationEventInfo info)
        {
            return info is { _isUnsafe: true };
        }
    }
}
