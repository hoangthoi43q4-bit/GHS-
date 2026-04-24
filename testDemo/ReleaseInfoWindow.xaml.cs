using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GHPHandShake
{
    /// <summary>
    /// Interaction logic for ReleaseInfoWindow.xaml
    /// </summary>
    public partial class ReleaseInfoWindow : Window
    {
        public ReleaseInfoWindow()
        {
            InitializeComponent();
            this.DataContext = new AboutViewModel();

  
        }
    }
}
