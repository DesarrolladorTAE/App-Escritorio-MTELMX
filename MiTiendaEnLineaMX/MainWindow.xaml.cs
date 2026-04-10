using Microsoft.Web.WebView2.Core;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace MiTiendaEnLineaMX
{
    public partial class MainWindow : Window
    {
        private readonly PrinterService _printerService = new PrinterService();
        private bool _configVisible = false;

        private const string STORE_URL = "https://mitiendaenlineamx.com.mx/login-register";
        private const string POS_URL = "https://mitiendaenlineamx.com.mx/prueba/pos";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HideConfigPanel();
            UpdateModePanels();

            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiTiendaEnLineaMX",
                    "WebView2"
                );

                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                await webView.EnsureCoreWebView2Async(env);

                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                    webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                }

                AppendLog("WebView2 inicializado correctamente.");
            }
            catch (Exception ex)
            {
                AppendLog("Error inicializando WebView2: " + ex.Message);
                MessageBox.Show("Error al abrir el sitio: " + ex.Message);
            }
        }

        private void btnToggleConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_configVisible)
                HideConfigPanel();
            else
                ShowConfigPanel();
        }

        private void ShowConfigPanel()
        {
            _configVisible = true;
            configPanel.Visibility = Visibility.Visible;
            colConfig.Width = new GridLength(360);
        }

        private void HideConfigPanel()
        {
            _configVisible = false;
            configPanel.Visibility = Visibility.Collapsed;
            colConfig.Width = new GridLength(0);
        }

        private void cmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateModePanels();
        }

        private void UpdateModePanels()
        {
            if (cmbMode == null || panelTcp == null || panelUsb == null)
                return;

            bool isTcp = cmbMode.SelectedIndex == 0;
            panelTcp.Visibility = isTcp ? Visibility.Visible : Visibility.Collapsed;
            panelUsb.Visibility = isTcp ? Visibility.Collapsed : Visibility.Visible;
        }

        private PaperSize ReadPaperSizeFromUi()
        {
            return cmbPaper.SelectedIndex == 1
                ? PaperSize.Mm58
                : PaperSize.Mm80;
        }

        private PrinterMode ReadPrinterModeFromUi()
        {
            return cmbMode.SelectedIndex == 1
                ? PrinterMode.Usb
                : PrinterMode.Tcp;
        }

        private PrinterConfig ReadConfigFromUi()
        {
            return new PrinterConfig
            {
                Mode = ReadPrinterModeFromUi(),
                PaperSize = ReadPaperSizeFromUi(),
                Cut = chkCut.IsChecked == true,
                OpenDrawer = chkDrawer.IsChecked == true,

                TcpHost = txtIP.Text?.Trim() ?? "",
                TcpPort = int.TryParse(txtPort.Text?.Trim(), out int port) ? port : 9100,

                PrinterName = txtPrinterName.Text?.Trim() ?? "",

                LogoBase64 = txtLogoBase64.Text?.Trim() ?? ""
            };
        }

        private void ApplyConfigToUi(PrinterConfig config)
        {
            cmbMode.SelectedIndex = config.Mode == PrinterMode.Usb ? 1 : 0;

            cmbPaper.SelectedIndex = config.PaperSize == PaperSize.Mm58 ? 1 : 0;

            chkCut.IsChecked = config.Cut;
            chkDrawer.IsChecked = config.OpenDrawer;

            txtIP.Text = config.TcpHost ?? "";
            txtPort.Text = config.TcpPort > 0 ? config.TcpPort.ToString() : "9100";

            txtPrinterName.Text = config.PrinterName ?? "";
            txtLogoBase64.Text = config.LogoBase64 ?? "";

            UpdateModePanels();

            if (!string.IsNullOrWhiteSpace(config.PrinterName))
            {
                bool exists = false;
                foreach (var item in cmbPrinters.Items)
                {
                    if (string.Equals(item?.ToString(), config.PrinterName, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbPrinters.SelectedItem = item;
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    cmbPrinters.Items.Add(config.PrinterName);
                    cmbPrinters.SelectedItem = config.PrinterName;
                }
            }
        }

        private void ValidateConfig(PrinterConfig config)
        {
            if (config.Mode == PrinterMode.Tcp)
            {
                if (string.IsNullOrWhiteSpace(config.TcpHost))
                    throw new Exception("Ingresa la IP de la impresora.");

                if (config.TcpPort <= 0)
                    throw new Exception("Puerto inválido.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(config.PrinterName))
                    throw new Exception("Selecciona una impresora USB.");
            }
        }

        private void LoadPrinters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                cmbPrinters.Items.Clear();

                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printer);
                }

                if (cmbPrinters.Items.Count > 0)
                {
                    cmbPrinters.SelectedIndex = 0;
                    txtPrinterName.Text = cmbPrinters.SelectedItem?.ToString() ?? "";
                    AppendLog("Impresoras USB cargadas correctamente.");
                }
                else
                {
                    AppendLog("No se encontraron impresoras instaladas.");
                    MessageBox.Show("No se encontraron impresoras instaladas.");
                }
            }
            catch (Exception ex)
            {
                AppendLog("Error al cargar impresoras: " + ex.Message);
                MessageBox.Show("Error al cargar impresoras: " + ex.Message);
            }
        }

        private void cmbPrinters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPrinters.SelectedItem != null)
                txtPrinterName.Text = cmbPrinters.SelectedItem.ToString() ?? "";
        }

        private void UseSelectedPrinter_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPrinters.SelectedItem != null)
            {
                txtPrinterName.Text = cmbPrinters.SelectedItem.ToString() ?? "";
                AppendLog("Impresora USB seleccionada: " + txtPrinterName.Text);
            }
        }

        private async void OpenStore_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToAsync(STORE_URL);
        }

        private async void OpenPos_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToAsync(POS_URL);
        }

        private async System.Threading.Tasks.Task NavigateToAsync(string url)
        {
            try
            {
                if (webView.CoreWebView2 == null)
                    await webView.EnsureCoreWebView2Async();

                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Navigate(url);
                    AppendLog("Navegando a: " + url);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir el sitio: " + ex.Message);
            }
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ReadConfigFromUi();
                ValidateConfig(config);

                byte[] bytes = _printerService.BuildDemoTicket(
                    config.PaperSize,
                    config.Cut,
                    config.OpenDrawer,
                    "https://mitiendaenlineamx.com.mx",
                    config.LogoBase64
                );

                await _printerService.PrintAsync(config, bytes);

                AppendLog($"Ticket de prueba enviado correctamente. Modo={config.Mode}, Papel={config.PaperSize}");
                MessageBox.Show("Ticket enviado correctamente.");
            }
            catch (Exception ex)
            {
                AppendLog("Error al imprimir prueba: " + ex.Message);
                MessageBox.Show("Error al imprimir: " + ex.Message);
            }
        }

        private async void TestTcp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ReadConfigFromUi();

                if (config.Mode != PrinterMode.Tcp)
                {
                    MessageBox.Show("Cambia el modo a TCP/IP para probar conexión.");
                    return;
                }

                ValidateConfig(config);

                using TcpClient client = new TcpClient();
                await client.ConnectAsync(config.TcpHost, config.TcpPort);

                AppendLog($"Conexión TCP exitosa a {config.TcpHost}:{config.TcpPort}");
                MessageBox.Show("Conexión TCP exitosa.");
            }
            catch (Exception ex)
            {
                AppendLog("No se pudo conectar por TCP: " + ex.Message);
                MessageBox.Show("No se pudo conectar: " + ex.Message);
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ReadConfigFromUi();

                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MiTiendaEnLineaMX");

                Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, "printer-config.json");

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json, Encoding.UTF8);

                AppendLog("Configuración guardada.");
                MessageBox.Show("Configuración guardada correctamente.");
            }
            catch (Exception ex)
            {
                AppendLog("Error al guardar configuración: " + ex.Message);
                MessageBox.Show("Error al guardar configuración: " + ex.Message);
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MiTiendaEnLineaMX");

                string filePath = Path.Combine(folder, "printer-config.json");

                if (!File.Exists(filePath))
                {
                    MessageBox.Show("No existe una configuración guardada.");
                    return;
                }

                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<PrinterConfig>(json);

                if (config == null)
                {
                    MessageBox.Show("No se pudo leer la configuración.");
                    return;
                }

                ApplyConfigToUi(config);

                AppendLog("Configuración cargada.");
                MessageBox.Show("Configuración cargada correctamente.");
            }
            catch (Exception ex)
            {
                AppendLog("Error al cargar configuración: " + ex.Message);
                MessageBox.Show("Error al cargar configuración: " + ex.Message);
            }
        }

        private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (webView.CoreWebView2 == null) return;

                string bridgeScript = @"
(function () {
    if (window.__mtelm_bridge_installed) return;
    window.__mtelm_bridge_installed = true;

    window.sendPrintPayloadToWindows = function (payload) {
        try {
            chrome.webview.postMessage(JSON.stringify(payload));
            return true;
        } catch (e) {
            console.error('Error enviando payload a Windows:', e);
            return false;
        }
    };
})();
";
                await webView.ExecuteScriptAsync(bridgeScript);
                AppendLog("Bridge JS inyectado.");
            }
            catch (Exception ex)
            {
                AppendLog("Error inyectando bridge JS: " + ex.Message);
            }
        }

        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string raw = e.TryGetWebMessageAsString();

                AppendLog("Mensaje recibido del WebView:");
                AppendLog(raw);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                PrintPayload? payload = null;

                try
                {
                    payload = JsonSerializer.Deserialize<PrintPayload>(raw, options);
                }
                catch
                {
                    AppendLog("No se pudo parsear directo, intentando como envelope...");
                }

                if (payload == null)
                {
                    try
                    {
                        var wrapper = JsonSerializer.Deserialize<PayloadWrapper>(raw, options);
                        payload = wrapper?.Payload;
                    }
                    catch
                    {
                        AppendLog("No se pudo parsear como wrapper.");
                    }
                }

                if (payload == null)
                {
                    AppendLog("Payload inválido.");
                    return;
                }

                var localConfig = ReadConfigFromUi();

                if (string.IsNullOrWhiteSpace(payload.Logo) && !string.IsNullOrWhiteSpace(localConfig.LogoBase64))
                    payload.Logo = localConfig.LogoBase64;

                // La app manda siempre sobre el payload del backend
                payload.PaperSize = localConfig.PaperSize == PaperSize.Mm58 ? "58" : "80";
                payload.Cut = localConfig.Cut;
                payload.OpenDrawer = localConfig.OpenDrawer;

                byte[] bytes = _printerService.BuildTicketFromPayload(payload);

                ValidateConfig(localConfig);
                await _printerService.PrintAsync(localConfig, bytes);

                AppendLog($"Impresión realizada correctamente. Modo={localConfig.Mode}, Papel={localConfig.PaperSize}");
            }
            catch (Exception ex)
            {
                AppendLog("Error procesando impresión: " + ex.Message);
            }
        }

        private void AppendLog(string message)
        {
            if (txtLog == null) return;

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToEnd();
        }

        private class PayloadWrapper
        {
            public bool Ok { get; set; }
            public PrintPayload? Payload { get; set; }
        }
    }
}
