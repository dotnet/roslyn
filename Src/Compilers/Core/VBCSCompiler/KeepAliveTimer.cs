using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// After the keep alive timer expires, if the server is quiet
    /// then it is shutdown.
    /// </summary>
    /// <remarks>
    /// If -1, the server never shuts down automatically.
    /// If 0, the server shuts down after the first compilation.
    /// </remarks>
    internal class KeepAliveTimer
    {
        private CancellationTokenSource cts = null;
        private int keepAliveHighwaterMark;
        private bool isDefault = true;

        /// <remarks>
        /// N.B. The keep-alive can only be set when there is no timer running.
        ///
        /// The keep-alive for the compiler can be set in various ways, and the
        /// following rules govern how it is decided.
        /// 1) The initial keep alive is set by the constructor.
        /// 2) The first attempt to set the keep-alive will succeed, regardless
        ///    of the value given. This allows clients to override the startup
        ///    default of the keep-alive.
        /// 3) Any subsequent attempts to set the keep-alive will only succeed
        ///    if they are greater than the current keep-alive. This prevents
        ///    any client from decreasing another client's keep-alive
        ///    unexpectedly.
        /// </remarks>
        public int KeepAliveTime
        {
            get { return keepAliveHighwaterMark; }
            set
            {
                Debug.Assert(this.cts == null);

                lock (this)
                {
                    if (isDefault || value > keepAliveHighwaterMark)
                    {
                        keepAliveHighwaterMark = value;
                        isDefault = false;
                    }
                }
            }
        }

        public KeepAliveTimer(int initialKeepAlive)
        {
            keepAliveHighwaterMark = initialKeepAlive;
        }

        public bool StopAfterFirstConnection => KeepAliveTime == 0;
        public bool IsKeepAliveFinite => KeepAliveTime > 0;

        public Task StartTimer()
        {
            lock (this)
            {
                if (this.cts == null)
                {
                    this.cts = new CancellationTokenSource();
                    return Task.Delay(this.KeepAliveTime,
                        this.cts.Token);
                }
            }
            return null;
        }

        public void CancelIfActive()
        {
            lock (this)
            {
                if (this.cts != null)
                {
                    this.cts.Cancel();
                    this.cts = null;
                }
            }
        }

        public void Clear()
        {
            lock (this)
            {
                this.cts = null;
            }
        }
    }
}
