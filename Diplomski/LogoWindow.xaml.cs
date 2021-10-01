using Diplomski.Properties;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Diplomski
{
    public partial class LogoWindow : Window
    {
        DispatcherTimer endLogoAnimation;
        DispatcherTimer fadeOutAnimation;

        public LogoWindow()
        {
            InitializeComponent();

            string color = Settings.Default.color;
            Uri uri = new Uri($"pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor." + color + ".xaml");
            Application.Current.Resources.MergedDictionaries.RemoveAt(2);
            Application.Current.Resources.MergedDictionaries.Insert(2, new ResourceDictionary() { Source = uri });

            DoubleAnimation ani = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(OpacityProperty, ani);

            endLogoAnimation = new DispatcherTimer();
            endLogoAnimation.Tick += endLogoAnimation_Tick;
            endLogoAnimation.Interval = new TimeSpan(0, 0, 2);
            endLogoAnimation.Start();
        }

        private void endLogoAnimation_Tick(object sender, EventArgs e)
        {
            fadeOutAnimation = new DispatcherTimer();
            fadeOutAnimation.Tick += fadeOutAnimation_Tick;
            fadeOutAnimation.Interval = new TimeSpan(0, 0, 1);

            DoubleAnimation ani = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(OpacityProperty, ani);
            fadeOutAnimation.Start();
            endLogoAnimation.Stop();
        }

        private void fadeOutAnimation_Tick(object sender, EventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Show();
            fadeOutAnimation.Stop();
            this.Close();
        }
    }
}
