using System.Collections.ObjectModel;

namespace GHPHandShake.Models
{
    public class MaterialProjectNode
    {
        public string ProjectName { get; set; }
        public ObservableCollection<MaterialType> Types { get; set; } = new ObservableCollection<MaterialType>();
    }
}
