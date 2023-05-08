using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace CSVTransformWPF.CSVConverter
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
            Formula = new RulePart[0];
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
        private static readonly Dictionary<string, Func<string, string, string>> StrOps = new ()
        {
                {"&", (x, y) => x + y}
        };

        // a dictionary of ternary operator strings (for string operands) and their corresponding functions
        private static readonly Dictionary<string, Func<string, string, string, string>> StrOps3 = new ()
        {
                {"/", (x, y, z) => x.Replace(y,z)},
                {"$", (x, y, z) => x.Substring(int.Parse(y), int.Parse(z))}
        };

        public static List<string> AllOps
        {
            get
            {
                // create a list of all unique keys in the dictionaries
                List<string> allOps = new List<string>(DoubleOps.Keys);
                allOps.AddRange(StrOps.Keys);
                allOps.AddRange(StrOps3.Keys);
                return allOps.Distinct().ToList();
            }
        }

        private CultureInfo _SourceCultureInfo;
        private CultureInfo _TargetCultureInfo;

        public Ops (CultureInfo sourceCultureInfo, CultureInfo targetCultureInfo)
        {
            _SourceCultureInfo = sourceCultureInfo;
            _TargetCultureInfo = targetCultureInfo;
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

    // A class that represents a CSV converter with a list of conversion rules
    [Serializable]
    public class CsvConverter
    {
        [XmlElement("source")]
        public CsvFormatSpec Source { get; set; }

        [XmlElement("target")]
        public CsvFormatSpec Target { get; set; }


        // A property that holds a list of CSV conversion rules
        [XmlArray("rules")]
        [XmlArrayItem("rule")]
        public CsvRule[] Rules { get; set; }

        private Ops _Ops;
        private Dictionary<string, int>? __StringCounts;

        public CsvConverter()
        {
            Rules = Array.Empty<CsvRule>();
           
            Source = new();
            Target = new();
            _Ops = new(Source.DecimalSeparator, Target.DecimalSeparator);
        }

        // A method that takes a CSV file name and returns an array of strings representing each line
        private static string[] ReadCsvFile(string fileName)
        {
            // Check if the file exists
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("The file does not exist.");
            }

            // Read all the lines from the file and return them as an array
            return File.ReadAllLines(fileName);
        }

        public void setCurrentOps(Ops ops)
        {
            _Ops = ops;
        }


        // takes an array of strings representing a CSV file and converts them to another format according to the CsvRules
        public string[] ConvertCsvFormat(string[] lines)
        {
            if (Rules == null)
            {
                throw new Exception("No rules found");
            }
            // Create a new array to store the converted lines
            string[] convertedLines = new string[lines.Length - Source.Header.Length + Target.Header.Length];

            // Insert the header of the target format
            Array.Copy(Target.Header, convertedLines, Target.Header.Length);
            // offset for the data after the header
            int offset = Target.Header.Length;

            // reset string counts for !COUNT operation
            __StringCounts = new Dictionary<string, int>();

            // Loop through each line in the original array
            for (int i = 0; i < lines.Length; i++)
            { 
                // Split the line by comma and store the parts in an array
                string[] parts = lines[i].Split(Source.Delimiter);

                // check if we have a rule for this line, with select operation
                var foundRule = Rules.FirstOrDefault(r => r.Identifier.Content == parts[r.Identifier.Index - 1]) ?? throw new FileFormatException($"No Rule found for Line: {i + 1}, {lines[i]}");
                string convertedLine = "";

                foreach (RulePart formula in foundRule.Formula)
                {
                    // Check if the formula is a constant value
                    if (formula.Content.StartsWith("\"") && formula.Content.EndsWith("\""))
                    {
                        convertedLine += formula.Content.Trim('"') + Target.Delimiter;
                    }
                    // it's an expression
                    else
                    {
                        convertedLine += EvaluateExpression(formula.Content, parts, formula.Decimals) + Target.Delimiter;
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

                if (__StringCounts.TryGetValue(__key, out int value))
                {
                    value++;
                    __StringCounts[__key] = value;
                }
                else
                {
                    __StringCounts[__key] = 1;
                }
                formula = formula.Replace("!COUNT", __StringCounts[__key].ToString());
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

        // take an array of strings representing each line of a CSV file and writes them to a new file
        public static void WriteCsvFile(string[] lines, string fileName)
        {
            // Create a new file with the given name and write all the lines to it
            File.WriteAllLines(fileName, lines);
        }

        // take two file names and converts the CSV format from one file to another using the rules from an XML file
        public static void ConvertCsvFiles(string inputFile, string outputFile, string xmlFile)
        {
            // Check if the XML file exists
            if (!File.Exists(xmlFile))
            {
                throw new FileNotFoundException("The XML file does not exist.");
            }

            // Create an XML serializer for the CsvConverter class
            XmlSerializer serializer = new(typeof(CsvConverter));

            // Read the XML file and deserialize it into a CsvConverter object
            CsvConverter? converter = null;
            using (StreamReader reader = new(xmlFile))
            {
                var obj = serializer.Deserialize(reader) ?? throw new XmlException("The XML file is empty / defective ");
                // check if the deserialized object is of type CsvConverter
                if (obj is CsvConverter converter1)
                {
                    converter = converter1;
                }
                else
                {
                    throw new XmlException("The XML file does not contain a CsvConverter object.");
                }
            }

            // Read the input file and store the lines in an array
            string[] inputLines = ReadCsvFile(inputFile);

            // Convert the CSV format of the input lines using the converter object and store them in another array
            string[] outputLines = converter.ConvertCsvFormat(inputLines);

            // Write the output lines to the output file
            WriteCsvFile(outputLines, outputFile);
        }
    }

    public class CsvConverterFactory { 
        public static CsvConverter FromXml(string inputfile)
        {
            // Check if the XML file exists
            if (!File.Exists(inputfile))
            {
                throw new FileNotFoundException("The XML file does not exist.");
            }
            // Create an XML serializer for the CsvConverter class
            XmlSerializer serializer = new(typeof(CsvConverter));
            // Read the XML file and deserialize it into a CsvConverter object
            CsvConverter? converter = null;
            using (StreamReader reader = new(inputfile))
            {
                var obj = serializer.Deserialize(reader) ?? throw new XmlException("The XML file is empty / defective ");
                // check if the deserialized object is of type CsvConverter
                if (obj is CsvConverter converter1)
                {
                    converter = converter1;
                }
                else
                {
                    throw new XmlException("The XML file does not contain a CsvConverter object.");
                }
            }
            converter.setCurrentOps(new(converter.Source.DecimalSeparator, converter.Target.DecimalSeparator));
            return converter;
        }
    }
}