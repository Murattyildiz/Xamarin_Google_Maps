using System;
using System.IO;
using Xamarin.Forms;

namespace App9
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string username = registerUserName.Text;
            string password = registerUserPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Hata", "Kullanıcı adı ve şifre boş olamaz.", "Tamam");
                return;
            }

            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "users.txt");
            string userEntry = $"{username}:{password}\n";

            // Kullanıcıyı dosyaya ekleme
            File.AppendAllText(filePath, userEntry);
            await DisplayAlert("Başarılı", "Kayıt işlemi başarılı.", "Tamam");

            // Ana sayfaya dön
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Geri git
        }
    }
}
