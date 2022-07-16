using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Ovenbird
{
    public class OvenbirdInfo : GH_AssemblyInfo
    {
        public override string Name => "Ovenbird";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Testing modules to aid geometric interoperability between BIM and BEM. " +
            "Part of Gingerbread project. WIP";

        public override Guid Id => new Guid("1216BC99-9452-4FF5-866D-AB8AE90C7C73");

        //Return a string identifying you or your company.
        public override string AuthorName => "Yikun";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "ianquinn@163.com /n Tongji University, Shanghai";
    }
}