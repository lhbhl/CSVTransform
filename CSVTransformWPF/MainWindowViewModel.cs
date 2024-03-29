﻿using Prism.Commands;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CSVTransformWPF
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private IList<InputFile>? _inputFiles;
        private IList<RuleSet>? _ruleSets;
        private Application _currentApplication;
        public string[] AvailableLanguages
        {
            get
            {
                return _availableLanguages;
            }
            set
            {
                _availableLanguages = value;
                OnPropertyChanged(nameof(AvailableLanguages));
            }
        }
        private string[] _availableLanguages = { "de-DE", "en-US" };
        

        public MainWindowViewModel(UserSettings settings, Application currentApplication)
        {
            _readyToSave = false;
            _readyToStart = false;
            _isProcessing = false;
            _settings = settings;
            _settings.PropertyChanged += OnSettingsPropertyChanged;
            _currentApplication = currentApplication;
            SelectedLanguage = _settings.Language;
            // 
            UpdateRuleSets();
        }

        public UserSettings Settings 
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }
        private UserSettings _settings;

        
        // look in Rules folder for rules files
        public IList<RuleSet> RuleSets
        {
            get
            {
                if (_ruleSets != null)
                {
                    return _ruleSets;
                }
                else
                {
                    return new List<RuleSet>();
                }
            }
            set
            {
                _ruleSets = value;
                OnPropertyChanged(nameof(RuleSets));
            }
        }

        // Selected rules file
        public RuleSet? SelectedRuleSet
        {
            get { return _selectedRuleSet; }
            set
            {
                _selectedRuleSet = value;
                OnPropertyChanged(nameof(SelectedRuleSet));
            }
        }
        private RuleSet? _selectedRuleSet;

        // delegate command to open openai dialog
        public DelegateCommand OpenOpenAIDialogCommand
        {
            get => _openOpenAIDialogCommand ?? new DelegateCommand(OpenOpenAIDialog, () => IsIdle).ObservesCanExecute(() => IsIdle);
            set { _openOpenAIDialogCommand = value; }
        }
        private DelegateCommand? _openOpenAIDialogCommand;

        // actual function to open openai dialog
        void OpenOpenAIDialog()
        {
            var __openAIDialog = new OpenAIDialog();
            var __vm = new OpenAIDialogViewModel();
            __openAIDialog.DataContext = __vm;
            __openAIDialog.ShowDialog();
        }

        // delegate command to start conversion
        public DelegateCommand StartCommand
        {
            get => _startCommand ?? new DelegateCommand(StartConversion, () => ReadyToStart).ObservesCanExecute(() => ReadyToStart);
            set { _startCommand = value; }
        }
        private DelegateCommand? _startCommand;

        // actual function to start conversion
        void StartConversion()
        {
            IsProcessing = true;

            if (SelectedRuleSet == null || SelectedRuleSet.Filepath == null || InputFiles == null)
            {
                return;
            }

            CsvConverter.CsvConverter __csvConverter = new(SelectedRuleSet.Filepath);

            foreach (var inputFile in InputFiles)
            {
                inputFile.State = 0;
                inputFile.ImageData = null;
            }
            foreach (var inputFile in InputFiles)
            {
                // read file into a string array 
                var __fp = inputFile.Filepath;
                if (__fp == null)
                {
                    continue;
                }
                
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(__fp);
                    var __returnedLines = __csvConverter.ConvertCsvFormat(lines);
                    // conversion went very wrong, nothing returned
                    if (__returnedLines == null)
                    {
                        inputFile.State = -1;
                        inputFile.ImageData = new BitmapImage(new Uri("pack://application:,,,/Resources/error.png"));
                        inputFile.ErrorMsg += _currentApplication.TryFindResource("NoLinesConvertedError").ToString();
                        continue;
                    }
                    // conversion went wrong, but some lines were returned
                    else if (!__returnedLines.All(x => x != null))
                    {
                        inputFile.State = 1;
                        inputFile.ImageData = new BitmapImage(new Uri("pack://application:,,,/Resources/error.png"));
                        inputFile.ErrorMsg += _currentApplication.TryFindResource("SomeLinesNotConvertedError").ToString();
                    }
                    // conversion went well
                    else
                    {
                        inputFile.State = 1;
                        inputFile.ImageData = new BitmapImage(new Uri("pack://application:,,,/Resources/ok.png"));
                    }

                    // copy all lines from __returnedLines that aren't null into inputFile.ConvertedLines
                    inputFile.ConvertedLines = __returnedLines.Where(x => x != null).ToArray();
                }
                catch (Exception e)
                {
                    inputFile.State = -1;
                    inputFile.ImageData = new BitmapImage(new Uri("pack://application:,,,/Resources/error.png"));
                    inputFile.ErrorMsg += e.Message;
                }
            }
            IsProcessing = false;
        }

        // delegate command to save converted files
        public DelegateCommand SaveCommand
        {
            get => _saveCommand ?? new DelegateCommand(SaveFiles, () => ReadyToSave).ObservesCanExecute(() => ReadyToSave);
            set { _saveCommand = value; }
        }
        private DelegateCommand? _saveCommand;

        // actual function to save converted files
        void SaveFiles()
        {
            System.Windows.Forms.FolderBrowserDialog __fbd = new();
            if (__fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var __saveDir = __fbd.SelectedPath;
                if (InputFiles == null) throw new NullReferenceException("");            
                if (!System.IO.Directory.Exists(__saveDir)) throw new System.IO.DirectoryNotFoundException(_currentApplication.TryFindResource("SaveDirDoesntExistError").ToString());
                // save each file to the folder
                foreach (var inputFile in InputFiles)
                {
                    if (inputFile.ConvertedLines == null)
                    {
                        inputFile.ErrorMsg += _currentApplication.TryFindResource("NoConvertedLinesOnSaveError");
                        continue;
                    }

                    if (inputFile.Filename == null)
                    {
                        inputFile.ErrorMsg += _currentApplication.TryFindResource("NoOutputFilenameError");
                        continue;
                    }

                    var __fp = System.IO.Path.Combine(__saveDir, inputFile.Filename);
                    // if file exists, ask User if they want to overwrite
                    if (System.IO.File.Exists(__fp))
                    {
                        var __result = System.Windows.MessageBox.Show($"{__fp}: {_currentApplication.TryFindResource("FileExistsExplanation").ToString()}", 
                            _currentApplication.TryFindResource("FileExistsHeader").ToString(), System.Windows.MessageBoxButton.YesNo);
                        if (__result == System.Windows.MessageBoxResult.No)
                        {
                            continue;
                        }
                    }
                    try
                    {
                        System.IO.File.WriteAllLines(__fp, inputFile.ConvertedLines);
                    }

                    catch (Exception e)
                    {
                        inputFile.State = -1;
                        inputFile.ImageData = new BitmapImage(new Uri("pack://application:,,,/Resources/error.png"));
                        inputFile.ErrorMsg += e.Message;
                    }

                    inputFile.State = 0;
                }
            }
        }
               
        // delegate command to choose files
        public DelegateCommand ChooseFilesCommand
        {
            get => _chooseFilesCommand ?? new DelegateCommand(ChooseFiles, () => IsIdle).ObservesCanExecute(() => IsIdle);
            set { _chooseFilesCommand = value; }
        }
        private DelegateCommand? _chooseFilesCommand;

        // actual function to choose files
        void ChooseFiles()
        {
            // open file dialog for user to choose one or more .csv files
            Microsoft.Win32.OpenFileDialog ofd = new()
            {
                Filter = "CSV files (*.csv)|*.csv",
                Multiselect = true
            };
            if (ofd.ShowDialog() == true)
            {
                var __inputFiles = new List<InputFile>();
                // add each file to InputFiles
                foreach (string filename in ofd.FileNames)
                {
                    __inputFiles.Add(new InputFile { Filename = System.IO.Path.GetFileName(filename), Filepath = filename, State = 0, ImageData = null });
                }
                InputFiles = __inputFiles;
            }
        }

        // delegate command to choose rules file directory
        public DelegateCommand ChooseRulesDirCommand
        {
            get => _chooseRulesDirCommand ?? new DelegateCommand(ChooseRulesDir, () => IsIdle).ObservesCanExecute(() => IsIdle);
            set { _chooseRulesDirCommand = value; }
        }
        private DelegateCommand? _chooseRulesDirCommand;

        // actual function to choose rules file directory
        void ChooseRulesDir()
        {
            // open folder dialog for user to choose rules directory
            System.Windows.Forms.FolderBrowserDialog fbd = new();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.LastRuleFolderPath = fbd.SelectedPath;
            }
        }

        private void UpdateRuleSets()
        {
            // check if directory contains rules files
            if (Settings.LastRuleFolderPath == null || !System.IO.Directory.Exists(Settings.LastRuleFolderPath)) return;

            var __rulesFiles = System.IO.Directory.GetFiles(Settings.LastRuleFolderPath, "*.xml");
            if (__rulesFiles.Length > 0)
            {
                var __ruleSets = new List<RuleSet>();

                foreach (string? filename in __rulesFiles)
                {
                    __ruleSets.Add(new(filename));
                }
                RuleSets = __ruleSets;
            }
        }

        public void WindowClosing(object? sender, CancelEventArgs e)
        {
            // if we have files that have been converted but not saved, ask the user if they want to save them
            if (InputFiles != null && InputFiles.Any(x => x.State == 1))
            {
                var __result = System.Windows.MessageBox.Show(_currentApplication.TryFindResource("UnsavedFilesMsgBoxExplanation").ToString(),
                                                             _currentApplication.TryFindResource("SaveFilesQuery").ToString(), System.Windows.MessageBoxButton.YesNoCancel);
                if (__result == System.Windows.MessageBoxResult.Yes)
                {
                    SaveFiles();
                }
                else if (__result == System.Windows.MessageBoxResult.Cancel)
                {
                    return;
                }
            }
        }

        public InputFile? SelectedInputFile
        {
            get { return _selectedInputFile; }
            set
            {
                _selectedInputFile = value;
                OnPropertyChanged(nameof(SelectedInputFile));
            }
        }
        private InputFile? _selectedInputFile;

        public bool IsIdle
        {
            get { return !_isProcessing; }
            set
            {
                IsProcessing = !value;
                OnPropertyChanged(nameof(IsIdle));
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        public bool ReadyToSave
        {
            get { return _readyToSave; }
            set
            {
                _readyToSave = value;
                OnPropertyChanged(nameof(ReadyToSave));
            }
        }
        private bool _readyToSave;
        public bool ReadyToStart
        {
            get { return _readyToStart; }
            set
            {
                _readyToStart = value;
                OnPropertyChanged(nameof(ReadyToStart));
            }
        }
        private bool _readyToStart;

        public bool IsProcessing
        {
            get { return _isProcessing; }
            set
            {
                _isProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
                OnPropertyChanged(nameof(IsIdle));
            }
        }
        private bool _isProcessing;

        public IList<InputFile>? InputFiles
        {
            get { return _inputFiles; }
            set
            {
                _inputFiles = value;
                OnPropertyChanged(nameof(InputFiles));
            }
        }

        public string SelectedLanguage
        {
            get { return _selectedLanguage; }
            set
            {
                if (value != null && value != _selectedLanguage)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged(nameof(SelectedLanguage));
                    Settings.Language = value;
                }
            }
        }
        private string _selectedLanguage;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            // if property name is in conditions for starting, update ReadyToStart
            if (readyToSTartConditionNames.Contains(propertyName))
            {
                if (InputFiles != null)
                    ReadyToStart = (InputFiles.Count > 0) && IsIdle;
            }

            // if property name is in conditions for saving, update ReadyToSave
            if (readyToSaveConditionNames.Contains(propertyName))
            {
                if (InputFiles != null)
                    ReadyToSave = (InputFiles.Any(x => x.ConvertedLines != null && x.ConvertedLines.Length > 0)) && IsIdle;
            }

            if (propertyName == nameof(RuleSets))
            {
                SelectedRuleSet = RuleSets[0];
            }

            

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly List<string> readyToSTartConditionNames = new() { nameof(IsIdle), nameof(InputFiles) };
        private readonly List<string> readyToSaveConditionNames = new() { nameof (IsIdle), nameof(InputFiles) };

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.LastRuleFolderPath))
            {
                UpdateRuleSets();
            }
            /*if (e.PropertyName == nameof(Settings.Language))
            {
                // notify Available language changed
                OnPropertyChanged(nameof(AvailableLanguages));
            }*/
        }
    }

    public class RuleSet : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RuleSet() { }
        public RuleSet(string _filePath)
        {
            Filepath = _filePath;
            Filename = System.IO.Path.GetFileName(_filePath);
        }

        public string? Filename
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
                OnPropertyChanged(nameof(Filename));
            }
        }
        private String? _filename;

        public string? Filepath
        {
            get
            {
                return _filepath;
            }
            set
            {
                _filepath = value;
                OnPropertyChanged(nameof(Filepath));
            }
        }
        private String? _filepath;
    }

    // represents an input file for ListView
    public class InputFile : INotifyPropertyChanged
    {
        public string? Filename
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
                OnPropertyChanged(nameof(Filename));
            }
        }
        private String? _filename;

        public string? Filepath
        {
            get
            {
                return _filepath;
            }
            set
            {
                _filepath = value;
                OnPropertyChanged(nameof(Filepath));
            }
        }
        private string? _filepath;

        public int State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }
        private int _state;

        public string? ErrorMsg
        {
            get
            {
                return _errorMsg;
            }
            set
            {
                _errorMsg = value;
                OnPropertyChanged(nameof(ErrorMsg));
            }
        }
        private string? _errorMsg;
        public BitmapImage? ImageData
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
                OnPropertyChanged(nameof(ImageData));
            }
        }
        private BitmapImage? _image;

        public string[]? ConvertedLines
        {
            get
            {
                return _convertedLines;
            }
            set
            {
                _convertedLines = value;
                OnPropertyChanged(nameof(ConvertedLines));
            }
        }
        private string[]? _convertedLines;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
