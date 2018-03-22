// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities
{
    public static class DispatcherExtensions
    {
        public static void DoEvents(this Dispatcher dispatcher)
        {
            EnsureHooked();
            // A DispatcherFrame represents a loop that processes pending work
            // items.
            var frame = new DispatcherFrame();
            var callback = (Action<DispatcherFrame>)(f => f.Continue = false);

            try
            {
                // Executes the specified delegate asynchronously.  When it is 
                // complete mark the frame as complete so the dispatcher loop
                // pops out (stops).
                var operation = dispatcher.BeginInvoke(
                    DispatcherPriority.SystemIdle, callback, frame);

                // Start the loop.  It will process all items in the queue, then 
                // will process the above callback.  That callback will tell the
                // loop to then stop processing.
                Dispatcher.PushFrame(frame);

                if (operation.Status != DispatcherOperationStatus.Completed)
                {
                    operation.Abort();
                }
            }
            catch (TargetParameterCountException ex) when (Environment.FailFast("Target Parameter Count Exception", ex))
            {
                var methodInfoBuilder = new StringBuilder();
                methodInfoBuilder.AppendLine("Caught TargetParameterCountException in DoEvents. Printing MethodInfos in reverse execution order.");
                PropertyInfo fullNameProperty = typeof(MethodBase).GetProperty("FullName", BindingFlags.NonPublic | BindingFlags.Instance);
                lock (s_lock)
                {
                    var orderedInfos = s_delegateInfos.Select(kvp => (kvp.Key, kvp.Value)).OrderByDescending(tuple => tuple.Value.startedTime);
                    foreach (var (info, (args, numArgs, posted, startedTime)) in orderedInfos)
                    {
                        methodInfoBuilder.Append($"{info.ReturnType.Name} {fullNameProperty.GetValue(info)} (");
                        var useComma = false;
                        foreach (var param in info.GetParameters())
                        {
                            methodInfoBuilder.Append($"{(useComma ? ", " : "")}{param.ParameterType.Name} {param.Name}");
                            useComma = true;
                        }
                        methodInfoBuilder.AppendLine(")");
                        methodInfoBuilder.AppendLine($"\tNum Args: {numArgs}");
                        if (args is Array arr)
                        {
                            methodInfoBuilder.Append("\tArgs (array):");
                            useComma = true;
                            foreach (var el in arr)
                            {
                                methodInfoBuilder.Append($"{(useComma ? "," : "")} {el}");

                            }
                            methodInfoBuilder.AppendLine();
                        }
                        else
                        {
                            methodInfoBuilder.AppendLine($"\tArgs: {args}");
                        }
                        methodInfoBuilder.AppendLine($"\tPosted: {posted} Time: {startedTime.ToShortTimeString()}");
                    }
                    s_delegateInfos.Clear();
                }
                throw new TargetParameterCountException(methodInfoBuilder.ToString(), ex);
            }

            bool IfTargetParameterCountExceptionFailFast(TargetParameterCountException ex)
            {
                Environment.FailFast("Target Parameter Count Exception", ex);
                return true;
            }
        }

        // Dispatcher hooks for troubleshooting DoEvents calls
        #region DispatcherHooks

        private readonly static object s_lock = new object();
        private static bool s_hooked = false;

        private readonly static Dictionary<MethodInfo, (object args, int numArgs, bool posted, DateTime startedTime)> s_delegateInfos = new Dictionary<MethodInfo, (object, int, bool, DateTime)>();

        private static void EnsureHooked()
        {
            if (s_hooked) return;
            lock (s_lock)
            {
                if (s_hooked) return;
                s_hooked = true;
                Dispatcher.CurrentDispatcher.Hooks.OperationPosted += Hooks_OperationPosted;
                Dispatcher.CurrentDispatcher.Hooks.OperationCompleted += Hooks_OperationCompleted;
                Dispatcher.CurrentDispatcher.Hooks.OperationStarted += Hooks_OperationStarted;
            }
        }

        private static void Hooks_OperationPosted(object sender, DispatcherHookEventArgs e)
        {
            var (info, args, numArgs) = GetFields(e.Operation);
            s_delegateInfos[info] = (args, numArgs, true, DateTime.UtcNow);
        }

        private static void Hooks_OperationStarted(object sender, DispatcherHookEventArgs e)
        {
            var (info, args, numArgs) = GetFields(e.Operation);
            lock (s_lock)
            {
                s_delegateInfos[info] = (args, numArgs, false, DateTime.UtcNow);
            }
        }

        private static (MethodInfo info, object args, int numArgs) GetFields(DispatcherOperation op)
        {

            FieldInfo methodField = typeof(DispatcherOperation).GetField("_method", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo argsField = typeof(DispatcherOperation).GetField("_args", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo numArgsField = typeof(DispatcherOperation).GetField("_numArgs", BindingFlags.NonPublic | BindingFlags.Instance);
            var invokedDelegate = (Delegate)methodField.GetValue(op);
            object args = argsField.GetValue(op);
            var numArgs = (int)numArgsField.GetValue(op);
            return (invokedDelegate.Method, args, numArgs);
        }

        private static void Hooks_OperationCompleted(object sender, DispatcherHookEventArgs e)
        {
            var (info, _, _) = GetFields(e.Operation);
            lock (s_lock)
            {
                s_delegateInfos.Remove(info);
            }
        }

        #endregion
    }
}
