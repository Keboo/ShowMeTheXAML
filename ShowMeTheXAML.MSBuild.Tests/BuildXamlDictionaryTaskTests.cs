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

            (var _, string xaml) = task.ParseXamlFile(SourceFiles.MdixList, "fileRef").Single(x => x.location.Key == "list_3");
            
            Assert.AreEqual(@"", xaml);
        }
    }
}
