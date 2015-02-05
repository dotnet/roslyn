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
    internal abstract class FrameDecoder : IDkmLanguageFrameDecoder
    {
        private readonly InstructionDecoder _instructionDecoder;

        internal FrameDecoder(InstructionDecoder instructionDecoder)
        {
            _instructionDecoder = instructionDecoder;
        }

        void IDkmLanguageFrameDecoder.GetFrameName(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmVariableInfoFlags argumentFlags, DkmCompletionRoutine<DkmGetFrameNameAsyncResult> completionRoutine)
        {
            try
            {
                Debug.Assert((argumentFlags & (DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types | DkmVariableInfoFlags.Values)) == argumentFlags,
                    "Unexpected argumentFlags", "argumentFlags = {0}", argumentFlags);

                var instructionAddress = (DkmClrInstructionAddress)frame.InstructionAddress;
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
                            try
                            {
                                var builder = ArrayBuilder<string>.GetInstance();
                                foreach (var argument in result.Arguments)
                                {
                                    var evaluatedArgument = argument as DkmSuccessEvaluationResult;
                                    // Not expecting Expandable bit, at least not from this EE.
                                    Debug.Assert((evaluatedArgument == null) || (evaluatedArgument.Flags & DkmEvaluationResultFlags.Expandable) == 0);
                                    builder.Add((evaluatedArgument != null) ? evaluatedArgument.Value : null);
                                }

                                var frameName = _instructionDecoder.GetName(instructionAddress, includeParameterTypes, includeParameterNames, builder);
                                builder.Free();
                                completionRoutine(new DkmGetFrameNameAsyncResult(frameName));
                            }
                            // TODO: Consider calling DkmComponentManager.ReportCurrentNonFatalException() to
                            // trigger a non-fatal Watson when this occurs.
                            catch (Exception e) when (!ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
                            {
                                completionRoutine(DkmGetFrameNameAsyncResult.CreateErrorResult(e));
                            }
                            finally
                            {
                                foreach (var argument in result.Arguments)
                                {
                                    argument.Close();
                                }
                            }
                        });
                }
                else
                {
                    var frameName = _instructionDecoder.GetName(instructionAddress, includeParameterTypes, includeParameterNames, null);
                    completionRoutine(new DkmGetFrameNameAsyncResult(frameName));
                }
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        void IDkmLanguageFrameDecoder.GetFrameReturnType(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmCompletionRoutine < DkmGetFrameReturnTypeAsyncResult > completionRoutine)
        {
            try
            {
                var returnType = _instructionDecoder.GetReturnType((DkmClrInstructionAddress)frame.InstructionAddress);
                var result = new DkmGetFrameReturnTypeAsyncResult(returnType);
                completionRoutine(result);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
