// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A field of a frame class that represents a variable that has been captured in a lambda.
    /// </summary>
    internal sealed class LambdaCapturedVariable : SynthesizedFieldSymbolBase
    {
        private readonly TypeSymbol _type;
        private readonly bool _isThis;

        private LambdaCapturedVariable(SynthesizedContainer frame, TypeSymbol type, string fieldName, bool isThisParameter)
            : base(frame,
                   fieldName,
                   isPublic: true,
                   isReadOnly: false,
                   isStatic: false)
        {
            Debug.Assert((object)type != null);

            // lifted fields do not need to have the CompilerGeneratedAttribute attached to it, the closure is already 
            // marked as being compiler generated.
            _type = type;
            _isThis = isThisParameter;
        }

        public static LambdaCapturedVariable Create(LambdaFrame frame, Symbol captured, ref int uniqueId)
        {
            Debug.Assert(captured is LocalSymbol || captured is ParameterSymbol);

            string fieldName = GetCapturedVariableFieldName(captured, ref uniqueId);
            TypeSymbol type = GetCapturedVariableFieldType(frame, captured);
            return new LambdaCapturedVariable(frame, type, fieldName, IsThis(captured));
        }

        private static bool IsThis(Symbol captured)
        {
            var parameter = captured as ParameterSymbol;
            return (object)parameter != null && parameter.IsThis;
        }

        private static string GetCapturedVariableFieldName(Symbol variable, ref int uniqueId)
        {
            if (IsThis(variable))
            {
                return GeneratedNames.ThisProxyFieldName();
            }

            var local = variable as LocalSymbol;
            if ((object)local != null)
            {
                if (local.SynthesizedKind == SynthesizedLocalKind.LambdaDisplayClass)
                {
                    return GeneratedNames.MakeLambdaDisplayLocalName(uniqueId++);
                }

                if (local.SynthesizedKind == SynthesizedLocalKind.ExceptionFilterAwaitHoistedExceptionLocal)
                {
                    return GeneratedNames.MakeHoistedLocalFieldName(local.SynthesizedKind, uniqueId++);
                }
            }

            Debug.Assert(variable.Name != null);
            return variable.Name;
        }

        private static TypeSymbol GetCapturedVariableFieldType(SynthesizedContainer frame, Symbol variable)
        {
            var local = variable as LocalSymbol;
            if ((object)local != null)
            {
                // if we're capturing a generic frame pointer, construct it with the new frame's type parameters
                var lambdaFrame = local.Type.OriginalDefinition as LambdaFrame;
                if ((object)lambdaFrame != null)
                {
                    // lambdaFrame may have less generic type parameters than frame, so trim them down (the first N will always match)
                    var typeArguments = frame.TypeArgumentsNoUseSiteDiagnostics;
                    if (typeArguments.Length > lambdaFrame.Arity)
                    {
                        typeArguments = ImmutableArray.Create(typeArguments, 0, lambdaFrame.Arity);
                    }
                    return lambdaFrame.ConstructIfGeneric(typeArguments.SelectAsArray(TypeMap.TypeSymbolAsTypeWithModifiers));
                }
            }

            return frame.TypeMap.SubstituteType((object)local != null ? local.Type : ((ParameterSymbol)variable).Type).Type;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }

        internal override bool IsCapturedFrame
        {
            get
            {
                return _isThis;
            }
        }
    }
}
