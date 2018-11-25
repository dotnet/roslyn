// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal struct AttributeUsageInfo : IEquatable<AttributeUsageInfo>
    {
        [Flags()]
        private enum PackedAttributeUsage
        {
            None = 0,
            Assembly = AttributeTargets.Assembly,
            Module = AttributeTargets.Module,
            Class = AttributeTargets.Class,
            Struct = AttributeTargets.Struct,
            Enum = AttributeTargets.Enum,
            Constructor = AttributeTargets.Constructor,
            Method = AttributeTargets.Method,
            Property = AttributeTargets.Property,
            Field = AttributeTargets.Field,
            Event = AttributeTargets.Event,
            Interface = AttributeTargets.Interface,
            Parameter = AttributeTargets.Parameter,
            Delegate = AttributeTargets.Delegate,
            ReturnValue = AttributeTargets.ReturnValue,
            GenericParameter = AttributeTargets.GenericParameter,
            All = AttributeTargets.All,

            // NOTE: VB allows AttributeUsageAttribute with no valid target, i.e. <AttributeUsageAttribute(0)>, and doesn't generate any diagnostics.
            // We use PackedAttributeUsage.Initialized field to differentiate between uninitialized AttributeUsageInfo and initialized AttributeUsageInfo with no valid target.
            Initialized = GenericParameter << 1,

            AllowMultiple = Initialized << 1,
            Inherited = AllowMultiple << 1
        }

        private readonly PackedAttributeUsage _flags;

        /// <summary>
        /// Default attribute usage for attribute types:
        /// (a) Valid targets: AttributeTargets.All
        /// (b) AllowMultiple: false
        /// (c) Inherited: true
        /// </summary>
        static internal readonly AttributeUsageInfo Default = new AttributeUsageInfo(validTargets: AttributeTargets.All, allowMultiple: false, inherited: true);

        static internal readonly AttributeUsageInfo Null = default(AttributeUsageInfo);

        internal AttributeUsageInfo(AttributeTargets validTargets, bool allowMultiple, bool inherited)
        {
            // NOTE: VB allows AttributeUsageAttribute with no valid target, i.e. <AttributeUsageAttribute(0)>, and doesn't generate any diagnostics.
            // We use PackedAttributeUsage.Initialized field to differentiate between uninitialized AttributeUsageInfo and initialized AttributeUsageInfo with no valid targets.
            _flags = (PackedAttributeUsage)validTargets | PackedAttributeUsage.Initialized;

            if (allowMultiple)
            {
                _flags |= PackedAttributeUsage.AllowMultiple;
            }

            if (inherited)
            {
                _flags |= PackedAttributeUsage.Inherited;
            }
        }

        public bool IsNull
        {
            get
            {
                return (_flags & PackedAttributeUsage.Initialized) == 0;
            }
        }


        internal AttributeTargets ValidTargets
        {
            get
            {
                return (AttributeTargets)(_flags & PackedAttributeUsage.All);
            }
        }

        internal bool AllowMultiple
        {
            get
            {
                return (_flags & PackedAttributeUsage.AllowMultiple) != 0;
            }
        }

        internal bool Inherited
        {
            get
            {
                return (_flags & PackedAttributeUsage.Inherited) != 0;
            }
        }

        public static bool operator ==(AttributeUsageInfo left, AttributeUsageInfo right)
        {
            return left._flags == right._flags;
        }

        public static bool operator !=(AttributeUsageInfo left, AttributeUsageInfo right)
        {
            return left._flags != right._flags;
        }

        public override bool Equals(object obj)
        {
            if (obj is AttributeUsageInfo)
            {
                return this.Equals((AttributeUsageInfo)obj);
            }

            return false;
        }

        public bool Equals(AttributeUsageInfo other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return _flags.GetHashCode();
        }

        internal bool HasValidAttributeTargets
        {
            get
            {
                var value = (int)ValidTargets;
                return value != 0 && (value & (int)~AttributeTargets.All) == 0;
            }
        }

        internal object GetValidTargetsErrorArgument()
        {
            var validTargetsInt = (int)ValidTargets;
            if (!HasValidAttributeTargets)
            {
                return string.Empty;
            }

            var builder = ArrayBuilder<string>.GetInstance();
            int flag = 0;
            while (validTargetsInt > 0)
            {
                if ((validTargetsInt & 1) != 0)
                {
                    builder.Add(GetErrorDisplayNameResourceId((AttributeTargets)(1 << flag)));
                }

                validTargetsInt >>= 1;
                flag++;
            }

            return new ValidTargetsStringLocalizableErrorArgument(builder.ToArrayAndFree());
        }

        private struct ValidTargetsStringLocalizableErrorArgument : IFormattable
        {
            private readonly string[] _targetResourceIds;

            internal ValidTargetsStringLocalizableErrorArgument(string[] targetResourceIds)
            {
                Debug.Assert(targetResourceIds != null);
                _targetResourceIds = targetResourceIds;
            }

            public override string ToString()
            {
                return ToString(null, null);
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                var builder = PooledStringBuilder.GetInstance();
                var culture = formatProvider as System.Globalization.CultureInfo;

                if (_targetResourceIds != null)
                {
                    foreach (string id in _targetResourceIds)
                    {
                        if (builder.Builder.Length > 0)
                        {
                            builder.Builder.Append(", ");
                        }

                        builder.Builder.Append(CodeAnalysisResources.ResourceManager.GetString(id, culture));
                    }
                }

                var message = builder.Builder.ToString();
                builder.Free();

                return message;
            }
        }

        private static string GetErrorDisplayNameResourceId(AttributeTargets target)
        {
            switch (target)
            {
                case AttributeTargets.Assembly: return nameof(CodeAnalysisResources.Assembly);
                case AttributeTargets.Class: return nameof(CodeAnalysisResources.Class1);
                case AttributeTargets.Constructor: return nameof(CodeAnalysisResources.Constructor);
                case AttributeTargets.Delegate: return nameof(CodeAnalysisResources.Delegate1);
                case AttributeTargets.Enum: return nameof(CodeAnalysisResources.Enum1);
                case AttributeTargets.Event: return nameof(CodeAnalysisResources.Event1);
                case AttributeTargets.Field: return nameof(CodeAnalysisResources.Field);
                case AttributeTargets.GenericParameter: return nameof(CodeAnalysisResources.TypeParameter);
                case AttributeTargets.Interface: return nameof(CodeAnalysisResources.Interface1);
                case AttributeTargets.Method: return nameof(CodeAnalysisResources.Method);
                case AttributeTargets.Module: return nameof(CodeAnalysisResources.Module);
                case AttributeTargets.Parameter: return nameof(CodeAnalysisResources.Parameter);
                case AttributeTargets.Property: return nameof(CodeAnalysisResources.Property);
                case AttributeTargets.ReturnValue: return nameof(CodeAnalysisResources.Return1);
                case AttributeTargets.Struct: return nameof(CodeAnalysisResources.Struct1);
                default:
                    throw ExceptionUtilities.UnexpectedValue(target);
            }
        }
    }
}
