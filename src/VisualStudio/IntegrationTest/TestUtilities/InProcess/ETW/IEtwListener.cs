using System;
using System.Diagnostics.Tracing;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class InProcessEtwEventListener : EventListener, IEtwCallbackManager
    {
        private const int LogEventId = 1;
        private const int BlockStartEventId = 2;
        private const int BlockStopEventId = 3;
        private const int BlockCancelEventId = 4;

        public readonly EtwCallbackManager CallbackManager = new EtwCallbackManager();

        public InProcessEtwEventListener()
        {
            EnableEvents(RoslynEventSource.Instance, EventLevel.LogAlways);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == LogEventId)
            {
                CallbackManager.Parser_Log(new LogEvent((int)eventData.Payload[1], DateTime.UtcNow.Ticks));
            }
            else if (eventData.EventId == BlockStartEventId)
            {
                CallbackManager.Parser_BlockStart(new BlockStartEvent((int)eventData.Payload[1], (int)eventData.Payload[2], DateTime.UtcNow.Ticks));
            }
            else if (eventData.EventId == BlockStopEventId)
            {
                CallbackManager.Parser_BlockStop(new BlockStopEvent((int)eventData.Payload[0], (int)eventData.Payload[2], (int)eventData.Payload[1], false, DateTime.UtcNow.Ticks));
            }
            else if (eventData.EventId == BlockCancelEventId)
            {
                CallbackManager.Parser_BlockStop(new BlockStopEvent((int)eventData.Payload[0], (int)eventData.Payload[2], (int)eventData.Payload[1], true, DateTime.UtcNow.Ticks));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            CallbackManager.Dispose();
        }

        public void Parser_BlockStart(BlockStartEvent arg)
        {
            CallbackManager.Parser_BlockStart(arg);
        }

        public void Parser_BlockStop(BlockStopEvent arg)
        {
            CallbackManager.Parser_BlockStop(arg);
        }

        public void Parser_Log(LogEvent arg)
        {
            CallbackManager.Parser_Log(arg);
        }

        public void StartListening(int functionId, Action<LogEvent> callback)
        {
            CallbackManager.StartListening(functionId, callback);
        }

        public void StartListening(int functionId, Action<BlockStartEvent> startCallback, Action<BlockStopEvent> stopCallback)
        {
            CallbackManager.StartListening(functionId, startCallback, stopCallback);
        }

        public void StopListening(int functionId, Action<LogEvent> callback)
        {
            CallbackManager.StopListening(functionId, callback);
        }

        public void StopListening(int functionId, Action<BlockStartEvent> startCallback, Action<BlockStopEvent> stopCallback)
        {
            CallbackManager.StopListening(functionId, startCallback, stopCallback);
        }

        public string GetCallbackLogsPerEvent()
        {
            // no logging for in process logger
            return string.Empty;
        }
    }
}