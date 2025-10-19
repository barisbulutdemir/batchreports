using System;
using System.Windows;

namespace takip
{
    /// <summary>
    /// Üretim notu ekleme dialog'u
    /// </summary>
    public partial class AddProductionNoteDialog : Window
    {
        public string NoteText { get; private set; } = "";
        public int FireProductCount { get; private set; } = 0;
        
        public AddProductionNoteDialog()
        {
            InitializeComponent();
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NoteText = NoteTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(NoteText))
                {
                    MessageBox.Show("Lütfen bir not girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                FireProductCount = 0; // Fire ürün artık PLC'den gelecek
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
