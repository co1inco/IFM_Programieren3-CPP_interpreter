using System.Globalization;

namespace CppInterpreter.Test;

public static class AssemblySetup
{

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        CultureInfo ci = new CultureInfo("en-us");
        Thread.CurrentThread.CurrentCulture = ci;
        Thread.CurrentThread.CurrentUICulture = ci;

        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;
    }
}