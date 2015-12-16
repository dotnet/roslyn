// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public class CommonObjectFilter : ObjectFilter
    {
        public override IEnumerable<StackFrame> Filter(IEnumerable<StackFrame> frames)
        {
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (IsHiddenMember(method))
                {
                    continue;
                }

                var type = method.DeclaringType;

                // TODO (https://github.com/dotnet/roslyn/issues/5250): look for other types indicating that we're in Roslyn code
                if (type == typeof(CommandLineRunner))
                {
                    yield break;
                }

                // TODO (tomat): we don't want to include awaiter helpers, shouldn't they be marked by DebuggerHidden in FX?
                if (IsTaskAwaiter(type) || IsTaskAwaiter(type.DeclaringType))
                {
                    continue;
                }

                yield return frame;
            }
        }

        public override IEnumerable<MemberInfo> Filter(IEnumerable<MemberInfo> members)
        {
            return members.Where(m => !IsGeneratedMemberName(m.Name));
        }

        private bool IsHiddenMember(MemberInfo info)
        {
            while (info != null)
            {
                if (IsGeneratedMemberName(info.Name) || 
                    info.GetCustomAttributes<DebuggerHiddenAttribute>().Any())
                {
                    return true;
                }

                info = info.DeclaringType?.GetTypeInfo();
            }

            return false;
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

        protected virtual bool IsGeneratedMemberName(string name) => false;
    }
}