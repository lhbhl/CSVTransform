using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace CSVTransformWPF.CsvConverter
{
    [Serializable]

    public class CsvFormatSpec
    {
        public CsvFormatSpec()
        {
            Header = Array.Empty<string>();
            Delimiter = ",";
            DecimalSeparator = ".";
        }

        [XmlArray("header")]
        [XmlArrayItem("line")]
        public string[] Header { get; set; }
        [XmlElement("delimiter")]
        public string Delimiter { get; set; }
        [XmlElement("decimalseparator")]
        public string DecimalSeparator { get; set; }
    }

    [Serializable]
    public class RulePart
    {
        public RulePart()
        {
            Content = "";
            Decimals = 3;
        }
        public RulePart(string part, int decimals)
        {
            Content = part;
            Decimals = decimals;
        }
        [XmlText]
        public string Content { get; set; }
        [XmlAttribute("decimals")]
        public int Decimals { get; set; }
    }

    [Serializable]
    public class RuleIdentifier
    {
        public RuleIdentifier()
        {
            Content = "";
            Index = 1;
        }
        public RuleIdentifier(string part, int index)
        {
            Content = part;
            Index = index;
        }
        [XmlText]
        public string Content { get; set; }
        [XmlAttribute("position")]
        public int Index { get; set; }
    }

    // A class that represents a CSV conversion rule
    [Serializable]
    public class CsvRule
    {
        public CsvRule()
        {
            Identifier = new RuleIdentifier();
            Formula = Array.Empty<RulePart>();
        }
        public CsvRule(RuleIdentifier identifier, RulePart[] formula)
        {
            Identifier = identifier;
            Formula = formula;
        }
        // A property that indicates the source format of the CSV line
        [XmlElement("identifier")]
        public RuleIdentifier Identifier { get; set; }

        // A property that indicates the conversion formula for each part of the CSV line
        [XmlArray("formula")]
        [XmlArrayItem("part")]
        public RulePart[] Formula { get; set; }
    }

    public class Ops
    {
        // a dictionary of operator strings (for double operands) and their corresponding functions
        private static readonly Dictionary<string, Func<double, double, double>> DoubleOps = new()
        {
                {"+", (x, y) => x + y},
                {"-", (x, y) => x - y},
                {"*", (x, y) => x * y},
                {"/", (x, y) => x / y}
        };

        // a dictionary of dual operator strings (for string operands) and their corresponding functions
        private static readonly Dictionary<string, Func<string, string, string>> StrOps = new()
        {
                {"&", (x, y) => x + y}
        };

        // a dictionary of ternary operator strings (for string operands) and their corresponding functions,
        // this is defined at runtime in the constructor, to avoid too making many of these dictionaries/operation categories of different types
        private Dictionary<string, Func<string, string, string, string>> StrOps3;

        public string Substring(string input, string from, string to)
        {
            var __from = int.Parse(from, _SourceCultureInfo);
            var __to = int.Parse(to, _SourceCultureInfo);
            return input.Substring(__from, __to);
        }

        public static List<string> AllOps
        {
            get
            {
                // create a list of all unique keys in the dictionaries
                List<string> allOps = new(DoubleOps.Keys);
                allOps.AddRange(StrOps.Keys);
                var __ops = new Ops(".", ".");
                allOps.AddRange(__ops.StrOps3.Keys);
                return allOps.Distinct().ToList();
            }
        }

        private readonly CultureInfo _SourceCultureInfo;
        private readonly CultureInfo _TargetCultureInfo;

        public Ops(CultureInfo sourceCultureInfo, CultureInfo targetCultureInfo)
        {
            _SourceCultureInfo = sourceCultureInfo;
            _TargetCultureInfo = targetCultureInfo;
            MakeStrops3();
        }

        public Ops(string? sourceDecimalSeparator, string? targetDecimalSeparator)
        {
            sourceDecimalSeparator ??= ".";
            targetDecimalSeparator ??= ".";

            _SourceCultureInfo = new CultureInfo(CultureInfo.InvariantCulture.Name);
            _SourceCultureInfo.NumberFormat.NumberDecimalSeparator = sourceDecimalSeparator;
            _SourceCultureInfo.NumberFormat.NumberGroupSeparator = "";
            _TargetCultureInfo = new CultureInfo(CultureInfo.InvariantCulture.Name);
            _TargetCultureInfo.NumberFormat.NumberDecimalSeparator = targetDecimalSeparator;
            _TargetCultureInfo.NumberFormat.NumberGroupSeparator = "";
            MakeStrops3();
        }

        private void MakeStrops3()
        {
            // defined here because of the use of the _SourceCultureInfo and _TargetCultureInfo fields
            StrOps3 = new()
            {
                    {"/", (x, y, z) => x.Replace(y,z)},
                    {"$", Substring}
            };
        }

        public string Apply(in string op, in string x, in string y, in int decimals = 3)
        {
            // check if operator in dictionary
            if (StrOps.TryGetValue(op, out Func<string, string, string>? strOpFunc))
            {
                return strOpFunc(x, y);
            }
            else if (DoubleOps.TryGetValue(op, out Func<double, double, double>? dblOpFunc))
            {
                // check if operands are numbers
                if (double.TryParse(x, _TargetCultureInfo, out double xNum) && double.TryParse(y, _TargetCultureInfo, out double yNum))
                {
                    var __res = dblOpFunc(xNum, yNum);
                    var __format = "{0:N" + decimals.ToString() + "}";
                    var __ret = string.Format(_TargetCultureInfo, __format, __res);
                    return __ret;
                }
            }
            throw new ArgumentException($"Operator: {op} not found for operands: {x} and {y}");
        }

        public string Apply(in string op, in string x, in string y, in string z)
        {
            // check if operator in dictionary
            if (!StrOps3.TryGetValue(op, out Func<string, string, string, string>? strOpFunc))
            {
                throw new ArgumentException($"Operator: {op} not found for operands: {x}, {y} and {z}");
            }
            return strOpFunc(x, y, z);
        }

        public string Apply(in string op, in string[] operands)
        {
            // check if third operand is empty string
            if (operands.Length == 2 | operands[2] == "")
            {
                return Apply(op, operands[0], operands[1]);
            }
            else if (operands.Length == 3)
            {
                return Apply(op, operands[0], operands[1], operands[2]);
            }
            throw new ArgumentException($"Invalid number of operands: {operands.Length}");
        }

        public string Apply(in string op, in string[] operands, in int decimals)
        {
            // check if third operand is empty string
            if (operands.Length == 2 | operands[2] == "")
            {
                return Apply(op, operands[0], operands[1], decimals);
            }
            else if (operands.Length == 3)
            {
                return Apply(op, operands[0], operands[1], operands[2]);
            }
            throw new ArgumentException($"Invalid number of operands: {operands.Length}");
        }

        public string Apply(in string operand, in int decimals)
        {
            if (double.TryParse(operand, _SourceCultureInfo, out double xNum))
            {
                var __res = xNum;
                var __format = "{0:N" + decimals.ToString() + "}";
                var __ret = string.Format(_TargetCultureInfo, __format, __res);
                return __ret;
            }
            return operand;
        }

    }

    // holds all the information about converting a CSV file from one format to another for CsvConverter to use
    [Serializable]
    public class CsvConverterSpec
    {
        [XmlElement("source")]
        public CsvFormatSpec Source
        { get; set; } = new();

        [XmlElement("target")]
        public CsvFormatSpec Target { get; set; } = new();

        // A property that holds a list of CSV conversion rules
        [XmlArray("rules")]
        [XmlArrayItem("rule")]
        public CsvRule[] Rules { get; set; } = Array.Empty<CsvRule>();

        public CsvConverterSpec() { }

        public static CsvConverterSpec FromXml(string csvConverterSpecFile)
        {
            // Check if the XML file exists
            if (!File.Exists(csvConverterSpecFile))
            {
                throw new FileNotFoundException($"The XML file {csvConverterSpecFile} does not exist.");
            }
            // Create an XML serializer for the CsvConverter class
            XmlSerializer serializer = new(typeof(CsvConverterSpec));
            // Read the XML file and deserialize it into a CsvConverter object
            CsvConverterSpec? converterSpec;
            using (StreamReader reader = new(csvConverterSpecFile))
            {
                var obj = serializer.Deserialize(reader) ?? throw new XmlException("The XML file is empty / defective ");
                // check if the deserialized object is of type CsvConverter
                if (obj is CsvConverterSpec converterSpec1)
                {
                    converterSpec = converterSpec1;
                }
                else
                {
                    throw new XmlException("The XML file does not contain a CsvConverter object.");
                }
            }
            return converterSpec;
        }
    }

    // A class that represents a CSV converter with a list of conversion rules
    public class CsvConverter
    {
        public CsvConverterSpec CsvConverterSpec
        {
            get { return _csvConverterSpec; }
            set
            {
                _csvConverterSpec = value;
                _Ops = new(CsvConverterSpec.Source.DecimalSeparator, CsvConverterSpec.Target.DecimalSeparator);
            }
        }
        private CsvConverterSpec _csvConverterSpec = new();

        private Ops _Ops;
        private Dictionary<string, int> _stringCounts = new();

        public CsvConverter(string csvConverterSpecFile)
        {
            CsvConverterSpec = CsvConverterSpec.FromXml(csvConverterSpecFile);
            _Ops = new(CsvConverterSpec.Source.DecimalSeparator, CsvConverterSpec.Target.DecimalSeparator);
        }

        // takes an array of strings representing a CSV file and converts them to another format according to the CsvRules
        public string[] ConvertCsvFormat(string[] lines)
        {
            if (CsvConverterSpec.Rules == null)
            {
                throw new Exception("No rules found");
            }
            // Create a new array to store the converted lines
            string[] convertedLines = new string[lines.Length - CsvConverterSpec.Source.Header.Length + CsvConverterSpec.Target.Header.Length];

            // Insert the header of the target format
            Array.Copy(CsvConverterSpec.Target.Header, convertedLines, CsvConverterSpec.Target.Header.Length);
            // offset for the data after the header
            int offset = CsvConverterSpec.Target.Header.Length;

            // reset string counts for !COUNT operation
            _stringCounts = new Dictionary<string, int>();

            // Loop through each line in the original array
            for (int i = 0; i < lines.Length; i++)
            {
                // Split the line by comma and store the parts in an array
                string[] parts = lines[i].Split(CsvConverterSpec.Source.Delimiter);

                // check if we have a rule for this line, with select operation
                var foundRule = CsvConverterSpec.Rules.FirstOrDefault(r => r.Identifier.Content == parts[r.Identifier.Index - 1]) ?? throw new FileFormatException($"No Rule found for Line: {i + 1}, {lines[i]}");
                string convertedLine = "";

                foreach (RulePart formula in foundRule.Formula)
                {
                    // Check if the formula is a constant value
                    if (formula.Content.StartsWith("\"") && formula.Content.EndsWith("\""))
                    {
                        convertedLine += formula.Content.Trim('"') + CsvConverterSpec.Target.Delimiter;
                    }
                    // it's an expression
                    else
                    {
                        convertedLine += EvaluateExpression(formula.Content, parts, formula.Decimals) + CsvConverterSpec.Target.Delimiter;
                    }
                }
                // remove last Delimiter from the converted line and add it to the new array
                convertedLines[i + offset] = convertedLine[..^1];
            }

            // return the new array of converted lines
            return convertedLines;
        }

        // convert wildcard expression to regular expression
        private static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

        // A method that takes a formula and an array of parts and evaluates it using simple arithmetic operations
        public string EvaluateExpression(string formula, string[] parts, int decimals)
        {
            // Replace any part index with its corresponding value from the parts array
            for (int i = 0; i < parts.Length; i++)
            {
                formula = formula.Replace("[" + (i + 1).ToString() + "]", _Ops.Apply(parts[i], decimals));
            }

            // check if formula still contains indices ( i.e. csv line is too short )
            if (Regex.IsMatch(formula, WildCardToRegular("*[?]*")))
            {
                throw new FileFormatException("Source CSV does not match format specified in conversion spec");
            }

            // special expression to count the number of times a string appears in the file
            if (formula.Contains("!COUNT"))
            {
                var __key = formula.Replace("!COUNT", "");

                if (_stringCounts.TryGetValue(__key, out int value))
                {
                    value++;
                    _stringCounts[__key] = value;
                }
                else
                {
                    _stringCounts[__key] = 1;
                }
                formula = formula.Replace("!COUNT", _stringCounts[__key].ToString());
            }

            var allOps = Ops.AllOps;
            // while there are operators in the formula

            while (allOps.Any(formula.Contains))
            {
                // find the first operator
                var op = allOps.First(formula.Contains);
                int index = formula.IndexOf(op);
                // find the operands
                int leftIndex = index - 1;
                int rightIndex = index + 1;
                while (leftIndex >= 0 && !Ops.AllOps.Contains("" + formula[leftIndex]))
                {
                    leftIndex--;
                }
                while (rightIndex < formula.Length && !Ops.AllOps.Contains("" + formula[rightIndex]))
                {
                    rightIndex++;
                }
                // get the operands
                var leftOperand = formula.Substring(leftIndex + 1, index - leftIndex - 1);
                var rightOperand = formula.Substring(index + 1, rightIndex - index - 1);
                // is there a third operand, separated by a semicolon from the second?
                var thirdOperand = "";
                if (rightOperand.Contains(';'))
                {
                    var split = rightOperand.Split(';');
                    rightOperand = split[0];
                    thirdOperand = split[1];
                }
                // apply the operator
                var result = _Ops.Apply(op, new string[] { leftOperand, rightOperand, thirdOperand }, decimals);
                // replace the operands and the operator with the result
                formula = string.Concat(formula.AsSpan(0, leftIndex + 1), result, formula.AsSpan(rightIndex));
            }
            return formula;
        }
    }
}