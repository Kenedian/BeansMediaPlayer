using System.Windows;

namespace BeansMediaPlayer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainContent.Content = new MenuView();
        }
    }
}