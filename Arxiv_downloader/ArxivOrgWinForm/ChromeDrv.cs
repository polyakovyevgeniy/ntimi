using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ArxivOrgWinForm
{
    public class ChromeDrv
    {
        int _processId = -1;
        public IWebDriver WebDriver { get; set; }

        public ChromeDrv()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--disable-extensions");
            options.AddArgument("test-type");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("no-sandbox");
            options.AddArgument("--headless");//hide browser

            //options.BinaryLocation = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

            //ChromeDriverService service = ChromeDriverService.CreateDefaultService(@"d:\_PROGRAMMING\DEVELOPMENT\C#\Solutions\14 Parsing Sites\Selenium\Arxiv.org\Arxiv.org\bin\Debug\");            
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(Environment.CurrentDirectory);

            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;//even we can hide command prompt window (with uncomment this line)  

            WebDriver = new ChromeDriver(service, options);
            //получить Id процесса
            _processId = service.ProcessId;
            //driver.Manage().Window.Maximize();
        }
        
        public void ChromeGoToURL(string url)
        {
            //перейти на страницу по адресу
            WebDriver.Navigate().GoToUrl(url);
        }
        
        internal bool IsPageNotFound()
        {
            try
            {
                //если загрузилась страница "Article xxxx.xxxx not found"                
                if (WebDriver.FindElements(By.CssSelector("#content > h1")).Count != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        internal List<string> GetArticleInfo()
        {
            List<string> lstArticleInfo = new List<string>();

            string category = WebDriver.FindElement(By.CssSelector("span.primary-subject")).Text;
            lstArticleInfo.Add(category);

            //заголовок
            string title = WebDriver.FindElement(By.CssSelector("#abs > h1")).Text;
            lstArticleInfo.Add(title);

            //авторы               
            //article.Autors = _driver.FindElement(By.CssSelector(".authors > a")).Text;
            string autors = "";
            var lstElem = WebDriver.FindElements(By.CssSelector(".authors > a"));
            foreach (IWebElement elem in lstElem)
                autors += elem.Text + ", ";
            lstArticleInfo.Add(autors);

            //Цитата
            string quotation = WebDriver.FindElement(By.CssSelector("#abs > blockquote")).Text;
            lstArticleInfo.Add(quotation);

            //дата публикации                              
            string date = WebDriver.FindElement(By.CssSelector(".submission-history")).Text;
            lstArticleInfo.Add(date);
            
            //ссылка на документ
            string document_Source = WebDriver.FindElement(By.CssSelector("div.full-text > ul > li > a")).GetAttribute("href") + ".pdf";
            lstArticleInfo.Add(document_Source);

            return lstArticleInfo;
        }
        
        internal Dictionary<string, IWebElement> GetLstElem_a()
        {
            //из первого ul выбрать все li с ссылками
            //var list_a = WebDriver.FindElement(By.CssSelector("#content > ul")).FindElements(By.CssSelector("li > a"));
            var list_l = WebDriver.FindElement(By.CssSelector("#content > ul")).FindElements(By.CssSelector("li"));

            Dictionary<string, IWebElement> dictElem = new Dictionary<string, IWebElement>();
            foreach (IWebElement l in list_l)
            {
                IWebElement a = l.FindElement(By.CssSelector("a"));
                dictElem.Add(l.Text, a);
            }

            return dictElem;
        }

        internal List<string> GetCategory(string text)
        {
            Subject subject = new Subject();

            var list_ul = WebDriver.FindElements(By.CssSelector("#content > ul"));

            //если есть на странице список категорий
            if (list_ul.Count > 1)
            {
                var list_category = list_ul[1].FindElements(By.CssSelector("li > b"));

                foreach (IWebElement cat in list_category)
                    subject.Category.Add(cat.Text);
            }
            else
            {
                subject.Category.Add(text);
            }

            return subject.Category;
        }

        public void ExitDriver()
        {
            if (WebDriver != null)
            {
                WebDriver.Quit();
            }

            WebDriver = null;

            try
            {
                // ChromeDriver
                //System.Diagnostics.Process.GetProcessesByName("chromedriver").ToList().ForEach(px => px.Kill());

                //убить процесс ChromeDriver
                Process.GetProcessById(_processId).Kill();
                
                //System.Diagnostics.Process.GetProcessesByName("chrome").ToList().ForEach(px => px.Kill());
            }
            catch { }
        }

    }
}
