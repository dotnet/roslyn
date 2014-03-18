// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A field of a frame class that represents a variable that has been captured in a lambda or iterator.
    /// </summary>
    internal abstract class SynthesizedCapturedVariable : SynthesizedFieldSymbolBase
    {
        private readonly TypeSymbol type;
        private readonly bool isThis;

        internal SynthesizedCapturedVariable(SynthesizedContainer container, Symbol captured, TypeSymbol type)
            : base(container,
                   name: IsThis(captured) ? GeneratedNames.IteratorThisProxyName() : captured.Name,
                   isPublic: true,
                   isReadOnly: false,
                   isStatic: false)
        {
            // lifted fields do not need to have the CompilerGeneratedAttribute attached to it, the closure is already 
            // marked as being compiler generated.
            this.type = type;
            this.isThis = IsThis(captured);
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.type;
        }

        private static bool IsThis(Symbol captured)
        {
            var parameter = captured as ParameterSymbol;
            return (object)parameter != null && parameter.IsThis;
        }

        internal override bool IsCapturedFrame
        {
            get
            {
                return isThis;
            }
        }
    }
}