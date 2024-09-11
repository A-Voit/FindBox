using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;

namespace FindBox.ScrapeFunctions
{
    public class Lacerta
    {
        
        private string webURL = "https://lacerta.com/catalog";

        public List<Product> Products;

        public event Action<int> ScrapingCompleted;

        public Lacerta()
        {
            Products = new List<Product>();
        }

        public async void ScrapeCatalog()
        {
            using HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(webURL);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // Select all product divs
            Products = doc.DocumentNode
                .SelectNodes("//div[@class='single-product']")
                .Select(node => new Product
                {
                    Name = node.SelectSingleNode(".//h4[@class='main-heading']")?.InnerText.Trim(),
                    Shape = node.GetAttributeValue("data-shape", string.Empty),
                    Capacity = node.GetAttributeValue("data-capacity", string.Empty).Replace(".", string.Empty),
                    Compartments = node.GetAttributeValue("data-compartments", string.Empty),
                    ImageURL = node.SelectSingleNode(".//img")?.GetAttributeValue("src", string.Empty),
                    Link = "https://lacerta.com/" + node.SelectSingleNode(".//a[@class='whole-product-link']")?.GetAttributeValue("href", string.Empty),
                    Store = "Lacerta"
                })
                .ToList();

            ScrapingCompleted?.Invoke(Products.Count);
        }

        public void NormalizeCapacities()
        {
            foreach (Product pdt in Products)
            {
                pdt.Capacity = Form1.NormalizeCapacity(pdt.Capacity);
            }
        }
    }


}
