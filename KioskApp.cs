using System;
using System.Windows.Forms;
using System.IO;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string webAppUri = null;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args.Length == 0) {
            MessageBox.Show("Es muss eine URI zum Webapp-Starten übergeben werden (z.B. index.html oder https://example.com) oder file://C:/index.html");
            Environment.Exit(1);
        }

        if (Uri.IsWellFormedUriString(args[0], UriKind.Absolute))
        {
            webAppUri = args[0];
        } else if (Uri.IsWellFormedUriString(args[0], UriKind.Relative))
         {
             webAppUri = Path.Combine(AppContext.BaseDirectory, args[0]);
            if (!File.Exists(webAppUri))
            {
                MessageBox.Show("Datei fehlt: " + webAppUri);
                Environment.Exit(1);
            }
         } else{
             MessageBox.Show("Ungültige URI: " + args[0]);
             Environment.Exit(1);
         }
        Application.Run(new FrontendContainer(webAppUri));
    }
}