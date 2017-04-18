using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Diagnostics.Eventing;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class EtwListener : EtwCallbackManager
    {
        private const string SessionName = "RoslynPerformanceTestSession";

        private const int LogFlushInMillisecond = 100;

        /// <summary>
        /// process id that we are monitoring
        /// </summary>
        private readonly int processId;

        /// <summary>
        /// real time etw trace event session
        /// </summary>
        private readonly TraceEventSession session;

        /// <summary>
        /// roslyn event source
        /// </summary>
        private readonly ETWTraceEventSource source;

        /// <summary>
        /// roslyn event source parser
        /// </summary>
        private readonly RoslynEventSourceTraceEventParser parser;

        /// <summary>
        /// Handle to the thread that is reading events from the source
        /// </summary>
        private readonly Thread traceEventSourceWorkerThread;

        private readonly ManualResetEventSlim done;

        public EtwListener(Process process)
        {
            this.processId = process.Id;
            this.done = new ManualResetEventSlim(false);

            // make sure we don't have same session still pending around
            VerifySession();

            // give it "null" for filename so that it becomes real time session
            this.session = new TraceEventSession(SessionName, fileName: null);
            this.session.StopOnDispose = true;

            // enable listening etw event for the roslyn event source
            this.session.EnableProvider(RoslynEventSourceTraceEventParser.ProviderGuid, TraceEventLevel.Informational);

            // create event source for our session
            this.source = new ETWTraceEventSource(SessionName, TraceEventSourceType.Session);

            // connect event source to our event parser
            this.parser = new RoslynEventSourceTraceEventParser(this.source);
            this.parser.Log += arg =>
            {
                if (arg.ProcessID == this.processId)
                {
                    Parser_Log(new LogEvent((int)arg.functionId, (long)arg.TimeStampRelativeMSec));
                }
            };

            this.parser.BlockStart += arg =>
            {
                if (arg.ProcessID == this.processId)
                {
                    Parser_BlockStart(new BlockStartEvent((int)arg.functionId, arg.blockId, (long)arg.TimeStampRelativeMSec));
                }
            };

            this.parser.BlockStop += arg =>
            {
                if (arg.ProcessID == this.processId)
                {
                    Parser_BlockStop(new BlockStopEvent((int)arg.functionId, arg.blockId, arg.tick, false, (long)arg.TimeStampRelativeMSec));
                }
            };

            this.parser.BlockCanceled += arg =>
            {
                if (arg.ProcessID == this.processId)
                {
                    Parser_BlockStop(new BlockStopEvent((int)arg.functionId, arg.blockId, arg.tick, true, (long)arg.TimeStampRelativeMSec));
                }
            };

            // start processing etw process on the worker thread
            this.traceEventSourceWorkerThread = new Thread(() =>
            {
                try
                {
                    var result = this.source.Process();
                    done.Set();

                    if (result)
                    {
                        Debug.WriteLine("Processing didn't finish properly");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine("Processing Failed : " + ex.ToString());
                }
            });

            this.traceEventSourceWorkerThread.IsBackground = true;
            this.traceEventSourceWorkerThread.Start();
        }

        private static void VerifySession()
        {
            for (var i = 0; i < 10; i++)
            {
                if (TraceEventSession.GetActiveSessionNames().All(n => n != SessionName))
                {
                    return;
                }

                try
                {
                    // try remove the session
                    var temp = new TraceEventSession(SessionName);
                    temp.Stop();
                }
                catch
                {
                }

                Thread.Sleep(5000);
            }

            throw new Exception("Session already exist - previous run should have failed badly");
        }

        public override void Dispose()
        {
            this.source.StopProcessing();

            done.Wait(TimeSpan.FromMinutes(5));
            done.Dispose();

            this.session.Stop();

            this.source.Dispose();
            this.session.Dispose();

            base.Dispose();
        }
    }
}
