using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShowMeTheXAML.Tests
{
    [TestClass]
    public class XamlFormatterTests
    {
        [TestMethod]
        public void CanIgnoreTopLevelElement()
        {
            //Arrange
            string xaml = @"
<StackPanel showMeTheXaml:XamlDisplay.Ignore=""This"" xmlns:showMeTheXaml=""clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML"">
  <Button />

  <Button />
</StackPanel>";

            var formatter = new XamlFormatter();

            //Act
            var formatted = formatter.FormatXaml(xaml);

            //Assert
            Assert.AreEqual("<Button />\r\n<Button />", formatted);
        }

        [TestMethod]
        public void CanIgnoreElementAndChildren()
        {
            //Arrange
            string xaml = @"
<StackPanel showMeTheXaml:XamlDisplay.Ignore=""This"" xmlns:showMeTheXaml=""clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML"">
  <Button />
  <StackPanel showMeTheXaml:XamlDisplay.Ignore=""ThisAndChildren"">
    <TextBlock Text=""Some Heading"" />
  </StackPanel>
  <Button />
</StackPanel>";

            var formatter = new XamlFormatter();

            //Act
            var formatted = formatter.FormatXaml(xaml);

            //Assert
            Assert.AreEqual("<Button />\r\n<Button />", formatted);
        }

        [TestMethod]
        public void CanIgnoreElementSyntaxChildren()
        {
            //Arrange
            string xaml = @"
<StackPanel showMeTheXaml:XamlDisplay.Ignore=""This"" xmlns:showMeTheXaml=""clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML"">
  <StackPanel.Resources>
    <Style TargetType=""Button"" />
  </StackPanel.Resources>
  <Button>
    <TextBlock Text=""Some Text"" />
  </Button>
  <Button />
</StackPanel>";

            var formatter = new XamlFormatter { Indent = "  " };

            //Act
            var formatted = formatter.FormatXaml(xaml);

            //Assert
            Assert.AreEqual(@"<Button>
  <TextBlock Text=""Some Text"" />
</Button>
<Button />", formatted);
        }
    }
}
