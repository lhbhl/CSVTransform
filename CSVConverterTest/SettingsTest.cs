using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVTransformTest
{
    [TestClass]
    public class SettingsTest
    {
        [TestMethod]
        public void TestUserSettingsManager()
        {
            CSVTransformWPF.SettingsManager<CSVTransformWPF.UserSettings> settingsManager = new("UserSettings.xml");
            var userSettings = settingsManager.Settings;
            Assert.IsNotNull(userSettings);
            
            userSettings.Language = "en-US";
            settingsManager = new("UserSettings.xml");
            userSettings = settingsManager.Settings;
            
            Assert.IsNotNull(userSettings);
            Assert.AreEqual("en-US", userSettings.Language);

            userSettings.Language = "de-DE";

            settingsManager = new("UserSettings.xml");
            userSettings = settingsManager.Settings;
            Assert.IsNotNull(userSettings);
            Assert.AreEqual("de-DE", userSettings.Language);
        }
    }
}
