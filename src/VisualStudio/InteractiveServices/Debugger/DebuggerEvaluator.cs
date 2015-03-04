// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.Internal;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.VisualStudio.LanguageServices.Debugging
{
    internal sealed class DebuggerEvaluator : IInteractiveEvaluator
    {
        private const uint RequestRadix = 10;
        private const uint RequestTimeout = uint.MaxValue; // TODO: Use reasonable timeout
        private const uint MaxChildren = 20;

        private readonly IVsDebugger _debugger;
        private TextWriter _output;
        private TextWriter _errorOutput;

        private IInteractiveWindow _currentWindow;

        internal DebuggerEvaluator(IVsDebugger debugger)
        {
            _debugger = debugger;
        }

        public TextWriter Output
        {
            get { return _output; }
            set { _output = value; }
        }

        public TextWriter ErrorOutput
        {
            get { return _errorOutput; }
            set { _errorOutput = value; }
        }

        public IInteractiveWindow CurrentWindow
        {
            get
            {
                return _currentWindow;
            }

            set
            {
                if (_currentWindow != value)
                {
                    _output = value.OutputWriter;
                    _errorOutput = value.ErrorOutputWriter;
                    _currentWindow = value;
                }
            }
        }

        public Task<ExecutionResult> InitializeAsync()
        {
            return ExecutionResult.Succeeded;
        }

        public Task<ExecutionResult> ResetAsync(bool initialize = true)
        {
            return ExecutionResult.Succeeded;
        }

        public bool CanExecuteCode(string text)
        {
            return true;
        }

        public Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            bool success = this.Execute(text);
            return Task.FromResult(new ExecutionResult(success));
        }

        public string FormatClipboard()
        {
            return null;
        }

        public void AbortExecution()
        {
        }

        public string GetConfiguration()
        {
            return null;
        }

        public void Dispose()
        {
        }

        public string GetPrompt()
        {
            return "> ";
        }

        private bool Execute(string code)
        {
            var frame = ((IDebuggerInternal)_debugger).CurrentStackFrame;
            if (frame == null)
            {
                _output.WriteLine("[no stack frame]");
                return false;
            }

            IDebugExpressionContext2 context;
            int hr = frame.GetExpressionContext(out context);
            if (hr != VSConstants.S_OK)
            {
                return false;
            }

            IDebugExpression2 expr;
            string error;
            uint n;
            hr = context.ParseText(code, enum_PARSEFLAGS.PARSE_EXPRESSION, RequestRadix, out expr, out error, out n);
            if (hr != VSConstants.S_OK)
            {
                return false;
            }

            IDebugProperty2 property;
            hr = expr.EvaluateSync(0, uint.MaxValue, null, out property); // TODO: Use ExecuteAsync.
            if (hr != VSConstants.S_OK)
            {
                return false;
            }

            this.ReportResult(property);
            return true;
        }

        private void ReportResult(IDebugProperty2 property)
        {
            var infos = new DEBUG_PROPERTY_INFO[1];
            int hr = property.GetPropertyInfo(
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NO_NONPUBLIC_MEMBERS |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_AUTOEXPAND,
                RequestRadix,
                RequestTimeout,
                null,
                0,
                infos);
            if (hr != VSConstants.S_OK)
            {
                return;
            }

            var info = infos[0];
            _output.WriteLine("[{0}, {1}]", info.bstrValue, info.bstrType);

            // If expandable, expand the first level. This matches
            // the Dev12 Immediate Window behavior.
            if (((info.dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0) &&
                (info.dwAttrib & enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE) != 0)
            {
                IEnumDebugPropertyInfo2 children;
                var filter = default(Guid);
                property.EnumChildren(
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB |
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME |
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE |
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE,
                    RequestRadix,
                    ref filter,
                    enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_ALL,
                    null,
                    RequestTimeout,
                    out children);
                if (hr != VSConstants.S_OK)
                {
                    return;
                }

                uint n;
                hr = children.GetCount(out n);
                if (hr != VSConstants.S_OK)
                {
                    return;
                }

                n = Math.Min(n, MaxChildren);
                infos = new DEBUG_PROPERTY_INFO[n];
                hr = children.Next(n, infos, out n);
                if (hr != VSConstants.S_OK)
                {
                    return;
                }

                for (int i = 0; i < n; i++)
                {
                    info = infos[i];
                    _output.WriteLine("[{0}, {1}, {2}]", info.bstrName, info.bstrValue, info.bstrType);
                }
            }
        }
    }
}
