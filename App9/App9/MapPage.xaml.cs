using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using XamarinFormsMap = Xamarin.Forms.Maps.Map;

namespace App9
{
    public partial class MapPage : ContentPage
    {
        private XamarinFormsMap map;
        private List<CustomPin> customPins;
        private string filePath;
        private CustomPin startPin;
        private CustomPin endPin;
        private bool isSelectingStart = false;
        private bool isSelectingEnd = false;

        public MapPage()
        {
            InitializeComponent();
            customPins = new List<CustomPin>();
            filePath = Path.Combine(FileSystem.AppDataDirectory, "pins.txt");
            InitializeMap();
            LoadPinsFromFile();
        }

        private void InitializeMap()
        {
            map = new XamarinFormsMap
            {
                MapType = MapType.Street,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            map.MapClicked += OnMapClicked;

            var searchBar = new SearchBar { Placeholder = "Adres ara..." };
            searchBar.SearchButtonPressed += OnSearchButtonPressed;

            var startButton = new Button { Text = "Başlangıç Noktası Seç" };
            startButton.Clicked += OnStartButtonClicked;

            var endButton = new Button { Text = "Bitiş Noktası Seç" };
            endButton.Clicked += OnEndButtonClicked;

            var routeButton = new Button { Text = "Rota Göster" };
            routeButton.Clicked += OnRouteButtonClicked;

            var listPinsButton = new Button { Text = "İşaretli Noktaları Göster" };
            listPinsButton.Clicked += OnListPinsButtonClicked;

            Content = new StackLayout
            {
                Children = { searchBar, startButton, endButton, routeButton, listPinsButton, map }
            };

            var position = new Position(41.0082, 28.9784); // Istanbul
            map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(10)));
        }

        private async void OnListPinsButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PinsListPage(customPins, this));
        }

        private async void OnSearchButtonPressed(object sender, EventArgs e)
        {
            var searchBar = (SearchBar)sender;
            var address = searchBar.Text;

            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("Hata", "İnternet bağlantınız yok.", "Tamam");
                return;
            }

            try
            {
                var locations = await Geocoding.GetLocationsAsync(address);
                var location = locations?.FirstOrDefault();
                if (location != null)
                {
                    var position = new Position(location.Latitude, location.Longitude);
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(1)));

                    var pin = new CustomPin
                    {
                        Position = position,
                        Label = address,
                        Address = address,
                        Type = PinType.SearchResult
                    };

                    map.Pins.Add(pin);
                    customPins.Add(pin);
                    SavePinToFile(pin);
                }
                else
                {
                    await DisplayAlert("Hata", "Adres bulunamadı", "Tamam");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                await DisplayAlert("Hata", "Adres aramada zaman aşımı hatası oluştu. Lütfen tekrar deneyin.", "Tamam");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Adres aramada hata oluştu: {ex.Message}", "Tamam");
            }
        }

        private void OnStartButtonClicked(object sender, EventArgs e)
        {
            isSelectingStart = true;
            isSelectingEnd = false;
            DisplayAlert("Bilgi", "Haritada başlangıç noktasını seçin", "Tamam");
        }

        private void OnEndButtonClicked(object sender, EventArgs e)
        {
            isSelectingStart = false;
            isSelectingEnd = true;
            DisplayAlert("Bilgi", "Haritada bitiş noktasını seçin", "Tamam");
        }

        private async void OnRouteButtonClicked(object sender, EventArgs e)
        {
            if (startPin == null || endPin == null)
            {
                await DisplayAlert("Hata", "Lütfen başlangıç ve bitiş noktalarını seçin", "Tamam");
                return;
            }
            // await ShowRoute();
        }

        private async void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            var position = e.Position;

            if (isSelectingStart)
            {
                if (startPin != null)
                {
                    map.Pins.Remove(startPin);
                }
                startPin = new CustomPin
                {
                    Position = position,
                    Label = "Başlangıç",
                    Type = PinType.Generic,
                    PinColor = Color.Green
                };
                map.Pins.Add(startPin);
                isSelectingStart = false;
                await DisplayAlert("Bilgi", "Başlangıç noktası seçildi", "Tamam");
            }
            else if (isSelectingEnd)
            {
                if (endPin != null)
                {
                    map.Pins.Remove(endPin);
                }
                endPin = new CustomPin
                {
                    Position = position,
                    Label = "Bitiş",
                    Type = PinType.Generic,
                    PinColor = Color.Red
                };
                map.Pins.Add(endPin);
                isSelectingEnd = false;
                await DisplayAlert("Bilgi", "Bitiş noktası seçildi", "Tamam");
            }
            else
            {
                string pinLabel = await DisplayPromptAsync("Yeni İşaret", "İşaret için bir etiket girin:");
                if (!string.IsNullOrWhiteSpace(pinLabel))
                {
                    var customPin = new CustomPin
                    {
                        Position = position,
                        Label = pinLabel,
                        Address = "Özel Konum",
                        Type = PinType.Generic
                    };

                    map.Pins.Add(customPin);
                    customPins.Add(customPin);
                    SavePinToFile(customPin);
                }
            }
        }

        private void LoadPinsFromFile()
        {
            if (!File.Exists(filePath)) return;

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(',');
                if (parts.Length == 3 &&
                    double.TryParse(parts[0], out double lat) &&
                    double.TryParse(parts[1], out double lng))
                {
                    var customPin = new CustomPin
                    {
                        Position = new Position(lat, lng),
                        Label = parts[2],
                        Type = PinType.Place
                    };
                    map.Pins.Add(customPin);
                    customPins.Add(customPin);
                }
            }
        }

        private void SavePinToFile(CustomPin customPin)
        {
            var line = $"{customPin.Position.Latitude},{customPin.Position.Longitude},{customPin.Label}";
            File.AppendAllText(filePath, line + Environment.NewLine);
        }

        // RemovePinFromMap metodu
        public void RemovePinFromMap(CustomPin pin)
        {
            map.Pins.Remove(pin);
            customPins.Remove(pin);
        }
    }

    public class CustomPin : Pin
    {
        public Color PinColor { get; set; }
    }
}
