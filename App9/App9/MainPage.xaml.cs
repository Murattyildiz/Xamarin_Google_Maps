using System;
using System.IO;
using Xamarin.Forms;

namespace App9
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        async void OnClicked(object sender, EventArgs args)
        {
            string enteredUsername = userName.Text;
            string enteredPassword = userPassword.Text;

            if (string.IsNullOrEmpty(enteredUsername) || string.IsNullOrEmpty(enteredPassword))
            {
                await DisplayAlert("Hata", "Kullanıcı adı ve şifre boş olamaz.", "Tamam");
                return;
            }

            if (AuthenticateUser(enteredUsername, enteredPassword))
            {
                await Navigation.PushAsync(new MapPage());
            }
            else
            {
                await DisplayAlert("Hata", "Kullanıcı adı veya şifre yanlış.", "Tamam");
            }
        }

        private bool AuthenticateUser(string username, string password)
        {
            try
            {
                // Dosya yolunu belirliyoruz
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "users.txt");

                // Dosya yolunu konsola yazdırıyoruz
                Console.WriteLine($"Dosya yolu: {filePath}");

                // Eğer dosya yoksa, örnek kullanıcılar ekliyoruz
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "admin:password\nuser:123456");
                }

                // Dosyayı okuyoruz
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        if (parts[0] == username && parts[1] == password)
                        {
                            return true; // Kullanıcı doğrulandı
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dosya okuma hatası: {ex.Message}");
            }

            return false; // Kullanıcı doğrulanamadı
        }

        async void OnRegisterClicked(object sender, EventArgs args)
        {
            await Navigation.PushAsync(new RegisterPage()); // Kayıt sayfasına yönlendirme
        }

    }
}