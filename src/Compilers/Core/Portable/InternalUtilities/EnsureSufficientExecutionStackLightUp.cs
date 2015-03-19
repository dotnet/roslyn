// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Roslyn.Utilities
{
    internal static class EnsureSufficientExecutionStackLightUp
    {
        private readonly static Action s_ensureSufficientExecutionStack;

        static EnsureSufficientExecutionStackLightUp()
        {
            // TODO (DevDiv workitem 966425): Replace with the RuntimeHelpers.EnsureSufficientExecutionStack API when available.
            if (!TryGetEnsureSufficientExecutionStack(out s_ensureSufficientExecutionStack))
            {
                s_ensureSufficientExecutionStack = () => { };
            }
        }

        private static bool TryGetEnsureSufficientExecutionStack(out Action ensureSufficientExecutionStack)
        {
            var type = typeof(object).GetTypeInfo().Assembly.GetType("System.Runtime.CompilerServices.RuntimeHelpers");
            if (type == null)
            {
                ensureSufficientExecutionStack = null;
                return false;
            }

            foreach (var methodInfo in type.GetTypeInfo().GetDeclaredMethods("EnsureSufficientExecutionStack"))
            {
                if (methodInfo.IsStatic && !methodInfo.ContainsGenericParameters && methodInfo.ReturnType == typeof(void) && methodInfo.GetParameters().Length == 0)
                {
                    ensureSufficientExecutionStack = (Action)methodInfo.CreateDelegate(typeof(Action));
                    return true;
                }
            }

            ensureSufficientExecutionStack = null;
            return false;
        }

        public static void EnsureSufficientExecutionStack()
        {
            s_ensureSufficientExecutionStack();
        }
    }
}
