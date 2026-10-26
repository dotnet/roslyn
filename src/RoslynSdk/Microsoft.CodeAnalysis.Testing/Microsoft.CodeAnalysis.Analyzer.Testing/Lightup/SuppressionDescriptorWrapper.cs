// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Testing.Lightup
{
    internal readonly struct SuppressionDescriptorWrapper
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.SuppressionDescriptor";
        internal static readonly Type? WrappedType = typeof(Diagnostic).GetTypeInfo().Assembly.GetType(WrappedTypeName);
        private static readonly Func<object, string> s_id;
        private static readonly Func<object, string> s_suppressedDiagnosticId;
        private static readonly Func<object, LocalizableString> s_justification;

        private readonly object _instance;

        static SuppressionDescriptorWrapper()
        {
            s_id = LightupHelpers.CreatePropertyAccessor<object, string>(WrappedType, nameof(Id), string.Empty);
            s_suppressedDiagnosticId = LightupHelpers.CreatePropertyAccessor<object, string>(WrappedType, nameof(SuppressedDiagnosticId), string.Empty);
            s_justification = LightupHelpers.CreatePropertyAccessor<object, LocalizableString>(WrappedType, nameof(Justification), string.Empty);
        }

        private SuppressionDescriptorWrapper(object instance)
        {
            _instance = instance;
        }

        public string Id => s_id(_instance);

        public string SuppressedDiagnosticId => s_suppressedDiagnosticId(_instance);

        public LocalizableString Justification => s_justification(_instance);

        public static SuppressionDescriptorWrapper FromInstance(object instance)
        {
            if (instance == null)
            {
                return default;
            }

            if (!IsInstance(instance))
            {
                throw new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'");
            }

            return new SuppressionDescriptorWrapper(instance);
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
