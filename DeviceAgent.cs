using System;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class DeviceAgent
{
    public event EventHandler<string> SymbolScanned;
    private readonly Timer scanTimer;
    private static readonly char[] SymbolChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    private readonly Random rnd = new Random();

    public DeviceAgent()
    {
        scanTimer = new Timer(_ => RaiseRandomSymbol(), null, dueTime: 2000, period: 2000);
    }
    private void RaiseRandomSymbol()
    {
        string s = "PACT" + RandomAlphaNumericUpper(12);
        SymbolScanned?.Invoke(this, s);
    }
    private string RandomAlphaNumericUpper(int length)
    {
        char[] buf = new char[length];
        lock (rnd)
        {
            for (int i = 0; i < length; i++)
            {
                buf[i] = SymbolChars[rnd.Next(SymbolChars.Length)];
            }
        }
        return new string(buf);
    }
    public enum PrintStatus { ok, error }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ModuleConfiguration
    {
        public string computerName { get; set; }
        public string printers { get; set; }
        public string devices { get; set; }
    }
    public PrintStatus Print(string zplCode)
    {
    if (string.IsNullOrEmpty(zplCode))
            return PrintStatus.error;
        return PrintStatus.ok;
    }
    public ModuleConfiguration GetModuleConfiguration()
    {
        return new ModuleConfiguration
        {
            computerName = Environment.MachineName,
            printers = GetInstalledPrintersAsString(),
            devices = GetDevicesAsString()
        };
    }
    private static string GetInstalledPrintersAsString()
    {
        try
        {
            var installedPrinters = PrinterSettings.InstalledPrinters
                .Cast<string>()
                .ToArray();

            return (installedPrinters.Length > 0)
                ? string.Join("; ", installedPrinters)
                : "";
        }
        catch
        {
            return "";
        }
    }
    private static string GetDevicesAsString()
    {
            return "TODO";
    }
}