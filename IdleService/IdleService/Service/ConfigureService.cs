using System;
using Topshelf;

namespace IdleService
{
    internal static class ConfigureService
    {
        internal static void Configure()
        {
            //Catch all uncaught erros and log them
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Utilities.Log(e.ExceptionObject.ToString());

            //Only run if OS is 64bit
            if (!Utilities.Is64BitOS()) { Utilities.Log("Not 64bit"); return; }

            //Only run if OS is Vista or higher
            if (!Utilities.IsWinVistaOrHigher()) { Utilities.Log("Not Vista+"); return; }

            //Configure the TopShelf service library to start the service
            try
            {
                HostFactory.Run(configure =>
                {
                    //set up some events for the TopShelf library, so we are notified of important events automatically
                    //Including starting/stopping the service as well as Windows Session changes.
                    configure.Service<IdleService.MyService>(service =>
                    {
                        service.ConstructUsing(s => new IdleService.MyService());
                        service.WhenStarted((s, hc) => s.Start(hc));
                        service.WhenStopped(s => s.Stop());
                        service.WhenSessionChanged((s, hc, args) => s.SessionChanged(args));
                    });

                    //set some information for TopShelf service registration
                    configure.EnableSessionChanged();
                    configure.ApplyCommandLine();
                    configure.RunAsLocalSystem();
                    configure.SetServiceName("IdleService");
                    configure.SetDisplayName("IdleService");
                    configure.SetDescription("");
                });
            }
            catch (Exception ex)
            {
                Utilities.Log("TopShelf: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}