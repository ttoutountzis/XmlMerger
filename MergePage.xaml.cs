using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;

namespace XmlMerger
{
    public partial class MergePage : ContentPage
    {
        private XDocument _xml1;
        private XDocument _xml2;
        private readonly IConfiguration _configuration;
        public ICommand RenameCommand { get; }

        public MergePage(List<FieldViewModel> fieldsFromXml1, List<FieldViewModel> fieldsFromXml2, XDocument xml1, XDocument xml2, IConfiguration configuration)
        {
            InitializeComponent();

            _xml1 = xml1;
            _xml2 = xml2;
            _configuration = configuration;
            SelectedFieldsListView.ItemsSource = fieldsFromXml1.Concat(fieldsFromXml2).ToList();
            BindingContext = this;
            RenameCommand = new Command<FieldViewModel>(async (field) => await ExecuteRenameCommand(field));
        }

        private async Task ExecuteRenameCommand(FieldViewModel field)
        {
            string result = await DisplayPromptAsync("Rename Field", "Enter a new name for the field:");
            if (!string.IsNullOrWhiteSpace(result))
            {
                field.NewFieldName = result;
                OnPropertyChanged(nameof(SelectedFieldsListView)); // Inform the UI to update
            }
        }

        private async void MergeAndExportXmlClicked(object sender, EventArgs e)
        {
            XDocument mergedXml = MergeXmlDocuments();

            // Export the merged XML to a file or display it
            string savePath = _configuration.GetValue<string>("SavePath");
            string path = Path.Combine(savePath, "merged.xml");
            await File.WriteAllTextAsync(path, mergedXml.ToString());

            await DisplayAlert("Exported", $"Merged XML has been exported to {path}", "OK");
        }

        private XDocument MergeXmlDocuments()
        {
            var fieldsFromXml1 = SelectedFieldsListView.ItemsSource.Cast<FieldViewModel>().Where(f => f.IsSelected && _xml1.Descendants(f.FieldName).Any()).ToList();
            var fieldsFromXml2 = SelectedFieldsListView.ItemsSource.Cast<FieldViewModel>().Where(f => f.IsSelected && _xml2.Descendants(f.FieldName).Any()).ToList();

            var mergedProducts = new XElement("products");
            foreach (var product in _xml1.Descendants("product"))
            {
                var code = product.Element("code")?.Value;
                var matchingProduct = _xml2.Descendants("product").FirstOrDefault(p => p.Element("code")?.Value == code);

                if (matchingProduct != null)
                {
                    var mergedProduct = new XElement("product");
                    foreach (var field in fieldsFromXml1)
                    {
                        var element = product.Element(field.FieldName);
                        if (element != null)
                        {
                            mergedProduct.Add(new XElement(field.NewFieldName ?? field.FieldName, element.Value));
                        }
                    }
                    foreach (var field in fieldsFromXml2)
                    {
                        var element = matchingProduct.Element(field.FieldName);
                        if (element != null)
                        {
                            mergedProduct.Add(new XElement(field.NewFieldName ?? field.FieldName, element.Value));
                        }
                    }
                    mergedProducts.Add(mergedProduct);
                }
            }

            var tempDoc = new XDocument(new XElement("data", mergedProducts));
            AdjustPricesWithVAT(tempDoc);

            return tempDoc;
        }

        void AdjustPricesWithVAT(XDocument xmlDoc)
        {
            const decimal vatRate = 0.24m; // 24% VAT

            foreach (var product in xmlDoc.Descendants("product"))
            {
                // Adjust WholeSalePriceGR
                var wholeSalePriceElement = product.Element("WholeSalePriceGR");
                if (wholeSalePriceElement != null && decimal.TryParse(wholeSalePriceElement.Value, out decimal wholeSalePrice))
                {
                    decimal priceIncludingVAT = wholeSalePrice * (1 + vatRate);
                    wholeSalePriceElement.Value = priceIncludingVAT.ToString("F2");
                }

                // Adjust PrRetailEShopPrice if greater than 0
                var prRetailEShopPriceElement = product.Element("PrRetailEShopPrice");
                if (prRetailEShopPriceElement != null && decimal.TryParse(prRetailEShopPriceElement.Value, out decimal prRetailEShopPrice) && prRetailEShopPrice > 0)
                {
                    decimal priceIncludingVAT = prRetailEShopPrice * (1 + vatRate);
                    prRetailEShopPriceElement.Value = priceIncludingVAT.ToString("F2");
                }
            }
        }

    }
}
