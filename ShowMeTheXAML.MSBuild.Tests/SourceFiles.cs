using System.Xml.Linq;

namespace ShowMeTheXAML.MSBuild.Tests
{
    public static class SourceFiles
    {
        public static XDocument MdixGrids => XDocument.Parse(@"<UserControl x:Class=""MaterialDesignColors.WpfExample.Grids""
             xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" 
             xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
             xmlns:materialDesign=""http://materialdesigninxaml.net/winfx/xaml/themes""
             xmlns:smtx=""clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML""
             mc:Ignorable=""d"" 
             d:DesignHeight=""300"" d:DesignWidth=""300"">
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source=""pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.DataGrid.xaml"" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
    <ScrollViewer>
        <StackPanel>
            <TextBlock>Custom Columns</TextBlock>
            <smtx:XamlDisplay Key=""grids_1"" Style=""{StaticResource PopupXamlDisplay}"">
                <DataGrid Margin=""0 8 0 0"" ItemsSource=""{Binding Items3}"" CanUserSortColumns=""True"" CanUserAddRows=""False"" AutoGenerateColumns=""False""
                          materialDesign:DataGridAssist.CellPadding=""13 8 8 8"" materialDesign:DataGridAssist.ColumnHeaderPadding=""8"">
                    <DataGrid.Columns>
                        <DataGridCheckBoxColumn Binding=""{Binding IsSelected}"" 
                                            ElementStyle=""{StaticResource MaterialDesignDataGridCheckBoxColumnStyle}""
                                            EditingElementStyle=""{StaticResource MaterialDesignDataGridCheckBoxColumnEditingStyle}"">
                            <DataGridCheckBoxColumn.Header>
                                <!--padding to allow hit test to pass thru for sorting -->
                                <Border Background=""Transparent"" Padding=""6 0 6 0"" HorizontalAlignment=""Center"">
                                    <CheckBox HorizontalAlignment=""Center""
                                          DataContext=""{Binding RelativeSource={RelativeSource AncestorType={x:Type DataGrid}}, Path=DataContext}""
                                          IsChecked=""{Binding IsAllItems3Selected}"" />
                                </Border>
                            </DataGridCheckBoxColumn.Header>
                        </DataGridCheckBoxColumn>
                        <DataGridTextColumn Binding=""{Binding Code}""
                                        Header=""Code""
                                        EditingElementStyle=""{StaticResource MaterialDesignDataGridTextColumnEditingStyle}"" />
                        <!-- if you want to use the pop up style (MaterialDesignDataGridTextColumnPopupEditingStyle), you must use MaterialDataGridTextColumn -->
                        <materialDesign:MaterialDataGridTextColumn Binding=""{Binding Name}""
                                                               Header=""Name""
                                                               EditingElementStyle=""{StaticResource MaterialDesignDataGridTextColumnPopupEditingStyle}"" 
                                                               />
                        <!-- set a max length to get an indicator in the editor -->
                        <materialDesign:MaterialDataGridTextColumn Binding=""{Binding Description}""
                                                               Header=""Description""
                                                               MaxLength=""255"" 
                                                               EditingElementStyle=""{StaticResource MaterialDesignDataGridTextColumnPopupEditingStyle}""  />
                        <materialDesign:MaterialDataGridTextColumn Binding=""{Binding Numeric}""
                                                        Header=""Numeric""
                                                        EditingElementStyle=""{StaticResource MaterialDesignDataGridTextColumnPopupEditingStyle}"">
                            <DataGridTextColumn.HeaderStyle>
                                <Style TargetType=""{x:Type DataGridColumnHeader}"" BasedOn=""{StaticResource MaterialDesignDataGridColumnHeader}"">
                                    <Setter Property=""HorizontalAlignment"" Value=""Right"" />
                                </Style>
                            </DataGridTextColumn.HeaderStyle>
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType=""{x:Type TextBlock}"">
                                    <Setter Property=""HorizontalAlignment"" Value=""Right"" />
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </materialDesign:MaterialDataGridTextColumn>

                        <!-- use custom combo box column to get better combos. Use ItemsSourceBinding as your binding template to be applied to each combo -->
                        <materialDesign:MaterialDataGridComboBoxColumn Header=""Food""
                                                                   SelectedValueBinding=""{Binding Food}""
                                                                   ItemsSourceBinding=""{Binding RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type DataGrid}}, Path=DataContext.Foods}"" />
                    </DataGrid.Columns>
                </DataGrid>
            </smtx:XamlDisplay>
            <TextBlock Margin=""0 24 0 0"">Auto Generated Columns</TextBlock>
            <smtx:XamlDisplay Key=""grids_2"" Style=""{StaticResource PopupXamlDisplay}"">
                <DataGrid  Margin=""0 8 0 0"" ItemsSource=""{Binding Items3}"" CanUserSortColumns=""True"" CanUserAddRows=""False"" />
            </smtx:XamlDisplay>
            <TextBlock Margin=""0 24 0 0"">Custom Padding</TextBlock>
            <smtx:XamlDisplay Key=""grids_3"" Style=""{StaticResource PopupXamlDisplay}"">
                <DataGrid  Margin=""0 8 0 0"" ItemsSource=""{Binding Items3}"" CanUserSortColumns=""True"" CanUserAddRows=""False""
                           materialDesign:DataGridAssist.CellPadding=""4 2 2 2"" materialDesign:DataGridAssist.ColumnHeaderPadding=""4 2 2 2""
                           />
            </smtx:XamlDisplay>
        </StackPanel>
    </ScrollViewer>
</UserControl>");

        public static XDocument MdixList => XDocument.Parse(@"<UserControl x:Class=""MaterialDesignColors.WpfExample.Lists""
             xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" 
             xmlns:d=""http://schemas.microsoft.com/expression/blend/2008"" 
             xmlns:domain=""clr-namespace:MaterialDesignColors.WpfExample.Domain""
             xmlns:smtx=""clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML""
             mc:Ignorable=""d"" 
             d:DesignHeight=""300"" d:DesignWidth=""300"">
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source=""pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.TextBlock.xaml"" />
                <ResourceDictionary Source=""pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.ToggleButton.xaml"" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
    <Grid Margin=""8"" >
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>
        <TextBlock Style=""{StaticResource MaterialDesignTitleTextBlock}"">ListBox</TextBlock>
        <Grid Grid.Row=""1"">
            <Grid.RowDefinitions>
                <RowDefinition/>
                <RowDefinition Height=""Auto""/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""1*"" />
                <ColumnDefinition Width=""1*"" />
                <ColumnDefinition Width=""1*"" />
            </Grid.ColumnDefinitions>
            <smtx:XamlDisplay Key=""list_1"" Style=""{StaticResource PopupXamlDisplay}"" Grid.Column=""0"">
                <ListBox IsEnabled=""{Binding IsChecked, ElementName=EnableListBox}"">
                    <TextBlock>Plain</TextBlock>
                    <TextBlock>Old</TextBlock>
                    <TextBlock>ListBox</TextBlock>
                    <TextBlock>Full of junk</TextBlock>
                </ListBox>
            </smtx:XamlDisplay>
            <CheckBox Name=""EnableListBox"" Grid.Column=""0""  Grid.Row=""1"" IsChecked=""True"">Enabled</CheckBox>

            <smtx:XamlDisplay Key=""list_2"" Style=""{StaticResource PopupXamlDisplay}"" Grid.Column=""1"" Grid.Row=""0"">
                <!-- piece together your own items control to create some nice stuff that will make everyone think you are cool. and rightly so, because you are cool.  you might even be a hipster for all I know -->
                <ItemsControl  ItemsSource=""{Binding Items1}""
                      Grid.IsSharedSizeScope=""True""
                      Margin=""12 0 12 0"">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate DataType=""{x:Type domain:SelectableViewModel}"">
                            <Border x:Name=""Border"" Padding=""8"">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition SharedSizeGroup=""Checkerz"" />
                                        <ColumnDefinition />
                                    </Grid.ColumnDefinitions>
                                    <CheckBox VerticalAlignment=""Center"" IsChecked=""{Binding IsSelected}""/>
                                    <StackPanel Margin=""8 0 0 0"" Grid.Column=""1"">
                                        <TextBlock FontWeight=""Bold"" Text=""{Binding Name}"" />
                                        <TextBlock Text=""{Binding Description}"" />
                                    </StackPanel>
                                </Grid>
                            </Border>
                            <DataTemplate.Triggers>
                                <DataTrigger Binding=""{Binding IsSelected}"" Value=""True"">
                                    <Setter TargetName=""Border"" Property=""Background"" Value=""{DynamicResource MaterialDesignSelection}"" />
                                </DataTrigger>
                            </DataTemplate.Triggers>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </smtx:XamlDisplay>

            <smtx:XamlDisplay Key=""list_3"" Style=""{StaticResource PopupXamlDisplay}"" Grid.Column=""2"" Grid.Row=""0"">
                <!-- and here's another -->
                <ItemsControl ItemsSource=""{Binding Items2}"" Grid.IsSharedSizeScope=""True"">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate DataType=""{x:Type domain:SelectableViewModel}"">
                            <Border x:Name=""Border"" Padding=""8"" BorderThickness=""0 0 0 1"" BorderBrush=""{DynamicResource MaterialDesignDivider}"">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition SharedSizeGroup=""Checkerz"" />
                                        <ColumnDefinition />
                                    </Grid.ColumnDefinitions>
                                    <ToggleButton VerticalAlignment=""Center"" IsChecked=""{Binding IsSelected}""
                                                  Style=""{StaticResource MaterialDesignActionLightToggleButton}""
                                                  Content=""{Binding Code}"" />
                                    <StackPanel Margin=""8 0 0 0"" Grid.Column=""1"">
                                        <TextBlock FontWeight=""Bold"" Text=""{Binding Name}"" />
                                        <TextBlock Text=""{Binding Description}"" />
                                    </StackPanel>
                                </Grid>
                            </Border>
                            <DataTemplate.Triggers>
                                <DataTrigger Binding=""{Binding IsSelected}"" Value=""True"">
                                    <Setter TargetName=""Border"" Property=""Background"" Value=""{DynamicResource MaterialDesignSelection}"" />
                                </DataTrigger>
                            </DataTemplate.Triggers>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </smtx:XamlDisplay>
        </Grid>
        <TextBlock Style=""{StaticResource MaterialDesignTitleTextBlock}"" Grid.Row=""2"" Margin=""0 32 0 0"">ListView</TextBlock>
        <smtx:XamlDisplay Key=""lists_4"" Style=""{StaticResource PopupXamlDisplay}"" Grid.Row=""3"">
            <ListView>
                <ListViewItem>
                    Hello
                </ListViewItem>
                <ListViewItem>
                    World
                </ListViewItem>
                <ListViewItem>
                    :)
                </ListViewItem>
            </ListView>
        </smtx:XamlDisplay>
        <TextBlock Style=""{StaticResource MaterialDesignTitleTextBlock}"" Grid.Row=""4"" Margin=""0 32 0 0"">ListView.GridView</TextBlock>
        <smtx:XamlDisplay Key=""lists_5"" Style=""{StaticResource PopupXamlDisplay}"" Grid.Row=""5"">
            <ListView ItemsSource=""{Binding Items1}"">
                <ListView.View>
                    <GridView>
                        <GridViewColumn DisplayMemberBinding=""{Binding Code}"" Header=""Code"" />
                        <GridViewColumn DisplayMemberBinding=""{Binding Name}"" Header=""Name"" />
                        <GridViewColumn DisplayMemberBinding=""{Binding Description}"" Header=""Description"" />
                    </GridView>
                </ListView.View>
            </ListView>
        </smtx:XamlDisplay>
    </Grid>

</UserControl>
");
    }
}