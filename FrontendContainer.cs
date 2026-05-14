using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

public class FrontendContainer : Form
{
    private readonly WebView2 webView = new WebView2();
    private readonly DeviceAgent deviceAgent = new DeviceAgent();
    private readonly string webAppUri;

    private bool pageReady = false;
    private string pendingSymbol = null;

    public FrontendContainer(string webAppUri = null)
    {

        this.webAppUri = webAppUri;
        
        // Kiosk Mode
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        ShowInTaskbar = false;

        Controls.Add(webView);
        webView.Dock = DockStyle.Fill;
        //webView.CoreWebView2.Settings.IsZoomControlEnabled = false;


        // Event vom DeviceAgent abonnieren (DeviceAgent bleibt unabhängig vom Frontend)
        deviceAgent.SymbolScanned += DeviceAgentOnSymbolScanned;
        deviceAgent.PrinterStatusChanged += DeviceAgentOnPrinterStatusChanged;

        Load += OnLoad;
    }

    private async void OnLoad(object sender, EventArgs e)
    {
        string userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView2",
            "KioskApp"
        );

        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
            userDataFolder: userDataFolder
        );

        await webView.EnsureCoreWebView2Async(env);

        // Zoom deaktivieren
        webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

        // Touch- und Scroll-Gesten blockieren
        await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
            // Alle Touch-Gesten blockieren
            document.addEventListener('touchstart', function(e) {
                e.preventDefault();
            }, { passive: false });

            document.addEventListener('touchmove', function(e) {
                e.preventDefault();
            }, { passive: false });

            document.addEventListener('touchend', function(e) {
                e.preventDefault();
            }, { passive: false });

            // Scrollen per Wheel blockieren (Maus, Trackpad)
            document.addEventListener('wheel', function(e) {
                e.preventDefault();
            }, { passive: false });

            // Pointer Events blockieren (Windows Touch API)
            document.addEventListener('pointerdown', function(e) {
                e.preventDefault();
            }, { passive: false });

            document.addEventListener('pointermove', function(e) {
                e.preventDefault();
            }, { passive: false });

            document.addEventListener('pointerup', function(e) {
                e.preventDefault();
            }, { passive: false });
        ");






        // HostObject registrieren
        webView.CoreWebView2.AddHostObjectToScript("deviceAgent", deviceAgent);

        // NavigationCompleted
        webView.CoreWebView2.NavigationCompleted += (_, ev) =>
        {
            if (!ev.IsSuccess) return;
            pageReady = true;

            if (pendingSymbol != null)
            {
                _ = CallSymbolScannedAsync(pendingSymbol);
                pendingSymbol = null;
            }
        };

        webView.CoreWebView2.Navigate(new Uri(webAppUri).AbsoluteUri);
    }


    private void DeviceAgentOnSymbolScanned(object sender, string symbol)
    {
        // ExecuteScriptAsync muss auf dem UI-Thread laufen.
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => DeviceAgentOnSymbolScanned(sender, symbol)));
            return;
        }

        if (!pageReady)
        {
            pendingSymbol = symbol;
            return;
        }

        _ = CallSymbolScannedAsync(symbol);
    }
    private async System.Threading.Tasks.Task CallSymbolScannedAsync(string symbol)
    {
        await webView.CoreWebView2.ExecuteScriptAsync($"SymbolScanned('{symbol}');");
    }




    private void DeviceAgentOnPrinterStatusChanged(object sender, string status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => DeviceAgentOnPrinterStatusChanged(sender, status)));
            return;
        }

        if (pageReady)
            RaisePrinterStatus(status);
    }
    
    private void RaiseSymbolScanned(string symbol)
    {
        var payload = new
        {
            eventName = "SymbolScanned",
            value = symbol
        };

        // .Net 8 -> string json = JsonSerializer.Serialize(payload);
        string json = "{ \"eventName\": \"SymbolScanned\", \"value\": \"" + symbol + "\" }";
        webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void RaisePrinterStatus(string status)
    {
        var payload = new
        {
            eventName = "PrinterStatus",
            value = status
        };

        // .Net 8 -> string json = JsonSerializer.Serialize(payload);
        string json = "{ \"eventName\": \"PrinterStatus\", \"value\": \"" + status + "\" }";
        webView.CoreWebView2.PostWebMessageAsJson(json);
    }

}
