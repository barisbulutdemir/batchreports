using System;
using System.Windows;
using System.Windows.Controls;

namespace takip
{
    public partial class CementSiloControl : UserControl
    {
        public static readonly DependencyProperty FillPercentProperty =
            DependencyProperty.Register(nameof(FillPercent), typeof(double), typeof(CementSiloControl),
                new PropertyMetadata(50.0, OnFillPercentChanged));

        public double FillPercent
        {
            get => (double)GetValue(FillPercentProperty);
            set => SetValue(FillPercentProperty, value);
        }

        private static void OnFillPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CementSiloControl)d).UpdateLevel();
        }

        public CementSiloControl()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateLevel();
        }

        private void UpdateLevel()
        {
            // LevelRect yüksekliği: 150 (XAML ile eşleşmeli). Doluluk 0-100.
            double totalHeight = 150.0;
            double pct = FillPercent;
            pct = Math.Max(0, Math.Min(100, pct));
            double height = totalHeight * (pct / 100.0);
            // LevelFill'ın Canvas.Top - hesapla
            double top = 50 + (totalHeight - height);
            LevelFill.Height = height;
            Canvas.SetTop(LevelFill, top);
            PercentLabel.Text = $"{(int)pct} %";
        }
    }
}


