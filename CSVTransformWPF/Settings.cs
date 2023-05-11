using System;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace CSVTransformWPF
{
    // holds the user settings
    [Serializable]
    public class UserSettings : INotifyPropertyChanged
    {
        public string Language
        {
            get => _language;
            set
            {
                if (value != _language)
                {
                    _language = value;
                    OnPropertyChanged(nameof(Language));
                }
            }
        }
        [XmlElement("Language")]
        private string _language = "de-DE";

        public string LastRuleFolderPath
        {
            get => _lastRuleFolderPath;
            set
            {
                if (value != _lastRuleFolderPath)
                {
                    _lastRuleFolderPath = value;
                    OnPropertyChanged(nameof(LastRuleFolderPath));
                }
            }
        }
        [XmlElement("LastRuleFolderPath")]
        private string _lastRuleFolderPath = System.IO.Path.GetFullPath("Rules");

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // manages the user settings
    public class SettingsManager<T> where T : class , INotifyPropertyChanged , new()
    {
        private readonly string _settingsFilePath;
        public T Settings
        {
            get => _settings;
            set
            {
                _settings = value;
            }
        }
        private T _settings;

        public SettingsManager(string settingsFileName)
        {
            _settingsFilePath = GetSettingsFilePath(settingsFileName);
            // if the settings file does not exist, create a new settings object
            _settings = LoadSettings() ?? new T();
            // Subscribe to the PropertyChanged event of the settings object to save the settings when a property changes
            _settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        private static string GetSettingsFilePath(string settingsFileName)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appDataPath, settingsFileName);
        }

        private T? LoadSettings() =>
            File.Exists(_settingsFilePath) ?
            new De_Serializer<T>().FromXml(_settingsFilePath) : null;
        
        public void SaveSettings(T settings) 
        { 
            new De_Serializer<T>().ToXml(_settingsFilePath, settings);
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveSettings(_settings);
        }
    }
}
