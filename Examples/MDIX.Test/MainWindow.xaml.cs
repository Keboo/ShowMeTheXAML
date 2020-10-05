using System.Windows;
using ShowMeTheXAML;

namespace MDIX.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Display1.Formatter = new IndentedXamlFormatter()
        }
    }
}
