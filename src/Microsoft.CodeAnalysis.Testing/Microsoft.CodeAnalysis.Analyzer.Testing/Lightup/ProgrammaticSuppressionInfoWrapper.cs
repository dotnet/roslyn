// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Testing.Lightup
{
    internal readonly struct ProgrammaticSuppressionInfoWrapper
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.Diagnostics.ProgrammaticSuppressionInfo";
        internal static readonly Type? WrappedType = typeof(Diagnostic).GetTypeInfo().Assembly.GetType(WrappedTypeName);
        private static readonly Func<object, ImmutableHashSet<(string id, LocalizableString justification)>> s_suppressions;

        private readonly object _instance;

        static ProgrammaticSuppressionInfoWrapper()
        {
            s_suppressions = LightupHelpers.CreatePropertyAccessor<object, ImmutableHashSet<(string id, LocalizableString justification)>>(WrappedType, nameof(Suppressions), ImmutableHashSet<(string id, LocalizableString justification)>.Empty);
        }

        private ProgrammaticSuppressionInfoWrapper(object instance)
        {
            _instance = instance;
        }

        public ImmutableHashSet<(string id, LocalizableString justification)> Suppressions => s_suppressions(_instance);

        public static ProgrammaticSuppressionInfoWrapper FromInstance(object instance)
        {
            if (instance == null)
            {
                return default;
            }

            if (!IsInstance(instance))
            {
                throw new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'");
            }

            return new ProgrammaticSuppressionInfoWrapper(instance);
        }

        public static bool IsInstance(object value)
        {
            if (value is null)
            {
                return false;
            }

            if (WrappedType is null)
            {
                return false;
            }

            return WrappedType.IsAssignableFrom(value.GetType());
        }
    }
}
