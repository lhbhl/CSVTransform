using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

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
            var settingsManager = new SettingsManager<UserSettings>("UserSettings.xml");
            var userSettings = settingsManager.Settings;
            
            SetStringDictionaryLocalized(userSettings.Language);
            var vm = new MainWindowViewModel(userSettings, Current);
            vm.PropertyChanged += OnLanguageChanged;
            mainWindow.Closing += vm.WindowClosing;
            mainWindow.DataContext = vm;
            mainWindow.Show();
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedLanguage")
            {
                if (sender is MainWindowViewModel vm)
                {
                    SetStringDictionaryLocalized(vm.SelectedLanguage);

                    // stupid way to refresh available language list
                    string[] _availableLanguages = new string[vm.AvailableLanguages.Length]; 
                    vm.AvailableLanguages.CopyTo(_availableLanguages, 0);
                    vm.AvailableLanguages = _availableLanguages;
                }
            }
        }

        private void Restart()
        {
            System.Diagnostics.Process.Start(ResourceAssembly.Location);
            Current.Shutdown();
        }

        private void SetStringDictionaryLocalized(string language)
        {
            ResourceDictionary dict = new()
            {
                Source = language switch
                {
                    "de-DE" => new Uri("Resources/Resources_de.xaml", UriKind.Relative),
                    "en-US" => new Uri("Resources/Resources_en.xaml", UriKind.Relative),
                    _ => new Uri("Resources/Resources_de.xaml", UriKind.Relative),
                }
            };
            Resources.MergedDictionaries.Add(dict);
        }
    }

    public class LanguageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string[])
            {
                string[]? _value = value as string[];
                if (_value != null && _value.Length > 0)
                {
                    string[] _r = new string[_value.Length];
                    for (int i = 0; i < _value.Length; i++)
                    {
                        _r[i] = Application.Current.TryFindResource("human_readable_language_" + _value[i]) as string ?? "";
                    }
                    return _r;
                }
                return new string[] { "" };
            }
            else
                return Application.Current.TryFindResource("human_readable_language_" + value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string[])
            {
                string[]? _value = value as string[];
                if (_value != null && _value.Length > 0)
                {
                    string[] _r = new string[_value.Length];
                    for (int i = 0; i < _value.Length; i++)
                    {
                        _r[i] = Application.Current.TryFindResource("universal_language_identifier_" + _value[i]) as string ?? "";
                    }
                    return _r;
                }
                return new string[] { "" };
            }
            else
                return Application.Current.TryFindResource("universal_language_identifier_" + value);
        }
    }
}
