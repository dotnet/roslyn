// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// This class provides method name, argument and return type information for the Call Stack window and DTE.
    /// </summary>
    /// <remarks>
    /// While it might be nice to provide language-specific syntax in the Call Stack window, previous implementations have
    /// always used C# syntax (but with language-specific "special names").  Since these names are exposed through public
    /// APIs, we will remain consistent with the old behavior (for consumers who may be parsing the frame names).
    /// </remarks>
    internal abstract class FrameDecoder<TCompilation, TMethodSymbol, TModuleSymbol, TTypeSymbol, TTypeParameterSymbol> : IDkmLanguageFrameDecoder
        where TCompilation : Compilation
        where TMethodSymbol : class, IMethodSymbol
        where TModuleSymbol : class, IModuleSymbol
        where TTypeSymbol : class, ITypeSymbol
        where TTypeParameterSymbol : class, ITypeParameterSymbol
    {
        private readonly InstructionDecoder<TCompilation, TMethodSymbol, TModuleSymbol, TTypeSymbol, TTypeParameterSymbol> _instructionDecoder;

        internal FrameDecoder(InstructionDecoder<TCompilation, TMethodSymbol, TModuleSymbol, TTypeSymbol, TTypeParameterSymbol> instructionDecoder)
        {
            _instructionDecoder = instructionDecoder;
        }

        void IDkmLanguageFrameDecoder.GetFrameName(
            DkmInspectionContext inspectionContext,
            DkmWorkList workList,
            DkmStackWalkFrame frame,
            DkmVariableInfoFlags argumentFlags,
            DkmCompletionRoutine<DkmGetFrameNameAsyncResult> completionRoutine)
        {
            try
            {
                Debug.Assert((argumentFlags & (DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types | DkmVariableInfoFlags.Values)) == argumentFlags,
                    $"Unexpected argumentFlags '{argumentFlags}'");

                GetNameWithGenericTypeArguments(inspectionContext, workList, frame,
                    onSuccess: method => GetFrameName(inspectionContext, workList, frame, argumentFlags, completionRoutine, method),
                    onFailure: e => completionRoutine(DkmGetFrameNameAsyncResult.CreateErrorResult(e)));
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        void IDkmLanguageFrameDecoder.GetFrameReturnType(
            DkmInspectionContext inspectionContext,
            DkmWorkList workList,
            DkmStackWalkFrame frame,
            DkmCompletionRoutine<DkmGetFrameReturnTypeAsyncResult> completionRoutine)
        {
            try
            {
                GetNameWithGenericTypeArguments(inspectionContext, workList, frame,
                    onSuccess: method => completionRoutine(new DkmGetFrameReturnTypeAsyncResult(_instructionDecoder.GetReturnTypeName(method))),
                    onFailure: e => completionRoutine(DkmGetFrameReturnTypeAsyncResult.CreateErrorResult(e)));
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void GetNameWithGenericTypeArguments(
            DkmInspectionContext inspectionContext,
            DkmWorkList workList,
            DkmStackWalkFrame frame,
            Action<TMethodSymbol> onSuccess,
            Action<Exception> onFailure)
        {
            // NOTE: We could always call GetClrGenericParameters, pass them to GetMethod and have that
            // return a constructed method symbol, but it seems unwise to call GetClrGenericParameters
            // for all frames (as this call requires a round-trip to the debuggee process).
            var instructionAddress = (DkmClrInstructionAddress)frame.InstructionAddress;
            var compilation = _instructionDecoder.GetCompilation(instructionAddress.ModuleInstance);
            var method = _instructionDecoder.GetMethod(compilation, instructionAddress);
            var typeParameters = _instructionDecoder.GetAllTypeParameters(method);
            if (!typeParameters.IsEmpty)
            {
                frame.GetClrGenericParameters(
                    workList,
                    result =>
                    {
                        try
                        {
                            // DkmGetClrGenericParametersAsyncResult.ParameterTypeNames will throw if ErrorCode != 0.
                            var serializedTypeNames = (result.ErrorCode == 0) ? result.ParameterTypeNames : null;
                            var typeArguments = _instructionDecoder.GetTypeSymbols(compilation, method, serializedTypeNames);
                            if (!typeArguments.IsEmpty)
                            {
                                method = _instructionDecoder.ConstructMethod(method, typeParameters, typeArguments);
                            }
                            onSuccess(method);
                        }
                        catch (Exception e)
                        {
                            onFailure(e);
                        }
                    });
            }
            else
            {
                onSuccess(method);
            }
        }

        private void GetFrameName(
            DkmInspectionContext inspectionContext,
            DkmWorkList workList,
            DkmStackWalkFrame frame,
            DkmVariableInfoFlags argumentFlags,
            DkmCompletionRoutine<DkmGetFrameNameAsyncResult> completionRoutine,
            TMethodSymbol method)
        {
            var includeParameterTypes = argumentFlags.Includes(DkmVariableInfoFlags.Types);
            var includeParameterNames = argumentFlags.Includes(DkmVariableInfoFlags.Names);

            if (argumentFlags.Includes(DkmVariableInfoFlags.Values))
            {
                // No need to compute the Expandable bit on
                // argument values since that can be expensive.
                inspectionContext = DkmInspectionContext.Create(
                    inspectionContext.InspectionSession,
                    inspectionContext.RuntimeInstance,
                    inspectionContext.Thread,
                    inspectionContext.Timeout,
                    inspectionContext.EvaluationFlags | DkmEvaluationFlags.NoExpansion,
                    inspectionContext.FuncEvalFlags,
                    inspectionContext.Radix,
                    inspectionContext.Language,
                    inspectionContext.ReturnValue,
                    inspectionContext.AdditionalVisualizationData,
                    inspectionContext.AdditionalVisualizationDataPriority,
                    inspectionContext.ReturnValues);

                // GetFrameArguments returns an array of formatted argument values. We'll pass
                // ourselves (GetFrameName) as the continuation of the GetFrameArguments call.
                inspectionContext.GetFrameArguments(
                    workList,
                    frame,
                    result =>
                    {
                        // DkmGetFrameArgumentsAsyncResult.Arguments will throw if ErrorCode != 0.
                        var argumentValues = (result.ErrorCode == 0) ? result.Arguments : null;
                        try
                        {
                            ArrayBuilder<string> builder = null;
                            if (argumentValues != null)
                            {
                                builder = ArrayBuilder<string>.GetInstance();
                                foreach (var argument in argumentValues)
                                {
                                    var formattedArgument = argument as DkmSuccessEvaluationResult;
                                    // Not expecting Expandable bit, at least not from this EE.
                                    Debug.Assert((formattedArgument == null) || (formattedArgument.Flags & DkmEvaluationResultFlags.Expandable) == 0);
                                    builder.Add(formattedArgument?.Value);
                                }
                            }

                            var frameName = _instructionDecoder.GetName(method, includeParameterTypes, includeParameterNames, argumentValues: builder);
                            builder?.Free();
                            completionRoutine(new DkmGetFrameNameAsyncResult(frameName));
                        }
                        catch (Exception e)
                        {
                            completionRoutine(DkmGetFrameNameAsyncResult.CreateErrorResult(e));
                        }
                        finally
                        {
                            if (argumentValues != null)
                            {
                                foreach (var argument in argumentValues)
                                {
                                    argument.Close();
                                }
                            }
                        }
                    });
            }
            else
            {
                var frameName = _instructionDecoder.GetName(method, includeParameterTypes, includeParameterNames, argumentValues: null);
                completionRoutine(new DkmGetFrameNameAsyncResult(frameName));
            }
        }
    }
}
