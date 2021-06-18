using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEHPSTEPAP242.ViewModel.Interfaces
{
    public interface IDstNodeDiffData
    {
        public enum PartOfKind { BOTH, FIRST, SECOND, SECONDTORELOCATE };
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get;  }
        public PartOfKind PartOf { get; set; }
        public string Signature { get; }

    }
}
