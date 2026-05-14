using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

public class FrontendContainer : Form
{
    private readonly WebView2 webView = new WebView2();
    private readonly DeviceAgent deviceAgent = new DeviceAgent();

    private bool pageReady = false;
    private string pendingSymbol = null;

    public FrontendContainer()
    {
        // Kiosk Mode
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        ShowInTaskbar = false;

        Controls.Add(webView);
        webView.Dock = DockStyle.Fill;

        // Event vom DeviceAgent abonnieren (DeviceAgent bleibt unabhängig vom Frontend)
        deviceAgent.SymbolScanned += DeviceAgentOnSymbolScanned;
        deviceAgent.PrinterStatusChanged += DeviceAgentOnPrinterStatusChanged;

        Load += OnLoad;
    }

    private async void OnLoad(object sender, EventArgs e)
    {
        string html = Path.Combine(AppContext.BaseDirectory, "index.html");
        if (!File.Exists(html))
        {
            MessageBox.Show("index.html fehlt:" + html);
            Environment.Exit(1);
        }


        string userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView2",
            "KioskApp"
        );

        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
            userDataFolder: userDataFolder
        );

        await webView.EnsureCoreWebView2Async(env);

        // Der gleiche DeviceAgent wird dem JS zugänglich gemacht.
        webView.CoreWebView2.AddHostObjectToScript("deviceAgent", deviceAgent);

        webView.CoreWebView2.NavigationCompleted += (_, ev) =>
        {
            if (!ev.IsSuccess) return;
            pageReady = true;

            // Falls während dem Laden schon ein Symbol kam: nachträglich ausliefern.
            if (pendingSymbol != null)
            {
                _ = CallSymbolScannedAsync(pendingSymbol);
                pendingSymbol = null;
            }
        };

        webView.CoreWebView2.Navigate(new Uri(html).AbsoluteUri);
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
