using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;
using FindBox.ScrapeFunctions;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FindBox
{
    public partial class Form1 : Form
    {
        private Lacerta lacerta;
        private Panoramic panoramic;

        private int counter;
        private int MaxValue; // Example max value for the progress bar

        public Form1()
        {
            InitializeComponent();

            // Set PictureBox properties
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            // Setup lacerta
            infoBox.AppendText($"Product parsing beginning, please wait...");
            lacerta = new Lacerta();
            panoramic = new Panoramic();

            lacerta.ScrapingCompleted += Lacerta_ScrapingCompleted;
            panoramic.ScrapingCompleted += Panoramic_ScrapingCompleted;
            panoramic.ScrapingUpdated += Panoramic_ScrapingUpdated;

            lacerta.ScrapeCatalog();
            panoramic.ScrapeCatalog();
            shapeCombo.SelectedIndex = 0;

            storeCombo.SelectedIndex = 0;
        }

        private void Panoramic_ScrapingUpdated(int obj)
        {
            infoBox.AppendText($"\r\nPanoramic parsed {obj} products.");
        }

        private void Panoramic_ScrapingCompleted(int obj)
        {
            infoBox.AppendText($"\r\nPanoramic Parsing Complete!\r\nParsed {obj} products.");
            panoramic.NormalizeCapacities();
            PopulateComboBoxWithCapacity();
        }

        private void Lacerta_ScrapingCompleted(int productCount)
        {
            // Show MessageBox when scraping is complete
            infoBox.AppendText($"\r\nLacerta Parsing Complete.\r\nParsed {productCount} products.");
            lacerta.NormalizeCapacities();
            PopulateComboBoxWithCapacity();
        }


        private async void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedProduct = listBoxProducts.SelectedItem as Product;

            // Populate the item
            ShowProductInfo(true);

            nameLabel.Text = selectedProduct.Name;
            shapeLabel.Text = selectedProduct.Shape;           
            capacityLabel.Text = selectedProduct.Capacity;           
            compartmentLabel.Text = selectedProduct.Compartments;           
            linkLabel.Text = selectedProduct.Link;
            pictureBox.ImageLocation = selectedProduct.ImageURL;
        }

        private void scrapeButton_Click(object sender, EventArgs e)
        {
            listBoxProducts.Refresh();

            List<Product> filteredProducts = new List<Product>();

            string storeText = storeCombo.SelectedItem as string;

            if(storeText.ToLower() == "any")
            {
                filteredProducts.AddRange(lacerta.Products);
                filteredProducts.AddRange(panoramic.Products);
            }else if(storeText.ToLower() == "lacerta")
            {
                filteredProducts.AddRange(lacerta.Products);
            }
            else if(storeText.ToLower() == "panoramic")
            {
                filteredProducts.AddRange(panoramic.Products);
            }

            ShowProductInfo(false);

            string shapeText = shapeCombo.SelectedItem as string;

            if(shapeText.ToLower() != "any")
            {
                filteredProducts = filteredProducts.Where(p => p.Shape.Equals(shapeText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            string capacityText = cboCapacity.SelectedItem as string;

            if(capacityText.ToLower() != "any")
            {
                filteredProducts = filteredProducts.Where(p => p.Capacity.Equals(capacityText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Add the filtered list to the listbox
            listBoxProducts.DataSource = filteredProducts;
            listBoxProducts.DisplayMember = "Name";
        }

        private void ShowProductInfo(bool show)
        {
            nameLabel.Visible = show;
            shapeLabel.Visible = show;
            capacityLabel.Visible = show;
            compartmentLabel.Visible = show;
            linkLabel.Visible = show;
            pictureBox.Visible = show;
        }

        private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = linkLabel.Text,
                UseShellExecute = true // Ensures the URL opens in the default browser
            });
        }

        private void PopulateComboBoxWithCapacity()
        {
            // Clear existing items
            cboCapacity.Items.Clear();

            cboCapacity.Items.Add("Any");

            List<Product> allProducts = new List<Product>();

            allProducts.AddRange(lacerta.Products);
            allProducts.AddRange(panoramic.Products);

            // Extract capacity values and add them to the ComboBox
            var capacities = allProducts.Select(p => p.Capacity).Distinct().OrderBy(c => GetCapacityValue(c)).ToList();

            var normalizedCapacities = capacities
                .Select(NormalizeCapacity)
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => GetCapacityValue(c))
                .ToList();


            foreach (var capacity in capacities)
            {
                cboCapacity.Items.Add(capacity);
            }

            // Optionally, set a default selected item
            if (cboCapacity.Items.Count > 0)
            {
                cboCapacity.SelectedIndex = 0;
            }
        }

        public static string NormalizeCapacity(string capacity)
        {
            // Remove extra whitespace and convert to lowercase
            string cleanedCapacity = capacity.Trim().ToLower();

            // Replace multiple spaces with a single space
            cleanedCapacity = Regex.Replace(cleanedCapacity, @"\s+", " ");

            // Convert common synonyms and abbreviations to standard "oz"
            cleanedCapacity = cleanedCapacity.Replace("ounces", "oz").Replace("oz", " oz");

            // Standardize format to "X oz" and ignore text if not related to ounces
            Match match = Regex.Match(cleanedCapacity, @"(\d+)\s*(oz|ounces)?", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return $"{match.Groups[1].Value} oz";
            }

            return string.Empty; // Return an empty string if not related to ounces
        }

        static double GetCapacityValue(string capacity)
        {
            // Extract numeric value from the capacity string for sorting
            Match match = Regex.Match(capacity, @"(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? double.Parse(match.Groups[1].Value) : double.MaxValue;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
