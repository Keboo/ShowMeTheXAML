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
            Assert.AreEqual("<Button />\r\n<Button />".NormalizeLineEndings(), formatted.NormalizeLineEndings());
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
            Assert.AreEqual("<Button />\r\n<Button />".NormalizeLineEndings(), formatted.NormalizeLineEndings());
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
<Button />".NormalizeLineEndings(), formatted.NormalizeLineEndings());
        }

        [TestMethod]
        public void CanFormatElementSyntaxCorrectly()
        {
            //Arrange
            string xaml =
                @"<DialogHost>
  <Border>
    <Button>RUN</Button>
  </Border>
</DialogHost>";

            var formatter = new XamlFormatter { Indent = "  " };

            //Act
            var formatted = formatter.FormatXaml(xaml);

            //Assert
            Assert.AreEqual(@"<DialogHost>
  <Border>
    <Button>
      RUN
    </Button>
  </Border>
</DialogHost>".NormalizeLineEndings(), formatted.NormalizeLineEndings());
        }

        [TestMethod]
        public void CanRemoveXamlDisplayDeclaration()
        {
            //Arrange
            string xaml =
                @"<smtx:XamlDisplay Key=""list_2"" xmlns:smtx=""clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML"">
  <!-- some comment -->
  <ItemsControl ItemsSource=""{Binding Items1}"" Grid.IsSharedSizeScope=""True"" Margin=""12 0 12 0"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    
  </ItemsControl>
</smtx:XamlDisplay>";

            var formatter = new XamlFormatter { Indent = "  " };

            //Act
            var formatted = formatter.FormatXaml(xaml);

            //Assert
            Assert.AreEqual(@"<!-- some comment -->
<ItemsControl ItemsSource=""{Binding Items1}"" Grid.IsSharedSizeScope=""True"" Margin=""12 0 12 0""></ItemsControl>", formatted.NormalizeLineEndings());
        }

        [TestMethod]
        public void HandlesRemovingNamespaces()
        {
            //Arrange
            string xaml =
                @"<smtx:XamlDisplay Key=""list_3"" Style=""{StaticResource PopupXamlDisplay}"" Grid.Column=""2"" Grid.Row=""0"" xmlns:smtx=""clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML"">
  <!-- and here's another -->
  <ItemsControl ItemsSource=""{Binding Items2}"" Grid.IsSharedSizeScope=""True"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ItemsControl.ItemTemplate>
      <DataTemplate DataType=""{x:Type domain:SelectableViewModel}"">
        <Border x:Name=""Border"" Padding=""8"" BorderThickness=""0 0 0 1"" BorderBrush=""{DynamicResource MaterialDesignDivider}"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
          
        </Border>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</smtx:XamlDisplay>";

            var formatter = new XamlFormatter { Indent = "  " };

            //Act
            var formatted = formatter.FormatXaml(xaml);

            //Assert
            Assert.AreEqual(@"<!-- and here's another -->
<ItemsControl ItemsSource=""{Binding Items2}"" Grid.IsSharedSizeScope=""True"">
  <ItemsControl.ItemTemplate>
    <DataTemplate DataType=""{x:Type domain:SelectableViewModel}"">
      <Border x:Name=""Border"" Padding=""8"" BorderThickness=""0 0 0 1"" BorderBrush=""{DynamicResource MaterialDesignDivider}""></Border>
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>", formatted.NormalizeLineEndings());
        }
    }
}
