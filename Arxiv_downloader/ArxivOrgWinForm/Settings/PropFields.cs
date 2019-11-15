using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArxivOrgWinForm.Settings
{
    public class PropFields
    {
        public string XMLFileName = Environment.CurrentDirectory + "\\settings.xml";
        public int YearStart = 2007;
        public int YearStop = 2007;
        public int MonthStart = 4;
        public int MonthStop = 4;
        public int Index = 1;
        public bool rdBtnUpTo07 = true;
        public string PathPDF = @"C:\";
        public bool GetPDF = false;
        public bool GetArticles = false;
    }
}
