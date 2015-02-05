// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// This class provides function name information for the Breakpoints window.
    /// </summary>
    internal abstract class LanguageInstructionDecoder<TMethodSymbol> : IDkmLanguageInstructionDecoder where TMethodSymbol : class, IMethodSymbol
    {
        private readonly InstructionDecoder<TMethodSymbol> _instructionDecoder;

        internal LanguageInstructionDecoder(InstructionDecoder<TMethodSymbol> instructionDecoder)
        {
            _instructionDecoder = instructionDecoder;
        }

        string IDkmLanguageInstructionDecoder.GetMethodName(DkmLanguageInstructionAddress languageInstructionAddress, DkmVariableInfoFlags argumentFlags)
        {
            try
            {
                // DkmVariableInfoFlags.FullNames was accepted by the old GetMethodName implementation,
                // but it was ignored.  Furthermore, it's not clear what FullNames would mean with respect
                // to argument names in C# or Visual Basic.  For consistency with the old behavior, we'll
                // just ignore the the flag as well.
                Debug.Assert((argumentFlags & (DkmVariableInfoFlags.FullNames | DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types)) == argumentFlags,
                    "Unexpected argumentFlags", "argumentFlags = {0}", argumentFlags);

                var method = _instructionDecoder.GetMethod((DkmClrInstructionAddress)languageInstructionAddress.Address);
                var includeParameterTypes = argumentFlags.Includes(DkmVariableInfoFlags.Types);
                var includeParameterNames = argumentFlags.Includes(DkmVariableInfoFlags.Names);

                return _instructionDecoder.GetName(method, includeParameterTypes, includeParameterNames);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
