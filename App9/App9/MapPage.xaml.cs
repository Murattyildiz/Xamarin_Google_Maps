﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using XamarinFormsMap = Xamarin.Forms.Maps.Map;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace App9
{
    public partial class MapPage : ContentPage
    {
        private XamarinFormsMap map;
        private List<CustomPin> customPins;
        private string filePath;
        private CustomPin startPin;
        private CustomPin endPin;
        private Polyline routePolyline;
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

            map.MapClicked += OnMapClicked; // Handles map taps for adding pins

            var searchBar = new SearchBar { Placeholder = "Adres ara..." };
            searchBar.SearchButtonPressed += OnSearchButtonPressed;

            var startButton = new Button { Text = "Başlangıç Noktası Seç" };
            startButton.Clicked += OnStartButtonClicked;

            var endButton = new Button { Text = "Bitiş Noktası Seç" };
            endButton.Clicked += OnEndButtonClicked;

            var routeButton = new Button { Text = "Rota Göster" };
            routeButton.Clicked += OnRouteButtonClicked;

            Content = new StackLayout
            {
                Children = { searchBar, startButton, endButton, routeButton, map }
            };

            var position = new Position(41.0082, 28.9784); // İstanbul
            map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(10)));
        }

        private async void OnSearchButtonPressed(object sender, EventArgs e)
        {
            var searchBar = (SearchBar)sender;
            var address = searchBar.Text;
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
            }
            else
            {
                await DisplayAlert("Hata", "Adres bulunamadı", "Tamam");
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

            await ShowRoute();
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

        private async Task ShowRoute()
        {
            if (startPin == null || endPin == null)
            {
                await DisplayAlert("Hata", "Lütfen başlangıç ve bitiş noktalarını seçin", "Tamam");
                return;
            }

            // Google Maps Directions API URL oluşturma
            string apiKey = "AIzaSyD83kkqqUivpMZNt5xcxISWZNFDhVKO1vI"; // Google Maps API anahtarınızı buraya ekleyin
            string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={startPin.Position.Latitude},{startPin.Position.Longitude}&destination={endPin.Position.Latitude},{endPin.Position.Longitude}&key={apiKey}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(url);
                    var routeData = JObject.Parse(response);

                    if (routeData["status"].ToString() == "OK")
                    {
                        var points = routeData["routes"][0]["overview_polyline"]["points"].ToString();
                        var positions = DecodePolyline(points);

                        if (routePolyline != null)
                        {
                            map.MapElements.Remove(routePolyline);
                        }

                        routePolyline = new Polyline
                        {
                            StrokeColor = Color.Blue,
                            StrokeWidth = 3
                        };

                        foreach (var position in positions)
                        {
                            routePolyline.Geopath.Add(new Position(position.Latitude, position.Longitude));
                        }

                        map.MapElements.Add(routePolyline);
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Rota alınamadı", "Tamam");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Rota alınırken bir hata oluştu: {ex.Message}", "Tamam");
            }
        }


        // Polyline'ı çözmek için DecodePolyline methodu
        public List<Position> DecodePolyline(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints)) return null;

            var poly = new List<Position>();
            char[] polylineChars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;

            while (index < polylineChars.Length)
            {
                // Latitude decode
                int sum = 0;
                int shifter = 0;
                int next5Bits;
                do
                {
                    next5Bits = polylineChars[index++] - 63;
                    sum |= (next5Bits & 31) << shifter;
                    shifter += 5;
                } while (next5Bits >= 32 && index < polylineChars.Length);

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                // Longitude decode
                sum = 0;
                shifter = 0;
                do
                {
                    next5Bits = polylineChars[index++] - 63;
                    sum |= (next5Bits & 31) << shifter;
                    shifter += 5;
                } while (next5Bits >= 32 && index < polylineChars.Length);

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                var position = new Position(Convert.ToDouble(currentLat) / 1E5, Convert.ToDouble(currentLng) / 1E5);
                poly.Add(position);
            }

            return poly;
        }


        private void SavePinToFile(CustomPin pin)
        {
            string pinData = $"{pin.Position.Latitude},{pin.Position.Longitude},{pin.Label}\n";
            File.AppendAllText(filePath, pinData);
        }

        private void LoadPinsFromFile()
        {
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        double latitude = double.Parse(parts[0]);
                        double longitude = double.Parse(parts[1]);
                        string label = parts[2];

                        var pin = new CustomPin
                        {
                            Position = new Position(latitude, longitude),
                            Label = label,
                            Address = "Özel Konum",
                            Type = PinType.Generic
                        };

                        map.Pins.Add(pin);
                        customPins.Add(pin);
                    }
                }
            }
        }
    }

    public class CustomPin : Pin
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public Color PinColor { get; set; }
    }
}