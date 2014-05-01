// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A field of a frame class that represents a variable that has been captured in a lambda.
    /// </summary>
    internal sealed class LambdaCapturedVariable : SynthesizedCapturedVariable
    {
        internal LambdaCapturedVariable(SynthesizedContainer frame, Symbol captured)
            : base(frame, captured, GetType(frame, captured))
        {
        }

        private static TypeSymbol GetType(SynthesizedContainer frame, Symbol captured)
        {
            var local = captured as LocalSymbol;
            if ((object)local != null)
            {
                // if we're capturing a generic frame pointer, construct it with the new frame's type parameters
                var lambdaFrame = local.Type.OriginalDefinition as LambdaFrame;
                if ((object)lambdaFrame != null)
                {
                    return lambdaFrame.ConstructIfGeneric(frame.TypeArgumentsNoUseSiteDiagnostics);
                }
            }
            return frame.TypeMap.SubstituteType((object)local != null ? local.Type : ((ParameterSymbol)captured).Type);
        }

        internal override int IteratorLocalIndex
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }
}