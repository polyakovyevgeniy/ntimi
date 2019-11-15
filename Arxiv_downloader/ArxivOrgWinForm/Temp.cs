using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArxivOrgWinForm
{
    public class ListFilterer
    {
        public static IEnumerable<int> GetIntegersFromList(List<object> listOfItems)
        {
            int n;

            List<int> lst = new List<int>();
            foreach (object o in listOfItems)
            {
                if (int.TryParse(o.ToString(), out n)) lst.Add((int)o);
            }

            return lst;
        }
    }

    class Temp
    {


    }


}
