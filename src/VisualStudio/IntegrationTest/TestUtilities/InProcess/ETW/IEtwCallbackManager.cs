using System;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    interface IEtwCallbackManager : IDisposable
    {
        void Parser_BlockStart(BlockStartEvent arg);
        void Parser_BlockStop(BlockStopEvent arg);
        void Parser_Log(LogEvent arg);
        void StartListening(int functionId, Action<LogEvent> callback);
        void StartListening(int functionId, Action<BlockStartEvent> startCallback, Action<BlockStopEvent> stopCallback);
        void StopListening(int functionId, Action<LogEvent> callback);
        void StopListening(int functionId, Action<BlockStartEvent> startCallback, Action<BlockStopEvent> stopCallback);
        string GetCallbackLogsPerEvent();
    }
}