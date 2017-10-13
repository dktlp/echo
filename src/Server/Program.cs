using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;
using System.IO;

using log4net;

using Echo.Runtime;
using Echo.Runtime.Engine;

namespace Echo.Server
{
    class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ProcessEventHandler handler, bool add);
        internal delegate void ProcessEventHandler(ProcessEvent e);

        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        private static Echo.Runtime.Engine.Server Server { get; set; }

        static void Main(string[] args)
        {
            // Read log4net configuration.
            using (Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream("Echo.Server.log4net.config"))
            {
                log4net.Config.XmlConfigurator.Configure(LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy)), stream);
            }
            
            Log.InfoFormat("Starting runtime model and engine v{0}", Assembly.GetEntryAssembly().GetName().Version);
            Log.InfoFormat("Server process started [PID:{0}]", Process.GetCurrentProcess().Id);

            // TODO: How to handle UnhandledException?
            //AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            SetConsoleCtrlHandler(new ProcessEventHandler(OnProcessEvent), true);

            try
            {
                Server = new Echo.Runtime.Engine.Server();
                Log.Info("Server runtime engine instantiated");

                Server.Configure();
                Server.Start();
                Server.WaitForExit();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }

            if (Server != null)
                Server.Dispose();

            Log.InfoFormat("Server process ended [PID:{0}]", Process.GetCurrentProcess().Id);
        }

        //private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    Log.Error(e.ExceptionObject);

        //    if (e.IsTerminating)
        //        Log.FatalFormat("Server process terminated due to fatal error [PID:{0}]", Process.GetCurrentProcess().Id);
        //}

        private static void OnProcessEvent(ProcessEvent e)
        {
            Log.InfoFormat("Process event '{0}' intercepted", e);

            switch (e)
            {
                case ProcessEvent.CtrlBreak:
                case ProcessEvent.CtrlClose:
                case ProcessEvent.CtrlLogoff:
                case ProcessEvent.CtrlShutdown:
                case ProcessEvent.CtrlC:
                    {
                        if (Server != null)
                            Server.Stop();

                        break;
                    }
            }
        }
    }

    internal enum ProcessEvent
    {
        CtrlC = 0,
        CtrlBreak = 1,
        CtrlClose = 2,
        CtrlLogoff = 5,
        CtrlShutdown = 6
    }
}