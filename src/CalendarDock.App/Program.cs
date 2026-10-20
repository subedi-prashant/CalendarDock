using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.Widgets.Providers;

namespace CalendarDock.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        var isComServer = args.Contains("-RegisterProcessAsComServer");
        var instance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("main");
        if (!instance.IsCurrent && !isComServer)
        {
            instance.RedirectActivationToAsync(Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs()).AsTask().Wait();
            return 0;
        }

        if (isComServer)
        {
            WidgetFactory.Register();
        }

        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }
}

internal static class WidgetFactory
{
    private const uint CLSCTX_LOCAL_SERVER = 4;
    private const uint REGCLS_MULTIPLEUSE = 1;

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(ref Guid rclsid, [MarshalAs(UnmanagedType.IUnknown)] object pUnk, uint dwClsContext, uint flags, out uint cookie);

    public static void Register()
    {
        var clsid = new Guid(WidgetProvider.CLSID);
        CoRegisterClassObject(ref clsid, new ClassFactory(), CLSCTX_LOCAL_SERVER, REGCLS_MULTIPLEUSE, out _);
    }

    [ComImport]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IClassFactoryNative
    {
        [PreserveSig]
        int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

        [PreserveSig]
        int LockServer(bool fLock);
    }

    [ComVisible(true)]
    private sealed class ClassFactory : IClassFactoryNative
    {
        public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;
            if (pUnkOuter != IntPtr.Zero)
            {
                return unchecked((int)0x80040110);
            }

            ppvObject = WinRT.MarshalInspectable<IWidgetProvider>.FromManaged(new WidgetProvider());
            return 0;
        }

        public int LockServer(bool fLock)
        {
            return 0;
        }
    }
}

