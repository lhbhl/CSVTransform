using Microsoft.Extensions.Primitives;
using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenAI.GPT3.ObjectModels.SharedModels.IOpenAiModels;

namespace CSVTransformWPF
{
    internal class OpenAIDialogViewModel : INotifyPropertyChanged
    {
        private string _prePrompt =
            "Given the following example .xml file, which contains the definition of conversion rules from one .csv format to another:\r\n" +
            "<CsvConverterSpec >\r\n  <target>\r\n    <header>\r\n      <line>MAP: Unkown</line>\r\n      <line>MODEL: </line>\r\n      <line>USER: Test   NAME: Caligo   DATE: 06.03.2023 13:00:37</line>\r\n      <line>-</line>\r\n      <line>-</line>\r\n      <line>-</line>\r\n      <line>-</line>\r\n      <line>-</line>\r\n      <line>-</line>\r\n      <line>-</line>\r\n    </header>\r\n    <delimiter>,</delimiter>\r\n    <decimalseparator>.</decimalseparator>  \r\n  </target>\r\n  <source>\r\n    <delimiter>;</delimiter>\r\n    <decimalseparator>,</decimalseparator>\r\n  </source>\r\n  <rules>\r\n    <rule>\r\n      <identifier position=\"1\">CIR</identifier>\r\n      <formula>\r\n        <part>[1]</part>\r\n        <part>Circle_!COUNT</part>\r\n        <part decimals=\"4\">[2]</part>\r\n        <part decimals=\"4\">[3]</part>\r\n        <part decimals=\"4\">[4]</part>\r\n        <part decimals=\"6\">[5]</part>\r\n        <part decimals=\"6\">[6]</part>\r\n        <part decimals=\"6\">[7]</part>\r\n        <part></part>\r\n        <part decimals=\"4\">[8]*2</part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part>\"Inner\"</part>\r\n        <part></part>\r\n        <part></part>\r\n        <part>\"0.00\"</part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>      \r\n      </formula>\r\n    </rule>\r\n    <rule>\r\n      <identifier position=\"1\">PLN</identifier>\r\n      <formula>\r\n        <part>[1]</part>\r\n        <part>Plane_!COUNT</part>\r\n        <part decimals=\"4\">[2]</part>\r\n        <part decimals=\"4\">[3]</part>\r\n        <part decimals=\"4\">[4]</part>\r\n        <part decimals=\"6\">[5]</part>\r\n        <part decimals=\"6\">[6]</part>\r\n        <part decimals=\"6\">[7]</part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part>\"0.00\"</part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n        <part></part>\r\n      </formula>\r\n    </rule>\r\n  </rules>\r\n</CsvConverterSpec>\r\n" +
            "What follows after the word \"Answer\" is a new conversion rule .xml string, created by a highly skilled intelligent expert, which converts the .csv example called \"Format1\" to the .csv format from the next example called \"Format2\". If the formats \"Format1\" or \"Format2\" are inconsistent or otherwise nonsensical, what follows is the string \"THIS MAKES NO SENSE\"\r\n";
            
        
        private string _postPrompt = "\r\nAnswer:";
        public string PrePrompt
        {
            get => _prePrompt;
            set
            {
                _prePrompt = value;
                OnPropertyChanged(nameof(PrePrompt));
            }
        }

        public string PostPrompt
        {
            get => _postPrompt;
            set
            {
                _postPrompt = value;
                OnPropertyChanged(nameof(PostPrompt));
            }
        }

        // delegate command to choose rules file directory
        public DelegateCommand GetAnswerCommand
        {
            get => _getAnswerCommand ?? new DelegateCommand(GetAnswer, () => CanGetAnswer).ObservesCanExecute(() => CanGetAnswer);
            set { _getAnswerCommand = value; }
        }
        private DelegateCommand? _getAnswerCommand;

        // function to get answer from GPT-3 API
        async void GetAnswer()
        {
            var __openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = ApiKey
            });

            var __prompt = PrePrompt + "Format1:\r\n" + ExampleSourceText + "\r\nFormat2:\r\n" + ExampleTargetText + "\r\n" + PostPrompt;

            var completionResult = await __openAiService.Completions.CreateCompletion(new CompletionCreateRequest()
            {
                Prompt = __prompt,
                Model = Models.TextDavinciV3
            });

            if (completionResult.Successful)
            {
                Response.Prompt = __prompt;
                Response.Completion = completionResult.Choices.FirstOrDefault().ToString();
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new Exception("Unknown Error");
                }
                Response.Prompt = __prompt;
                Response.Completion = ApiKey + $"{completionResult.Error.Code}: {completionResult.Error.Message}";
            }
        }

        private bool _canGetAnswer = false;
        public bool CanGetAnswer
        {
            get => _canGetAnswer;
            set
            {
                _canGetAnswer = value;
                OnPropertyChanged(nameof(CanGetAnswer));
            }
        }

        private string _apiKey = string.Empty;
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                _apiKey = value;
                OnPropertyChanged(nameof(ApiKey));
            }
        }

        private string _exampleSourceText = string.Empty;
        public string ExampleSourceText
        {
            get => _exampleSourceText;
            set
            {
                _exampleSourceText = value;
                OnPropertyChanged(nameof(ExampleSourceText));
            }
        }

        private string _exampleTargetText = string.Empty;
        public string ExampleTargetText
        {
            get => _exampleTargetText;
            set
            {
                _exampleTargetText = value;
                OnPropertyChanged(nameof(ExampleTargetText));
            }
        }

        private GPTResponse _response = new();
        public GPTResponse Response
        {
            get => _response;
            set
            {
                _response = value;
                OnPropertyChanged(nameof(Response));
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (readyToSTartConditionNames.Contains(propertyName))
            {
                CanGetAnswer = (ExampleSourceText != null) && (ExampleTargetText != null) && ApiKey!=null && !IsBusy;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly List<string> readyToSTartConditionNames = new() { nameof(ApiKey), nameof(ExampleTargetText), nameof(ExampleSourceText), nameof(IsBusy) };

    }

    public class GPTResponse : INotifyPropertyChanged
    {
        public string? Prompt
        {
            get => _prompt;
            set
            {
                _prompt = value;
                OnPropertyChanged(nameof(Prompt));
            }
        }
        private string? _prompt;

        public string? Completion
        {
            get => _completion;
            set
            {
                _completion = value;
                OnPropertyChanged(nameof(Completion));
            }
        }
        private string? _completion;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
