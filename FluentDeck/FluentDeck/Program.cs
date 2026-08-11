using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace FluentDeck;

public static class Program
{
    [STAThread]
    static void Main(string[] _)
    {
        // Set the base directory before any Windows App SDK code initializes
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
#pragma warning disable CA1806
            new App();
#pragma warning restore CA1806
        });
    }
}
