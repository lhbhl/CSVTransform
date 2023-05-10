using System.Diagnostics;

namespace CSVConverterTest
{
    [TestClass]
    public class ConverterTest
    {
        [TestMethod]
        [DeploymentItem("TestData\\toEAT40.xml", "TestData")]
        [DeploymentItem("TestData\\TestSource.csv", "TestData")]
        [DeploymentItem("TestData\\TestTarget.csv", "TestData")]
        public void TestEAT40()
        {

            // Arrange
            CSVTransformWPF.CsvConverter.CsvConverter converter = new("TestData\\toEAT40.xml");
            // read source file to string array
            var input = File.ReadAllLines("TestData\\TestSource.csv");
            // strip newline characters
            input = input.Select(s => s.TrimEnd('\r', '\n')).ToArray();

            // same for target file
            var expected = File.ReadAllLines("TestData\\TestTarget.csv");
            expected = expected.Select(s => s.TrimEnd('\r', '\n')).ToArray();

            // Act
            var actual = converter.ConvertCsvFormat(input);
            // Assert
            Assert.AreEqual(expected.Length, actual.Length);
            for(int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileFormatException))]
        public void SourceFileFormatExceptionTest()
        {
            // Arrange
            CSVTransformWPF.CsvConverter.CsvConverter converter = new("TestData\\toEAT40.xml");
            var input = new string[] { "CIR;1000,00;500,00;100,00;1,00;0,00;0,00\r\n"};
            
            // Act
            var _ = converter.ConvertCsvFormat(input);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ArgumentExceptionTest()
        {
            // Arrange
            CSVTransformWPF.CsvConverter.CsvConverter converter = new("TestData\\toEAT40.xml");
            var input = new string[] { "CIR;1000,00;500,00;100,00;1,00;0,00;0,00;BAD\r\n" };

            // Act
            var _ = converter.ConvertCsvFormat(input);
        }

        [TestMethod]
        [ExpectedException(typeof(FileFormatException))]
        public void SourceFileFormatExceptionTest2()
        {
            // Arrange
            CSVTransformWPF.CsvConverter.CsvConverter converter = new("TestData\\toEAT40.xml");
            var input = new string[] { "BAD;1000,00;500,00;100,00;1,00;0,00;0,00;BAD\r\n" };
            // Act
            var _ = converter.ConvertCsvFormat(input);
        }
    }

}