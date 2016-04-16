// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SpeculativeSemanticModelTestsBase
    {
#if false // https://github.com/dotnet/roslyn/issues/4453
        protected void CheckAllMembers<T>(T instance, IDictionary<Type, Func<object>> valueProviders, IDictionary<MemberInfo, Type> expectedExceptions)
        {
            foreach (var m in typeof(T).GetMembers())
            {
                try
                {
                    switch (m.MemberType)
                    {
                        case MemberTypes.Field:
                            ((FieldInfo)m).GetValue(instance);
                            break;
                        case MemberTypes.Property:
                            ((PropertyInfo)m).GetValue(instance);
                            break;
                        case MemberTypes.Method:
                            var method = (MethodInfo)m;
                            if (method.IsStatic)
                                continue;
                            method.Invoke(instance, method.GetParameters().Select(p =>
                                (valueProviders.GetValueOrDefault(p.ParameterType) ?? (() => null))()).ToArray());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Type exceptionType;
                    if (!expectedExceptions.TryGetValue(m, out exceptionType))
                        throw;

                    ex = ex is TargetInvocationException ? ex.InnerException : ex;
                    if (ex.GetType() != exceptionType)
                        throw;
                }
            }
        }
#endif
    }
}
