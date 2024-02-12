using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;

namespace XmlMerger
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
        private XDocument _xml1;
        private XDocument _xml2;
        private async void LoadXmlFromFileClicked(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    var xmlString = await File.ReadAllTextAsync(result.FullPath);
                    var fields = ParseXmlFields(xmlString);

                    if (button == LoadXml1FromFileButton)
                    {
                        _xml1 = XDocument.Parse(xmlString);
                        Xml1FieldsListView.ItemsSource = fields;
                    }
                    else if (button == LoadXml2FromFileButton)
                    {
                        _xml2 = XDocument.Parse(xmlString);
                        Xml2FieldsListView.ItemsSource = fields;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                await DisplayAlert("Error", "Failed to load XML: " + ex.Message, "OK");
            }
        }

        private async void LoadXmlFromUrlClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var urlEntry = (button == LoadXml1FromUrlButton) ? Xml1UrlEntry : Xml2UrlEntry;
            var listView = (button == LoadXml1FromUrlButton) ? Xml1FieldsListView : Xml2FieldsListView;

            var url = urlEntry.Text;
            if (string.IsNullOrWhiteSpace(url))
            {
                await DisplayAlert("Error", "Please enter a valid URL.", "OK");
                return;
            }

            try
            {
                var httpClient = new HttpClient();
                var xmlString = await httpClient.GetStringAsync(url);
                var xmlDocument = XDocument.Parse(xmlString);

                // Determine which XML document to update based on the button pressed
                if (button == LoadXml1FromUrlButton)
                {
                    _xml1 = xmlDocument;
                }
                else if (button == LoadXml2FromUrlButton)
                {
                    _xml2 = xmlDocument;
                }

                var fields = ParseXmlFields(xmlString);
                listView.ItemsSource = fields;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load XML from URL: " + ex.Message, "OK");
            }
        }

        private List<FieldViewModel> ParseXmlFields(string xmlString)
        {
            var xmlDocument = XDocument.Parse(xmlString);
            var fields = xmlDocument.Descendants("product").First().Elements()
                            .Select(element => new FieldViewModel
                            {
                                FieldName = element.Name.LocalName,
                                IsSelected = false
                            }).ToList();
            return fields;
        }

        // This event should be triggered by a button on the MainPage
        private async void NavigateToMergePageClicked(object sender, EventArgs e)
        {
            var selectedFieldsFromXml1 = Xml1FieldsListView.ItemsSource
                .Cast<FieldViewModel>()
                .Where(f => f.IsSelected)
                .ToList();

            var selectedFieldsFromXml2 = Xml2FieldsListView.ItemsSource
                .Cast<FieldViewModel>()
                .Where(f => f.IsSelected)
                .ToList();

            if (_xml1 != null && _xml2 != null)
            {
                var mergePage = new MergePage(selectedFieldsFromXml1, selectedFieldsFromXml2, _xml1, _xml2, ((App)Application.Current).Configuration);
                await Navigation.PushAsync(mergePage);
            }
            else
            {
                await DisplayAlert("Error", "Please load both XML files before merging.", "OK");
            }
        }
        
    }
}
