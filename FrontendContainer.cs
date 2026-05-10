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

        await webView.EnsureCoreWebView2Async();

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

}
