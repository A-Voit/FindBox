using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace FindBox.ScrapeFunctions
{
    public class Panoramic
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public List<Product> Products;

        public event Action<int> ScrapingCompleted;

        public event Action<int> ScrapingUpdated;

        private int _parsedProductCount = 0;

        public Panoramic()
        {
            Products = new List<Product>();
        }

        public async Task ScrapeCatalog()
        {
            int totalPages = 31;
            string baseUrl = "https://www.panoramicinc.com/shop/page/";

            List<Task> tasks = new List<Task>();

            for (int page = 1; page <= totalPages; page++)
            {
                tasks.Add(ProcessPageAsync(baseUrl + page));
            }

            await Task.WhenAll(tasks);

            ScrapingCompleted?.Invoke(_parsedProductCount);
        }

     
        public async Task ProcessPageAsync(string url)
        {
            string pageHtml = await httpClient.GetStringAsync(url);
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(pageHtml);

            var productNodes = htmlDoc.DocumentNode.SelectNodes("//li[contains(@class, 'product')]");

            List<Task> productTasks = new List<Task>();

            foreach (var node in productNodes)
            {
                var nameNode = node.SelectSingleNode(".//h2[@class='woocommerce-loop-product__title']/a");
                var skuNode = node.SelectSingleNode(".//div[@class='product-meta-sku']");
                var linkNode = node.SelectSingleNode(".//a[contains(@class, 'woocommerce-LoopProduct-link')]");

                var product = new Product
                {
                    Name = nameNode?.InnerText.Trim(),
                    SKU = skuNode?.InnerText.Replace("SKU: ", "").Trim(),
                    Link = linkNode?.GetAttributeValue("href", "").Trim(),
                    Store = "Panoramic"
                };

                Products.Add(product);

                productTasks.Add(ProcessProductPageAsync(product.Link, product));
            }

            await Task.WhenAll(productTasks);
            
        }

        private async Task ProcessProductPageAsync(string url, Product product)
        {
            string productHtml = await httpClient.GetStringAsync(url);
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(productHtml);

            // Parse the product detail page
            var shapeNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='part_dimension'][.//div[@class='custom-label'][text()='Shape']]/div[@class='custom-value']");
            var capacityNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='part_dimension'][.//div[@class='custom-label'][text()='Capacity']]/div[@class='custom-value']");
            var cellsNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='part_dimension'][.//div[@class='custom-label'][text()='Cells']]/div[@class='custom-value']");
            var imgNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-thumb]");

            // Output or process the product information
            product.Shape = shapeNode.InnerText.Trim();
            product.Capacity = capacityNode.InnerText.Trim();
            product.Compartments = cellsNode.InnerText.Trim();
            product.ImageURL = imgNode.GetAttributeValue("data-thumb", string.Empty);

            // Update the product count and raise the event
            _parsedProductCount++;
            ScrapingUpdated?.Invoke(_parsedProductCount);
        }

        public void NormalizeCapacities()
        {
            foreach(Product pdt in Products)
            {
                pdt.Capacity = Form1.NormalizeCapacity(pdt.Capacity);
            }
        }
    }
}
