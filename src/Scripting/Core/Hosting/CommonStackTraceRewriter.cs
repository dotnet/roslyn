// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public class CommonStackTraceRewriter : StackTraceRewriter
    {
        public override IEnumerable<StackFrame> Rewrite(IEnumerable<StackFrame> frames)
        {
            foreach (var frame in frames)
            {
                var method = frame.Method;
                var type = method.DeclaringType;

                // TODO (https://github.com/dotnet/roslyn/issues/5250): look for other types indicating that we're in Roslyn code
                if (type == typeof(CommandLineRunner))
                {
                    yield break;
                }

                // TODO: we don't want to include awaiter helpers, shouldn't they be marked by DebuggerHidden in FX?
                if (IsTaskAwaiter(type) || IsTaskAwaiter(type.DeclaringType))
                {
                    continue;
                }

                yield return frame;
            }
        }

        private static bool IsTaskAwaiter(Type type)
        {
            if (type == typeof(TaskAwaiter) || type == typeof(ConfiguredTaskAwaitable))
            {
                return true;
            }

            if (type?.GetTypeInfo().IsGenericType == true)
            {
                var genericDef = type.GetTypeInfo().GetGenericTypeDefinition();
                return genericDef == typeof(TaskAwaiter<>) || type == typeof(ConfiguredTaskAwaitable<>);
            }

            return false;
        }
    }
}