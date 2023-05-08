using System.Windows;

namespace CSVTransformWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void ChooseFiles(object sender, RoutedEventArgs e)
        {
            // ToDo: let user choose file(s) to transform
        }

        private void About(object sender, RoutedEventArgs e)
        {
            // ToDo: show about dialog
        }
    }

}
