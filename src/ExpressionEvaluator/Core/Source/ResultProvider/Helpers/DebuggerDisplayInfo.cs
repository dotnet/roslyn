// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal class DebuggerDisplayInfo
    {
        private readonly DkmClrType m_targetType;
        private readonly DebuggerDisplayItemInfo m_value;
        private readonly DebuggerDisplayItemInfo m_simpleValue;

        private bool m_hasFavoritesInfo = false;

        public readonly DebuggerDisplayItemInfo Name;
        public readonly DebuggerDisplayItemInfo TypeName;

        public DebuggerDisplayInfo(DkmClrType targetType)
        {
            m_targetType = targetType;
        }

        private DebuggerDisplayInfo(
            DkmClrType targetType,
            DebuggerDisplayItemInfo name,
            DebuggerDisplayItemInfo value,
            DebuggerDisplayItemInfo simpleValue,
            DebuggerDisplayItemInfo typeName,
            bool hasFavoritesInfo)
            : this(targetType)
        {
            Name = name;
            m_value = value;
            m_simpleValue = simpleValue;
            TypeName = typeName;
            m_hasFavoritesInfo = hasFavoritesInfo;
        }

        public bool HasValues { get { return (Name != null || TypeName != null || m_value != null); } }

        public DebuggerDisplayItemInfo GetValue(DkmInspectionContext inspectionContext)
        {
            if (m_simpleValue != null && (inspectionContext.EvaluationFlags & DkmEvaluationFlags.UseSimpleDisplayString) == DkmEvaluationFlags.UseSimpleDisplayString)
            {
                return m_simpleValue;
            }

            return m_value;
        }

        public DebuggerDisplayInfo WithFavoritesInfo(DkmClrObjectFavoritesInfo favoritesInfo)
        {
            var value = m_value;
            var simpleValue = m_simpleValue;

            if (favoritesInfo.DisplayString != null)
            {
                value = new DebuggerDisplayItemInfo(favoritesInfo.DisplayString, m_targetType);
                simpleValue = favoritesInfo.SimpleDisplayString != null ?
                    new DebuggerDisplayItemInfo(favoritesInfo.SimpleDisplayString, m_targetType) : null;
            }

            return new DebuggerDisplayInfo(
                targetType: m_targetType,
                name: Name,
                value: value,
                simpleValue: simpleValue,
                typeName: TypeName,
                hasFavoritesInfo: true);
        }

        public DebuggerDisplayInfo WithDebuggerDisplayAttribute(DkmClrDebuggerDisplayAttribute attribute, DkmClrType attributeTarget)
        {
            var name = Name;
            var value = m_value;
            var simpleValue = m_simpleValue;
            var typeName = TypeName;

            if (attribute.Name != null)
            {
                name = new DebuggerDisplayItemInfo(attribute.Name, attributeTarget);
            }

            // Favorites info takes priority for value and simple value
            if (!m_hasFavoritesInfo && attribute.Value != null)
            {
                value = new DebuggerDisplayItemInfo(attribute.Value, attributeTarget);
                simpleValue = null;
            }

            if (attribute.TypeName != null)
            {
                typeName = new DebuggerDisplayItemInfo(attribute.TypeName, attributeTarget);
            }

            return new DebuggerDisplayInfo(
                targetType: m_targetType,
                name: name,
                value: value,
                simpleValue: simpleValue,
                typeName: typeName,
                hasFavoritesInfo: m_hasFavoritesInfo);
        }
    }

    internal class DebuggerDisplayItemInfo
    {
        public readonly string Value;
        public readonly DkmClrType TargetType;

        public DebuggerDisplayItemInfo(string value, DkmClrType targetType)
        {
            Value = value;
            TargetType = targetType;
        }
    }
}
