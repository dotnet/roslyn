// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal struct AttributeUsageInfo 
    {
        [Flags()]
        enum PackedAttributeUsage 
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
            // We use use PackedAttributeUsage.Initialized field to differentiate between uninitialized AttributeUsageInfo and initialized AttributeUsageInfo with no valid target.
            Initialized = GenericParameter << 1,
            
            AllowMultiple = Initialized << 1,
            Inherited = AllowMultiple << 1
        }

        private PackedAttributeUsage flags;
        
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
            // We use use PackedAttributeUsage.Initialized field to differentiate between uninitialized AttributeUsageInfo and initialized AttributeUsageInfo with no valid targets.
            flags = (PackedAttributeUsage)validTargets | PackedAttributeUsage.Initialized;
            
            if (allowMultiple)
            {
                flags |= PackedAttributeUsage.AllowMultiple;
            }

            if (inherited)
            {
                flags |= PackedAttributeUsage.Inherited;
            }
        }

        public bool IsNull
        {
            get
            {
                return (flags & PackedAttributeUsage.Initialized) == 0;
            }
        }

        
        internal AttributeTargets ValidTargets
        {
            get
            {
                return (AttributeTargets)(flags & PackedAttributeUsage.All);
            }
        }

        internal bool AllowMultiple 
        {
            get
            {
                return (flags & PackedAttributeUsage.AllowMultiple) != 0;
            }
        }

        internal bool Inherited 
        {
            get
            {
                return (flags & PackedAttributeUsage.Inherited) != 0;
            }
        }

        public static bool operator ==(AttributeUsageInfo left, AttributeUsageInfo right)
        {
            return left.flags == right.flags;
        }

        public static bool operator !=(AttributeUsageInfo left, AttributeUsageInfo right)
        {
            return left.flags != right.flags;
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
            return flags.GetHashCode();
        }

        internal bool HasValidAttributeTargets
        {
            get
            {
                var value = (int)ValidTargets;
                return value != 0 && (value & (int)~AttributeTargets.All) == 0;
            }
        }

        internal string GetValidTargetsString()
        {
            var validTargetsInt = (int)ValidTargets;
            if (!HasValidAttributeTargets)
            {
                return "InvalidAttributeTargets";
            }

            var builder = PooledStringBuilder.GetInstance();
            int flag = 0;
            while (validTargetsInt > 0)
            {
                if ((validTargetsInt & 1) != 0)
                {
                    if (builder.Builder.Length > 0)
                    {
                        builder.Builder.Append(", ");
                    }

                    builder.Builder.Append(GetErrorDisplayName((AttributeTargets)(1 << flag)));
                }

                validTargetsInt >>= 1;
                flag++;
            }

            return builder.ToStringAndFree();
        }

        private static string GetErrorDisplayName(AttributeTargets target)
        {
            switch (target)
            {
                case AttributeTargets.Assembly:          return CodeAnalysisResources.Assembly;
                case AttributeTargets.Class:             return CodeAnalysisResources.Class1;
                case AttributeTargets.Constructor:       return CodeAnalysisResources.Constructor;
                case AttributeTargets.Delegate:          return CodeAnalysisResources.Delegate1;
                case AttributeTargets.Enum:              return CodeAnalysisResources.Enum1;
                case AttributeTargets.Event:             return CodeAnalysisResources.Event1;
                case AttributeTargets.Field:             return CodeAnalysisResources.Field;
                case AttributeTargets.GenericParameter:  return CodeAnalysisResources.TypeParameter;
                case AttributeTargets.Interface:         return CodeAnalysisResources.Interface1;
                case AttributeTargets.Method:            return CodeAnalysisResources.Method;
                case AttributeTargets.Module:            return CodeAnalysisResources.Module;
                case AttributeTargets.Parameter:         return CodeAnalysisResources.Parameter;
                case AttributeTargets.Property:          return CodeAnalysisResources.Property;
                case AttributeTargets.ReturnValue:       return CodeAnalysisResources.Return1;
                case AttributeTargets.Struct:            return CodeAnalysisResources.Struct1;
                default:
                    throw ExceptionUtilities.UnexpectedValue(target);
            }
        }
    }
}
