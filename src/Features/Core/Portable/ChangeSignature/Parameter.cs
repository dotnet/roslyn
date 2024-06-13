// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    /// <summary>
    /// Base type for Parameter information, whether the parameter
    /// is preexisting or new.
    /// </summary>
    internal abstract class Parameter
    {
        public abstract bool HasDefaultValue { get; }
        public abstract string Name { get; }
    }

    internal sealed class ExistingParameter(IParameterSymbol param) : Parameter
    {
        public IParameterSymbol Symbol { get; } = param;

        public override bool HasDefaultValue => Symbol.HasExplicitDefaultValue;
        public override string Name => Symbol.Name;
    }

    internal sealed class AddedParameter : Parameter
    {
        public AddedParameter(
            ITypeSymbol type,
            string typeName,
            string name,
            CallSiteKind callSiteKind,
            string callSiteValue = "",
            bool isRequired = true,
            string defaultValue = "",
            bool typeBinds = true)
        {
            Type = type;
            TypeBinds = typeBinds;
            TypeName = typeName;
            Name = name;
            CallSiteValue = callSiteValue;

            IsRequired = isRequired;
            DefaultValue = defaultValue;
            CallSiteKind = callSiteKind;

            // Populate the call site text for the UI
            switch (CallSiteKind)
            {
                case CallSiteKind.Value:
                case CallSiteKind.ValueWithName:
                    CallSiteValue = callSiteValue;
                    break;
                case CallSiteKind.Todo:
                    CallSiteValue = FeaturesResources.ChangeSignature_NewParameterIntroduceTODOVariable;
                    break;
                case CallSiteKind.Omitted:
                    CallSiteValue = FeaturesResources.ChangeSignature_NewParameterOmitValue;
                    break;
                case CallSiteKind.Inferred:
                    CallSiteValue = FeaturesResources.ChangeSignature_NewParameterInferValue;
                    break;
                default:
                    throw ExceptionUtilities.Unreachable();
            }
        }

        public override string Name { get; }
        public override bool HasDefaultValue => !string.IsNullOrWhiteSpace(DefaultValue);

        public ITypeSymbol Type { get; }
        public string TypeName { get; }
        public bool TypeBinds { get; }

        public CallSiteKind CallSiteKind { get; }

        /// <summary>
        /// Display string for the Call Site column in the Change Signature dialog.
        /// </summary>
        public string CallSiteValue { get; }

        /// <summary>
        /// True if required, false if optional with a default value.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Value to use in the declaration of an optional parameter.
        /// E.g. the "3" in M(int x = 3);
        /// </summary>
        public string DefaultValue { get; }

        // For test purposes: to display assert failure details in tests.
        public override string ToString() => $"{Type.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters))} {Name} ({CallSiteValue})";
    }
}
