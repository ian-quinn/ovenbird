using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovenbird
{
    public class OvenbirdIcon : Grasshopper.Kernel.GH_AssemblyPriority
    {
        public override Grasshopper.Kernel.GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("Ovenbird", Properties.Resources.Coenobita);
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("Ovenbird", 'O');

            return Grasshopper.Kernel.GH_LoadingInstruction.Proceed;
        }
    }
}