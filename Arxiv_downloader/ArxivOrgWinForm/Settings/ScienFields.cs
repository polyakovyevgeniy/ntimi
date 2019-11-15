using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArxivOrgWinForm.Settings
{
    public class ScienFields
    {
        public string XMLFileName = Environment.CurrentDirectory + "\\settingsSubj.xml";
        //public string XMLFileName = string.Format(Environment.CurrentDirectory + @"\settingsSubj{0}.{1}.{2}.xml",
        //                                            DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Date);

        public List<Science> Sciences;

        public ScienFields()
        {
            Sciences = new List<Science>();
        }
    }
}
