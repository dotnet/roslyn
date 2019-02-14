// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    public class MetadataSignatureHelper
    {
        #region Helpers
        private const BindingFlags BINDING_FLAGS =
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        static private void AppendComma(StringBuilder sb)
        {
            sb.Append(", ");
        }

        static private void RemoveTrailingComma(StringBuilder sb)
        {
            if (sb.ToString().EndsWith(", ", StringComparison.Ordinal))
            {
                sb.Length -= 2;
            }
        }

        static private void AppendType(Type type, StringBuilder sb, bool showGenericConstraints = false)
        {
            if (showGenericConstraints && type.IsGenericParameter)
            {
                var typeInfo = type.GetTypeInfo();
                if (typeInfo.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)) sb.Append("class ");
                if (typeInfo.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)) sb.Append("valuetype ");
                if (typeInfo.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)) sb.Append(".ctor ");

                var genericConstraints = typeInfo.GetGenericParameterConstraints();
                if (genericConstraints.Length > 0)
                {
                    sb.Append("(");
                    foreach (var genericConstraint in genericConstraints)
                    {
                        AppendType(genericConstraint, sb);
                        AppendComma(sb);
                    }
                    RemoveTrailingComma(sb);
                    sb.Append(") ");
                }
            }
            sb.Append(type);
        }

        static private void AppendValue(object value, StringBuilder sb, bool includeAssignmentOperator = true)
        {
            if (value != null)
            {
                if (includeAssignmentOperator)
                {
                    sb.Append(" = ");
                }

                if (value.GetType() == typeof(string))
                {
                    sb.AppendFormat("\"{0}\"", value.ToString());
                }
                else
                {
                    sb.Append(Roslyn.Test.Utilities.TestHelpers.GetCultureInvariantString(value));
                }
            }
        }

        static private void AppendCustomAttributeData(CustomAttributeData attribute, StringBuilder sb)
        {
            sb.Append("[");
            AppendType(attribute.Constructor.DeclaringType, sb);
            sb.Append("(");
            foreach (var positionalArgument in attribute.ConstructorArguments)
            {
                AppendValue(positionalArgument.Value, sb, false);
                AppendComma(sb);
            }
            foreach (var namedArgument in attribute.NamedArguments)
            {
                sb.Append(namedArgument.MemberName);
                AppendValue(namedArgument.TypedValue.Value, sb);
                AppendComma(sb);
            }
            RemoveTrailingComma(sb);
            sb.Append(")]");
        }

        static private void AppendParameterInfo(ParameterInfo parameter, StringBuilder sb)
        {
            foreach (var attribute in parameter.CustomAttributes)
            {
                // these are pseudo-custom attributes that are added by Reflection but don't appear in metadata as custom attributes:
                if (attribute.AttributeType != typeof(OptionalAttribute) &&
                    attribute.AttributeType != typeof(InAttribute) &&
                    attribute.AttributeType != typeof(OutAttribute) &&
                    attribute.AttributeType != typeof(MarshalAsAttribute))
                {
                    AppendCustomAttributeData(attribute, sb);
                    sb.Append(" ");
                }
            }

            foreach (var modreq in parameter.GetRequiredCustomModifiers())
            {
                sb.Append("modreq(");
                AppendType(modreq, sb);
                sb.Append(") ");
            }
            foreach (var modopt in parameter.GetOptionalCustomModifiers())
            {
                sb.Append("modopt(");
                AppendType(modopt, sb);
                sb.Append(") ");
            }

            int length = sb.Length;
            AppendParameterAttributes(sb, parameter.Attributes, all: false);
            if (sb.Length > length)
            {
                sb.Append(" ");
            }

            AppendType(parameter.ParameterType, sb);
            if (!string.IsNullOrWhiteSpace(parameter.Name)) // If this is not the 'return' parameter
            {
                sb.Append(" ");
                sb.Append(parameter.Name);

                var defaultValue = parameter.RawDefaultValue;
                if (defaultValue != DBNull.Value)
                {
                    AppendValue(defaultValue, sb);
                }
            }
        }

        public static bool AppendParameterAttributes(StringBuilder sb, ParameterAttributes attributes, bool all = true)
        {
            List<string> list = new List<string>();

            if ((attributes & ParameterAttributes.Optional) != 0) list.Add("[opt]");
            if ((attributes & ParameterAttributes.In) != 0) list.Add("[in]");
            if ((attributes & ParameterAttributes.Out) != 0) list.Add("[out]");

            if (all)
            {
                if ((attributes & ParameterAttributes.HasFieldMarshal) != 0) list.Add("marshal");
                if ((attributes & ParameterAttributes.HasDefault) != 0) list.Add("default");
            }

            sb.Append(list.Join(" "));
            return list.Count > 0;
        }

        public static bool AppendPropertyAttributes(StringBuilder sb, PropertyAttributes attributes, bool all = true)
        {
            List<string> list = new List<string>();

            if ((attributes & PropertyAttributes.SpecialName) != 0) list.Add("specialname");
            if ((attributes & PropertyAttributes.RTSpecialName) != 0) list.Add("rtspecialname");

            if (all)
            {
                if ((attributes & PropertyAttributes.HasDefault) != 0) list.Add("default");
            }

            sb.Append(list.Join(" "));
            return list.Count > 0;
        }

        public static bool AppendEventAttributes(StringBuilder sb, EventAttributes attributes, bool all = true)
        {
            List<string> list = new List<string>();

            if ((attributes & EventAttributes.SpecialName) != 0) list.Add("specialname");
            if ((attributes & EventAttributes.RTSpecialName) != 0) list.Add("rtspecialname");

            sb.Append(list.Join(" "));
            return list.Count > 0;
        }

        public static StringBuilder AppendFieldAttributes(StringBuilder sb, FieldAttributes attributes, bool all = true)
        {
            string visibility;
            switch (attributes & FieldAttributes.FieldAccessMask)
            {
                case FieldAttributes.PrivateScope: visibility = "privatescope"; break;
                case FieldAttributes.Private: visibility = "private"; break;
                case FieldAttributes.FamANDAssem: visibility = "famandassem"; break;
                case FieldAttributes.Assembly: visibility = "assembly"; break;
                case FieldAttributes.Family: visibility = "family"; break;
                case FieldAttributes.FamORAssem: visibility = "famorassem"; break;
                case FieldAttributes.Public: visibility = "public"; break;

                default:
                    throw new InvalidOperationException();
            }

            sb.Append(visibility);
            sb.Append((attributes & FieldAttributes.Static) != 0 ? " static" : " instance");

            if ((attributes & FieldAttributes.InitOnly) != 0) sb.Append(" initonly");
            if ((attributes & FieldAttributes.Literal) != 0) sb.Append(" literal");
            if ((attributes & FieldAttributes.NotSerialized) != 0) sb.Append(" notserialized");
            if ((attributes & FieldAttributes.SpecialName) != 0) sb.Append(" specialname");
            if ((attributes & FieldAttributes.RTSpecialName) != 0) sb.Append(" rtspecialname");

            if (all)
            {
                if ((attributes & FieldAttributes.PinvokeImpl) != 0) sb.Append(" pinvokeimpl");
                if ((attributes & FieldAttributes.HasFieldMarshal) != 0) sb.Append(" marshal");
                if ((attributes & FieldAttributes.HasDefault) != 0) sb.Append(" default");
                if ((attributes & FieldAttributes.HasFieldRVA) != 0) sb.Append(" rva");
            }

            return sb;
        }

        public static StringBuilder AppendMethodAttributes(StringBuilder sb, MethodAttributes attributes, bool all = true)
        {
            string visibility;
            switch (attributes & MethodAttributes.MemberAccessMask)
            {
                case MethodAttributes.PrivateScope: visibility = "privatescope"; break;
                case MethodAttributes.Private: visibility = "private"; break;
                case MethodAttributes.FamANDAssem: visibility = "famandassem"; break;
                case MethodAttributes.Assembly: visibility = "assembly"; break;
                case MethodAttributes.Family: visibility = "family"; break;
                case MethodAttributes.FamORAssem: visibility = "famorassem"; break;
                case MethodAttributes.Public: visibility = "public"; break;

                default:
                    throw new InvalidOperationException();
            }

            sb.Append(visibility);

            if ((attributes & MethodAttributes.HideBySig) != 0) sb.Append(" hidebysig");
            if ((attributes & MethodAttributes.NewSlot) != 0) sb.Append(" newslot");
            if ((attributes & MethodAttributes.CheckAccessOnOverride) != 0) sb.Append(" strict");
            if ((attributes & MethodAttributes.SpecialName) != 0) sb.Append(" specialname");
            if ((attributes & MethodAttributes.RTSpecialName) != 0) sb.Append(" rtspecialname");
            if ((attributes & MethodAttributes.RequireSecObject) != 0) sb.Append(" reqsecobj");
            if ((attributes & MethodAttributes.UnmanagedExport) != 0) sb.Append(" unmanagedexp");
            if ((attributes & MethodAttributes.Abstract) != 0) sb.Append(" abstract");
            if ((attributes & MethodAttributes.Virtual) != 0) sb.Append(" virtual");
            if ((attributes & MethodAttributes.Final) != 0) sb.Append(" final");

            sb.Append((attributes & MethodAttributes.Static) != 0 ? " static" : " instance");

            if (all)
            {
                if ((attributes & MethodAttributes.PinvokeImpl) != 0) sb.Append(" pinvokeimpl");
            }

            return sb;
        }

        public static StringBuilder AppendMethodImplAttributes(StringBuilder sb, MethodImplAttributes attributes)
        {
            string codeType;
            switch (attributes & MethodImplAttributes.CodeTypeMask)
            {
                case MethodImplAttributes.IL: codeType = "cil"; break;
                case MethodImplAttributes.OPTIL: codeType = "optil"; break;
                case MethodImplAttributes.Runtime: codeType = "runtime"; break;
                case MethodImplAttributes.Native: codeType = "native"; break;

                default:
                    throw new InvalidOperationException();
            }

            sb.Append(codeType);
            sb.Append(" ");
            sb.Append((attributes & MethodImplAttributes.Unmanaged) == MethodImplAttributes.Unmanaged ? "unmanaged" : "managed");

            if ((attributes & MethodImplAttributes.PreserveSig) != 0) sb.Append(" preservesig");
            if ((attributes & MethodImplAttributes.ForwardRef) != 0) sb.Append(" forwardref");
            if ((attributes & MethodImplAttributes.InternalCall) != 0) sb.Append(" internalcall");
            if ((attributes & MethodImplAttributes.Synchronized) != 0) sb.Append(" synchronized");
            if ((attributes & MethodImplAttributes.NoInlining) != 0) sb.Append(" noinlining");
            if ((attributes & MethodImplAttributes.AggressiveInlining) != 0) sb.Append(" aggressiveinlining");
            if ((attributes & MethodImplAttributes.NoOptimization) != 0) sb.Append(" nooptimization");

            return sb;
        }

        public static StringBuilder AppendTypeAttributes(StringBuilder sb, TypeAttributes attributes)
        {
            string visibility;
            switch (attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.NotPublic: visibility = "private"; break;
                case TypeAttributes.Public: visibility = "public"; break;
                case TypeAttributes.NestedPrivate: visibility = "nested private"; break;
                case TypeAttributes.NestedFamANDAssem: visibility = "nested famandassem"; break;
                case TypeAttributes.NestedAssembly: visibility = "nested assembly"; break;
                case TypeAttributes.NestedFamily: visibility = "nested family"; break;
                case TypeAttributes.NestedFamORAssem: visibility = "nested famorassem"; break;
                case TypeAttributes.NestedPublic: visibility = "nested public"; break;

                default:
                    throw new InvalidOperationException();
            }

            string layout;
            switch (attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.AutoLayout: layout = "auto"; break;
                case TypeAttributes.SequentialLayout: layout = "sequential"; break;
                case TypeAttributes.ExplicitLayout: layout = "explicit"; break;

                default:
                    throw new InvalidOperationException();
            }

            string stringFormat;
            switch (attributes & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: stringFormat = "ansi"; break;
                case TypeAttributes.UnicodeClass: stringFormat = "unicode"; break;
                case TypeAttributes.AutoClass: stringFormat = "autochar"; break;

                default:
                    throw new InvalidOperationException();
            }

            if ((attributes & TypeAttributes.Interface) != 0) sb.Append("interface ");

            sb.Append(visibility);

            if ((attributes & TypeAttributes.Abstract) != 0) sb.Append(" abstract");

            sb.Append(" ");
            sb.Append(layout);
            sb.Append(" ");
            sb.Append(stringFormat);

            if ((attributes & TypeAttributes.Import) != 0) sb.Append(" import");
            if ((attributes & TypeAttributes.WindowsRuntime) != 0) sb.Append(" windowsruntime");
            if ((attributes & TypeAttributes.Sealed) != 0) sb.Append(" sealed");
            if ((attributes & TypeAttributes.Serializable) != 0) sb.Append(" serializable");
            if ((attributes & TypeAttributes.BeforeFieldInit) != 0) sb.Append(" beforefieldinit");
            if ((attributes & TypeAttributes.SpecialName) != 0) sb.Append(" specialname");
            if ((attributes & TypeAttributes.RTSpecialName) != 0) sb.Append(" rtspecialname");

            return sb;
        }

        static private void AppendMethodInfo(MethodInfo method, StringBuilder sb)
        {
            sb.Append(".method");

            foreach (var attribute in method.CustomAttributes)
            {
                sb.Append(" ");
                AppendCustomAttributeData(attribute, sb);
            }

            sb.Append(" ");
            AppendMethodAttributes(sb, method.Attributes);
            sb.Append(" ");
            AppendParameterInfo(method.ReturnParameter, sb);
            sb.Append(" ");
            sb.Append(method.Name);

            if (method.IsGenericMethod)
            {
                sb.Append("<");
                foreach (var typeParameter in method.GetGenericArguments())
                {
                    AppendType(typeParameter, sb, true);
                    AppendComma(sb);
                }
                RemoveTrailingComma(sb);
                sb.Append(">");
            }

            sb.Append("(");
            foreach (var parameter in method.GetParameters())
            {
                AppendParameterInfo(parameter, sb);
                AppendComma(sb);
            }
            RemoveTrailingComma(sb);
            sb.Append(") ");
            AppendMethodImplAttributes(sb, method.GetMethodImplementationFlags());
        }

        static private void AppendConstructorInfo(ConstructorInfo constructor, StringBuilder sb)
        {
            sb.Append(".method");

            foreach (var attribute in constructor.CustomAttributes)
            {
                sb.Append(" ");
                AppendCustomAttributeData(attribute, sb);
            }

            sb.Append(" ");
            AppendMethodAttributes(sb, constructor.Attributes);
            sb.Append(" ");
            sb.Append("void ");
            sb.Append(constructor.Name);

            if (constructor.IsGenericMethod)
            {
                sb.Append("<");
                foreach (var typeParameter in constructor.GetGenericArguments())
                {
                    AppendType(typeParameter, sb, true);
                    AppendComma(sb);
                }
                RemoveTrailingComma(sb);
                sb.Append(">");
            }

            sb.Append("(");
            foreach (var parameter in constructor.GetParameters())
            {
                AppendParameterInfo(parameter, sb);
                AppendComma(sb);
            }
            RemoveTrailingComma(sb);
            sb.Append(")");

            var implFlags = constructor.GetMethodImplementationFlags();
            if (implFlags.HasFlag(MethodImplAttributes.IL)) sb.Append(" cil");
            if (implFlags.HasFlag(MethodImplAttributes.ForwardRef)) sb.Append(" forwardref");
            if (implFlags.HasFlag(MethodImplAttributes.InternalCall)) sb.Append(" internalcall");
            if (implFlags.HasFlag(MethodImplAttributes.Managed)) sb.Append(" managed");
            if (implFlags.HasFlag(MethodImplAttributes.Native)) sb.Append(" native");
            if (implFlags.HasFlag(MethodImplAttributes.NoInlining)) sb.Append(" noinlining");
            if (implFlags.HasFlag(MethodImplAttributes.NoOptimization)) sb.Append(" nooptimization");
            if (implFlags.HasFlag(MethodImplAttributes.OPTIL)) sb.Append(" optil");
            if (implFlags.HasFlag(MethodImplAttributes.PreserveSig)) sb.Append(" preservesig");
            if (implFlags.HasFlag(MethodImplAttributes.Runtime)) sb.Append(" runtime");
            if (implFlags.HasFlag(MethodImplAttributes.Synchronized)) sb.Append(" synchronized");
            if (implFlags.HasFlag(MethodImplAttributes.Unmanaged)) sb.Append(" unmanaged");
        }

        static private void AppendPropertyInfo(PropertyInfo property, StringBuilder sb)
        {
            sb.Append(".property ");

            foreach (var attribute in property.CustomAttributes)
            {
                AppendCustomAttributeData(attribute, sb);
                sb.Append(" ");
            }
            foreach (var modreq in property.GetRequiredCustomModifiers())
            {
                sb.Append("modreq(");
                AppendType(modreq, sb);
                sb.Append(") ");
            }
            foreach (var modopt in property.GetOptionalCustomModifiers())
            {
                sb.Append("modopt(");
                AppendType(modopt, sb);
                sb.Append(") ");
            }

            if (property.CanRead && property.CanWrite)
            {
                sb.Append("readwrite ");
            }
            else if (property.CanRead)
            {
                sb.Append("readonly ");
            }
            else if (property.CanWrite)
            {
                sb.Append("writeonly ");
            }

            if (property.Attributes.HasFlag(PropertyAttributes.SpecialName)) sb.Append("specialname ");
            if (property.Attributes.HasFlag(PropertyAttributes.RTSpecialName)) sb.Append("rtspecialname ");

            var propertyAccessors = property.GetAccessors();
            if (propertyAccessors.Length > 0)
            {
                sb.Append(propertyAccessors[0].IsStatic ? "static " : "instance ");
            }
            AppendType(property.PropertyType, sb);
            sb.Append(" ");
            sb.Append(property.Name);

            var indexParameters = property.GetIndexParameters();
            if (indexParameters.Length > 0)
            {
                sb.Append("(");
                foreach (var indexParameter in indexParameters)
                {
                    AppendParameterInfo(indexParameter, sb);
                    AppendComma(sb);
                }
                RemoveTrailingComma(sb);
                sb.Append(")");
            }
        }

        static private void AppendFieldInfo(FieldInfo field, StringBuilder sb)
        {
            sb.Append(".field ");

            foreach (var attribute in field.CustomAttributes)
            {
                AppendCustomAttributeData(attribute, sb);
                sb.Append(" ");
            }

            foreach (var modreq in field.GetRequiredCustomModifiers())
            {
                sb.Append("modreq(");
                AppendType(modreq, sb);
                sb.Append(") ");
            }
            foreach (var modopt in field.GetOptionalCustomModifiers())
            {
                sb.Append("modopt(");
                AppendType(modopt, sb);
                sb.Append(") ");
            }

            if (field.IsPrivate) sb.Append("private ");
            if (field.IsPublic) sb.Append("public ");
            if (field.IsFamily) sb.Append("family ");
            if (field.IsAssembly) sb.Append("assembly ");
            if (field.IsFamilyOrAssembly) sb.Append("famorassem ");
            if (field.IsFamilyAndAssembly) sb.Append("famandassem ");

            if (field.IsInitOnly) sb.Append("initonly ");
            if (field.IsLiteral) sb.Append("literal ");
            if (field.IsNotSerialized) sb.Append("notserialized ");
            if (field.Attributes.HasFlag(FieldAttributes.SpecialName)) sb.Append("specialname ");
            if (field.Attributes.HasFlag(FieldAttributes.RTSpecialName)) sb.Append("rtspecialname ");
            if (field.IsPinvokeImpl) sb.Append("pinvokeimpl ");

            sb.Append(field.IsStatic ? "static " : "instance ");
            AppendType(field.FieldType, sb);
            sb.Append(" ");
            sb.Append(field.Name);

            if (field.IsLiteral)
            {
                AppendValue(field.GetRawConstantValue(), sb);
            }
        }

        static private void AppendEventInfo(EventInfo @event, StringBuilder sb)
        {
            sb.Append(".event ");

            foreach (var attribute in @event.CustomAttributes)
            {
                AppendCustomAttributeData(attribute, sb);
                sb.Append(" ");
            }

            if (@event.Attributes.HasFlag(EventAttributes.SpecialName)) sb.Append("specialname ");
            if (@event.Attributes.HasFlag(EventAttributes.RTSpecialName)) sb.Append("rtspecialname ");

            AppendType(@event.EventHandlerType, sb);
            sb.Append(" ");
            sb.Append(@event.Name);
        }
        #endregion

        static public IEnumerable<string> GetMemberSignatures(System.Reflection.Assembly assembly, string fullyQualifiedTypeName)
        {
            var candidates = new List<string>();
            var sb = new StringBuilder();
            var type = assembly.GetType(fullyQualifiedTypeName);
            if (type != null)
            {
                foreach (var constructor in type.GetConstructors(BINDING_FLAGS).OrderBy((member) => member.Name))
                {
                    AppendConstructorInfo(constructor, sb);
                    candidates.Add(sb.ToString());
                    sb.Clear();
                }
                foreach (var method in type.GetMethods(BINDING_FLAGS).OrderBy((member) => member.Name))
                {
                    AppendMethodInfo(method, sb);
                    candidates.Add(sb.ToString());
                    sb.Clear();
                }
                foreach (var property in type.GetProperties(BINDING_FLAGS).OrderBy((member) => member.Name))
                {
                    AppendPropertyInfo(property, sb);
                    candidates.Add(sb.ToString());
                    sb.Clear();
                }
                foreach (var @event in type.GetEvents(BINDING_FLAGS).OrderBy((member) => member.Name))
                {
                    AppendEventInfo(@event, sb);
                    candidates.Add(sb.ToString());
                    sb.Clear();
                }
                foreach (var field in type.GetFields(BINDING_FLAGS).OrderBy((member) => member.Name))
                {
                    AppendFieldInfo(field, sb);
                    candidates.Add(sb.ToString());
                    sb.Clear();
                }
            }
            return candidates;
        }

        static public IEnumerable<string> GetMemberSignatures(System.Reflection.Assembly assembly, string fullyQualifiedTypeName, string memberName)
        {
            IEnumerable<string> retVal = null;
            if (string.IsNullOrWhiteSpace(memberName))
            {
                retVal = GetMemberSignatures(assembly, fullyQualifiedTypeName);
            }
            else
            {
                var sb = new StringBuilder();
                var type = assembly.GetType(fullyQualifiedTypeName);
                var candidates = new SortedSet<string>();

                if (type != null)
                {
                    foreach (var constructor in type.GetConstructors(BINDING_FLAGS))
                    {
                        if (constructor.Name == memberName)
                        {
                            AppendConstructorInfo(constructor, sb);
                            candidates.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    foreach (var method in type.GetMethods(BINDING_FLAGS))
                    {
                        if (method.Name == memberName)
                        {
                            AppendMethodInfo(method, sb);
                            candidates.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    foreach (var property in type.GetProperties(BINDING_FLAGS))
                    {
                        if (property.Name == memberName)
                        {
                            AppendPropertyInfo(property, sb);
                            candidates.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    foreach (var @event in type.GetEvents(BINDING_FLAGS))
                    {
                        if (@event.Name == memberName)
                        {
                            AppendEventInfo(@event, sb);
                            candidates.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    foreach (var field in type.GetFields(BINDING_FLAGS))
                    {
                        if (field.Name == memberName)
                        {
                            AppendFieldInfo(field, sb);
                            candidates.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                }

                retVal = candidates;
            }
            return retVal;
        }
    }
}
