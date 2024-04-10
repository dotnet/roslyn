// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// This class provides function name information for the Breakpoints window.
    /// </summary>
    internal abstract class LanguageInstructionDecoder<TCompilation, TMethodSymbol, TModuleSymbol, TTypeSymbol, TTypeParameterSymbol, TParameterSymbol> : IDkmLanguageInstructionDecoder
        where TCompilation : Compilation
        where TMethodSymbol : class, IMethodSymbolInternal
        where TModuleSymbol : class, IModuleSymbolInternal
        where TTypeSymbol : class, ITypeSymbolInternal
        where TTypeParameterSymbol : class, ITypeParameterSymbolInternal
        where TParameterSymbol : class, IParameterSymbolInternal
    {
        private readonly InstructionDecoder<TCompilation, TMethodSymbol, TModuleSymbol, TTypeSymbol, TTypeParameterSymbol> _instructionDecoder;

        internal LanguageInstructionDecoder(InstructionDecoder<TCompilation, TMethodSymbol, TModuleSymbol, TTypeSymbol, TTypeParameterSymbol> instructionDecoder)
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
                // just ignore the flag as well.
                Debug.Assert((argumentFlags & (DkmVariableInfoFlags.FullNames | DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types)) == argumentFlags,
                    $"Unexpected argumentFlags '{argumentFlags}'");

                var instructionAddress = (DkmClrInstructionAddress)languageInstructionAddress.Address;
                var compilation = _instructionDecoder.GetCompilation(instructionAddress.ModuleInstance);
                var method = _instructionDecoder.GetMethod(compilation, instructionAddress);
                var includeParameterTypes = argumentFlags.Includes(DkmVariableInfoFlags.Types);
                var includeParameterNames = argumentFlags.Includes(DkmVariableInfoFlags.Names);

                return _instructionDecoder.GetName(method, includeParameterTypes, includeParameterNames);
            }
            catch (NotImplementedMetadataException)
            {
                return languageInstructionAddress.GetMethodName(argumentFlags);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
