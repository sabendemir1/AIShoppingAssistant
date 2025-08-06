using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PricePilot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set full width to screen
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Top = 0;
            this.Left = 0;
        }

        private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            var screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            var screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

            string filename;
            using (var bmp = new Bitmap(screenWidth, screenHeight))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }

                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                filename = System.IO.Path.Combine(desktop, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                bmp.Save(filename, ImageFormat.Png);
            }

            System.Windows.MessageBox.Show("Screenshot saved to Desktop.", "Screenshot", MessageBoxButton.OK, MessageBoxImage.Information);

            // Send the screenshot to the server
            var client = new HttpClient();
            var imageBytes = File.ReadAllBytes(filename);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "file", System.IO.Path.GetFileName(filename));

            var response = await client.PostAsync("http://localhost:8000/analyze", content);
            string responseString = await response.Content.ReadAsStringAsync();
            //System.Windows.MessageBox.Show(responseString, "Server Response", MessageBoxButton.OK, MessageBoxImage.Information);
            //System.Diagnostics.Debug.WriteLine(responseString); // Use Debug.WriteLine for WPF apps
            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);
                JsonElement root = doc.RootElement;

                string rawText = root.GetProperty("raw_text").GetString();
                string extractedInfoRaw = root.GetProperty("extracted_product_info").ToString();

                string formattedProductInfo = "";

                try
                {
                    using JsonDocument extractedDoc = JsonDocument.Parse(extractedInfoRaw);
                    JsonElement extractedRoot = extractedDoc.RootElement;

                    if (extractedRoot.ValueKind == JsonValueKind.Array)
                    {
                        int idx = 1;
                        foreach (var product in extractedRoot.EnumerateArray())
                        {
                            formattedProductInfo += $"Product {idx}:\n";
                            foreach (var property in product.EnumerateObject())
                            {
                                formattedProductInfo += $"  {property.Name}: {property.Value}\n";
                            }
                            formattedProductInfo += "\n";
                            idx++;
                        }
                    }
                    else if (extractedRoot.ValueKind == JsonValueKind.Object)
                    {
                        formattedProductInfo += "Product:\n";
                        foreach (var property in extractedRoot.EnumerateObject())
                        {
                            formattedProductInfo += $"  {property.Name}: {property.Value}\n";
                        }
                    }
                    else
                    {
                        formattedProductInfo = extractedInfoRaw;
                    }
                }
                catch
                {
                    // If parsing fails, treat as plain string
                    formattedProductInfo = extractedInfoRaw;
                }

                System.Windows.MessageBox.Show(
                    $"🧾 OCR Raw Text:\n{rawText}\n\n📦 Product Info:\n{formattedProductInfo}",
                    "PricePilot Result",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error parsing server response:\n{ex.Message}\n\nRaw:\n{responseString}",
                    "Parse Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}