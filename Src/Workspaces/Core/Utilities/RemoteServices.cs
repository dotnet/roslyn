using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

namespace Roslyn.Utilities
{
    internal class RemoteServices : MarshalByRefObject
    {
        private const string ServiceName = "RemoteServices";
        private const int ProcessStartupTimeout = 30000; // 30 seconds
        private static readonly NonReentrantLock gate = new NonReentrantLock(); // gates process startup
        private static RemoteServices remoteInstance;
        private static TerminationWatcher watcher;
        private static bool isRemoteProcess;
        private static readonly ManualResetEventSlim clientExited = new ManualResetEventSlim(false);
        private static IChannel ipcChannel;

        public static bool CurrentProcessIsRemoteServiceProcess
        {
            get { return isRemoteProcess; }
        }

        /// <summary>
        /// The single remote services instance. This is for client-side access.
        /// </summary>
        private static RemoteServices Instance
        {
            get
            {
                if (remoteInstance == null)
                {
                    using (gate.DisposableWait())
                    {
                        if (remoteInstance == null)
                        {
                            StartRemoteServicesProcess();
                            watcher = new TerminationWatcher();
                        }
                    }
                }

                return remoteInstance;
            }
        }

        public override object InitializeLifetimeService()
        {
            // return null so the object/connection won't timeout
            return null;
        }

        // this class signals shutdown when the client AppDomain tears down.
        private class TerminationWatcher
        {
            ~TerminationWatcher()
            {
                try
                {
                    RemoteServices.ShutdownRemoteServiceProcess();
                }
                catch (Exception)
                {
                    // we tried, but were denied.
                }
            }
        }

        /// <summary>
        /// Start the remote services process
        /// </summary>
        private static void StartRemoteServicesProcess()
        {
            // create global semaphore to tell us when the remote service process is ready
            string semaphoreName = Guid.NewGuid().ToString();
            string ipcPortName = semaphoreName; // use the same name for the IPC port
            Semaphore serviceStart = new Semaphore(0, 1, semaphoreName);

            // launch a separate 64 bit process that runs this dll (we are really an exe!)
            string remoteServicesLocation = typeof(RemoteServices).Assembly.Location;
            var processInfo = new ProcessStartInfo(remoteServicesLocation, semaphoreName);
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;
            processInfo.WorkingDirectory = System.Environment.CurrentDirectory;
            var process = Process.Start(processInfo);

            // wait until the remote service process is ready
            if (!serviceStart.WaitOne(ProcessStartupTimeout))
            {
                throw new InvalidOperationException("Could not start remote service process");
            }

            // this is the remote url for the RemoteServices instance
            string url = string.Format("ipc://{0}/{1}", ipcPortName, ServiceName);

            ipcChannel = new IpcClientChannel(ipcPortName, sinkProvider: null);
            ChannelServices.RegisterChannel(ipcChannel, ensureSecurity: false);
            RemotingConfiguration.RegisterWellKnownClientType(typeof(RemoteServices), url);

            remoteInstance = (RemoteServices)Activator.GetObject(typeof(RemoteServices), url);
            remoteInstance.Initialize(Process.GetCurrentProcess().Id);
        }

        private static void ShutdownRemoteServiceProcess()
        {
            RemoteServices.Instance.Shutdown();

            if (ipcChannel != null)
            {
                ChannelServices.UnregisterChannel(ipcChannel);
                ipcChannel = null;
            }
        }

        /// <summary>
        /// Entry point for remote service process
        /// </summary>
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string semaphoreName = args[0];
                string ipcPortName = semaphoreName;

                IpcServerChannel channel = null;

                try
                {
                    // semaphore should initially have one entry
                    using (Semaphore servicesStart = Semaphore.OpenExisting(semaphoreName))
                    {
                        if (servicesStart != null)
                        {
                            isRemoteProcess = true;

                            channel = new IpcServerChannel(ipcPortName);
                            ChannelServices.RegisterChannel(channel, ensureSecurity: false);
                            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteServices), ServiceName, WellKnownObjectMode.Singleton);

                            servicesStart.Release();

                            // block until client process exits or the app domain shuts down
                            clientExited.Wait();
                        }
                    }
                }
                finally
                {
                    if (channel != null)
                    {
                        ChannelServices.UnregisterChannel(channel);
                    }
                }
            }
        }

        /// <summary>
        /// Shuts down the remote services process.
        /// </summary>
        public void Shutdown()
        {
            clientExited.Set();
        }

        /// <summary>
        /// Initializes the remote services process.
        /// </summary>
        public void Initialize(int clientProcessId)
        {
            Process clientProcess;
            try
            {
                clientProcess = Process.GetProcessById(clientProcessId);
            }
            catch (ArgumentException)
            {
                // client died prematurely:
                clientExited.Set();
                return;
            }

            // this will cause shutdown when the client process exits
            clientProcess.EnableRaisingEvents = true;
            clientProcess.Exited += new EventHandler((_, __) => clientExited.Set());

            if (clientProcess.HasExited)
            {
                clientExited.Set();
                return;
            }
        }

        public object CreateInstance(string typeName)
        {
            var type = typeof(RemoteServices).Assembly.GetType(typeName);
            if (type != null)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        public static T CreateInstance<T>()
        {
            return (T)Instance.CreateInstance(typeof(T).FullName);
        }
    }
}