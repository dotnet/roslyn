// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal class CommonMemberFilter : MemberFilter
    {
        public override bool Include(StackFrame frame)
        {
            var method = frame.GetMethod();

            // TODO (https://github.com/dotnet/roslyn/issues/23101): investigate which submission frames *can* be excluded
            if (method.DeclaringType?.FullName.StartsWith("Submission#0").ToThreeState() == ThreeState.True)
            {
                return true;
            }

            if (IsHiddenMember(method))
            {
                return false;
            }

            var type = method.DeclaringType;

            // TODO (https://github.com/dotnet/roslyn/issues/5250): look for other types indicating that we're in Roslyn code
            if (type == typeof(CommandLineRunner))
            {
                return false;
            }

            // Type is null for DynamicMethods and global methods.
            // TODO (tomat): we don't want to include awaiter helpers, shouldn't they be marked by DebuggerHidden in FX?
            if (type == null || IsTaskAwaiter(type) || IsTaskAwaiter(type.DeclaringType))
            {
                return false;
            }

            if (type == typeof(ExceptionDispatchInfo) && method.Name == nameof(ExceptionDispatchInfo.Throw))
            {
                return false;
            }

            return true;
        }

        public override bool Include(MemberInfo member)
        {
            return !IsGeneratedMemberName(member.Name);
        }

        private bool IsHiddenMember(MemberInfo info)
        {
            while (info != null)
            {
                // GetCustomAttributes returns null when called on DynamicMethod 
                // (bug https://github.com/dotnet/corefx/issues/6402)
                if (IsGeneratedMemberName(info.Name) ||
                    info.GetCustomAttributes<DebuggerHiddenAttribute>()?.Any() == true)
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
