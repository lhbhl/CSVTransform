using System.Windows;

namespace CSVTransformWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // override OnStartup to show MainWindow
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var mainWindow = new MainWindow();
            var vm = new MainWindowViewModel();
            mainWindow.DataContext = vm;
            mainWindow.Show();
        }
    }
}
