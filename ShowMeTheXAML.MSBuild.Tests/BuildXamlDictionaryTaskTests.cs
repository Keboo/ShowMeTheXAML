using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ShowMeTheXAML.MSBuild.Tests
{
    [TestClass]
    public class BuildXamlDictionaryTaskTests
    {
        [TestMethod, Ignore]
        public void CanParseWithComment()
        {
            var task = new BuildXamlDictionaryTask();

            BuildXamlDictionaryTask.DisplayerLocation location = task.ParseXamlFile(SourceFiles.MdixList, "fileRef").Single(x => x.Key == "list_3");
            
            Assert.AreEqual(@"", location.XamlData);
        }
    }
}
