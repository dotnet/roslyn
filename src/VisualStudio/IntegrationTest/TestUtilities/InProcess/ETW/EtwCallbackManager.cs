using System;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal struct LogEvent
    {
        public readonly int functionId;
        public readonly long timeStampMS;

        public LogEvent(int functionId, long timeStampMS)
        {
            this.functionId = functionId;
            this.timeStampMS = timeStampMS;
        }
    }

    internal struct BlockStartEvent
    {
        public readonly int blockId;
        public readonly int functionId;
        public readonly long timeStampMS;

        public BlockStartEvent(int functionId, int blockId, long timeStampMS)
        {
            this.functionId = functionId;
            this.blockId = blockId;
            this.timeStampMS = timeStampMS;
        }
    }

    internal struct BlockStopEvent
    {
        public readonly int blockId;
        public readonly int functionId;
        public readonly int tick;
        public readonly bool canceled;
        public readonly long timeStampMS;

        public BlockStopEvent(int functionId, int blockId, int tick, bool canceled, long timeStampMS)
        {
            this.functionId = functionId;
            this.blockId = blockId;
            this.tick = tick;
            this.canceled = canceled;
            this.timeStampMS = timeStampMS;
        }
    }

    internal class EtwCallbackManager : IDisposable, IEtwCallbackManager
    {
        private readonly object gate = new object();

        /// <summary>
        /// callbacks for roslyn log event
        /// </summary>
        private ImmutableDictionary<int, ImmutableList<Action<LogEvent>>> logCallbacks = ImmutableDictionary<int, ImmutableList<Action<LogEvent>>>.Empty;

        /// <summary>
        /// callback pairs for roslyn block start event
        /// </summary>
        private ImmutableDictionary<int, ImmutableList<Action<BlockStartEvent>>> blockStartCallbacks = ImmutableDictionary<int, ImmutableList<Action<BlockStartEvent>>>.Empty;

        /// <summary>
        /// callback pairs for roslyn block stop event
        /// </summary>
        private ImmutableDictionary<int, ImmutableList<Action<BlockStopEvent>>> blockStopCallbacks = ImmutableDictionary<int, ImmutableList<Action<BlockStopEvent>>>.Empty;

        public void Parser_Log(LogEvent arg)
        {
            FireCallbacks(logCallbacks, arg.functionId, arg);
        }

        public void Parser_BlockStart(BlockStartEvent arg)
        {
            FireCallbacks(blockStartCallbacks, arg.functionId, arg);
        }

        public void Parser_BlockStop(BlockStopEvent arg)
        {
            FireCallbacks(blockStopCallbacks, arg.functionId, arg);
        }

        private void FireCallbacks<T>(ImmutableDictionary<int, ImmutableList<Action<T>>> dictionary, int functionId, T arg)
        {
            var callbacks = default(ImmutableList<Action<T>>);
            if (!dictionary.TryGetValue(functionId, out callbacks))
            {
                return;
            }

            foreach (var callback in callbacks)
            {
                callback(arg);
            }
        }

        public void StartListening(int functionId, Action<LogEvent> callback)
        {
            Dictionary_Add(ref logCallbacks, functionId, callback);
        }

        public void StopListening(int functionId, Action<LogEvent> callback)
        {
            Dictionary_Remove(ref logCallbacks, functionId, callback);
        }

        public void StartListening(int functionId, Action<BlockStartEvent> startCallback, Action<BlockStopEvent> stopCallback)
        {
            Dictionary_Add(ref blockStartCallbacks, functionId, startCallback);
            Dictionary_Add(ref blockStopCallbacks, functionId, stopCallback);
        }

        public void StopListening(int functionId, Action<BlockStartEvent> startCallback, Action<BlockStopEvent> stopCallback)
        {
            Dictionary_Remove(ref blockStartCallbacks, functionId, startCallback);
            Dictionary_Remove(ref blockStopCallbacks, functionId, stopCallback);
        }

        private void Dictionary_Remove<T>(ref ImmutableDictionary<int, ImmutableList<Action<T>>> dictionary, int functionId, Action<T> callback)
        {
            lock (gate)
            {
                do
                {
                    Dictionary_RemoveNoLock(ref dictionary, functionId, callback);
                } while (dictionary.ContainsKey(functionId) && dictionary[functionId].Contains(callback));
            }
        }

        private void Dictionary_RemoveNoLock<T>(ref ImmutableDictionary<int, ImmutableList<Action<T>>> dictionary, int functionId, Action<T> callback)
        {
            var list = default(ImmutableList<Action<T>>);
            if (!dictionary.TryGetValue(functionId, out list))
            {
                return;
            }

            list = list.Remove(callback);
            dictionary = dictionary.SetItem(functionId, list);

            if (list.Count == 0)
            {
                dictionary = dictionary.Remove(functionId);
            }
        }

        private void Dictionary_Add<T>(ref ImmutableDictionary<int, ImmutableList<Action<T>>> dictionary, int functionId, Action<T> callback)
        {
            lock (gate)
            {
                do
                {
                    Dictionary_AddNoLock(ref dictionary, functionId, callback);
                } while (!dictionary.ContainsKey(functionId) || !dictionary[functionId].Contains(callback));
            }
        }

        private void Dictionary_AddNoLock<T>(ref ImmutableDictionary<int, ImmutableList<Action<T>>> dictionary, int functionId, Action<T> callback)
        {
            var list = default(ImmutableList<Action<T>>);
            if (!dictionary.TryGetValue(functionId, out list))
            {
                dictionary = dictionary.Add(functionId, ImmutableList.Create(callback));
                return;
            }

            list = list.Add(callback);
            dictionary = dictionary.SetItem(functionId, list);
        }

        public virtual void Dispose()
        {
            lock (gate)
            {
                this.logCallbacks = this.logCallbacks.Clear();
                this.blockStartCallbacks = this.blockStartCallbacks.Clear();
                this.blockStopCallbacks = this.blockStopCallbacks.Clear();
            }
        }

        public string GetCallbackLogsPerEvent()
        {
            var sb = new StringBuilder();

            foreach (var kv in logCallbacks)
            {
                sb.AppendLine($"\t Log FunctionId: {kv.Key}, Callback Count: {kv.Value.Count}");
            }

            foreach (var kv in blockStartCallbacks)
            {
                sb.AppendLine($"\t BlockStart FunctionId: {kv.Key}, Callback Count: {kv.Value.Count}");
            }

            foreach (var kv in blockStopCallbacks)
            {
                sb.AppendLine($"\t BlockStop FunctionId : {kv.Key} Count: {kv.Value.Count}");
            }

            return sb.ToString();
        }
    }
}
