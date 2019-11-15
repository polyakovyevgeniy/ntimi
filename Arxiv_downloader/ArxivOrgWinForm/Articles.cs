using System;
using System.Collections.Generic;

using System.Threading;
using OpenQA.Selenium;
using ArxivOrgWinForm.DAL;
using ArxivOrgWinForm.DBModel;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using ArxivOrgWinForm.Settings;

namespace ArxivOrgWinForm
{
    public class Articles
    {
        IWebDriver _driver;
        IWebDriver _driver2;
        public ChromeDrv _browser;

        string _pathFirst = "https://arxiv.org/abs/"; 
        string _pathPDF = @"";
        //string _path = string.Empty;   //0704.0001

        int _yearStart, _yearStop; //года
        int _monthStart, _monthStop; // месяцы
        int _indexStart; //индекс документа

        bool _chkDnldModem; //разрешить перезагрузку модема     
        bool _flagPDF; //разрешить загрузку PDF
        bool _flagArticles; //разрешить получать данные со страницы

        int _count = 0; // количество попыток перехода по ссылке до перезагрузки модема

        List<int> lstSettings;  //список параметров для сохранения

        string articleSource;

        bool _cancel = false;
        bool flagReboot = false;       

        public Articles(int yearStart, int yearStop, int monthStart, int monthStop, int index, bool chkDnldModem, string pathPDF, bool flagPDF, bool flagArticles)
        {
            _yearStart = yearStart;
            _yearStop = yearStop;
            _monthStart = monthStart;
            _monthStop = monthStop;
            _indexStart = index;
            _chkDnldModem = chkDnldModem;
            _pathPDF = pathPDF;
            _flagPDF = flagPDF;
            _flagArticles = flagArticles;
        }

        public void Cancel()
        {
            _cancel = true;
        }

        public void GetSubjects(object context)
        {
            SynchronizationContext contextSync = (SynchronizationContext)context;

            ArticleModel article = new ArticleModel();
            DataAccess dal = new DataAccess(); ; 

            int year = 0;
            int month = 0;
            int index = 0;

            string y = "";
            string m = "";

            //////////////////////////////////////////////////////////////////////
            try
            {
                _browser = new ChromeDrv();
                //_driver = _browser.GetWebDriver();
            }
            catch(Exception e)
            {
                _browser.ExitDriver();
                contextSync.Send(OnGetArticles_Exception, "Error : " + e.Message);
            }

            //////////////////////////////////////////////////////////////////////
            Scien scien2 = new Scien();
            List<Science> scienses2 = new List<Science>();
            if (scien2.ReadXml())
            {
                scienses2 = scien2.Fields.Sciences;
            }
            //////////////////////////////////////////////////////////////////////

            //годы с 2007 по 2019
            for (year = _yearStart; year <= _yearStop; year++)
            {
                //месяцы
                for (month = _monthStart; month <= _monthStop; month++)
                {
                    //индекс статьи
                    for (index = _indexStart; ; index++)
                    {          
                        //если запрос на перезагрузку модема
                        if (flagReboot && _chkDnldModem)
                        {
                            contextSync.Send(OnGetSource_Exception, RebootModem());  
                            flagReboot = false;
                        }

                        try
                        {
                            //0704.0001
                            article.Article_Source = articleSource = string.Format("{0}{1:d2}{2:d2}.{3:d5}", 
                                                                    _pathFirst, int.Parse(year.ToString().Substring(2,2)), month, index);
                            //вывести в textBox
                            contextSync.Send(OnGetSource, article.Article_Source);

                            _driver.Navigate().GoToUrl(article.Article_Source);
                            

                            //---------------------------------------------------

                            /*
                            string archive = "https://arxiv.org/archive/";
                            _driver.Navigate().GoToUrl(archive);
                            
			                //из первого ul выбрать все li с ссылками 
                            var list_a = _driver.FindElement(By.CssSelector("#content > ul")).FindElements(By.CssSelector("li > a"));

                            List<IWebElement> listSubject = new List<IWebElement>();
                            foreach (IWebElement el in list_a)
                            {
                                listSubject.Add(el);
                            }

                            //---------------------------------------------------

                            try
                            {                                
                                _driver2 = _browser.GetWebDriver();
                            }
                            catch (Exception e)
                            {
                                contextSync.Send(OnGetArticles_Exception, " Ошибка получения WebDriver. Error : " + e.Message);
                                _browser.ExitDriver();
                                return;
                            }

                            List<Subject> subjects = new List<Subject>();

                            foreach (IWebElement el in listSubject)
                            {           
                                
                                Subject subject = new Subject();                                
                                //название предмета
                                subject.Name = el.Text;

                                //перейти по ссылке в атрибуте "href"
                                _driver2.Navigate().GoToUrl(el.GetAttribute("href"));

                                var list_ul = _driver2.FindElements(By.CssSelector("#content > ul"));

                                //если есть на странице список категорий
                                if (list_ul.Count > 1)
                                {   
                                    var list_category = list_ul[1].FindElements(By.CssSelector("li > b"));                                  

                                    foreach (IWebElement cat in list_category)
                                        subject.Categoty.Add(cat.Text);
                                }
                                else
                                {
                                    subject.Categoty.Add(el.Text);
                                }

                                //список предметов
                                subjects.Add(subject);
                            }

                            //---------------------------------------------------

                            List<Science> scienses = new List<Science>();

                            ScienceDictionary sc = new ScienceDictionary();

                            foreach (Subject subj in subjects)
                            {
                                Science science = null;

                                foreach (KeyValuePair<string, string> kv in sc.subjects)
                                {
                                    if (subj.Name.Contains(kv.Key))
                                    {
                                        science = new Science();
                                        science.Name = kv.Value;
                                        science.subjects.Add(subj);
                                        //article.Science = kv.Value;
                                        //НАПРАВЛЕНИЕ В НАУКЕ !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                        //article.DirectScience = kv.Key;
                                    }
                                }

                                scienses.Add(science);
                            }      


                            //---------------------------------------------------

                            Scien scien = new Scien();
                            scien.Fields.Sciences = scienses;
                            scien.WriteXml();
                            

                            //---------------------------------------------------

                            Scien scien2 = new Scien();
                            List<Science> scienses2 = new List<Science>();

                            if (scien2.ReadXml())
                            {
                                scienses2 = scien2.Fields.Sciences;
                            }

                            //---------------------------------------------------
                            */

                            //если нет такой страницы, переходим в следующий месяц
                            if (PageNotFound())
                            {
                                //вывести в textBox и записать в лог
                                contextSync.Send(OnGetSource_Exception, "Page not found : " + article.Article_Source);
                                //для следующего месяца начать индекс
                                _indexStart = 1;
                                break;
                            }

                            //id записи статьи                           
                            article.Id = 0;                                                      

                            //категория предмета
                            article.Category = _driver.FindElement(By.CssSelector("span.primary-subject")).Text;
                            _count = 0; //сбросить счетчик перезагрузки модема если нет исключения "no such element" 

                            bool flagReturn = false;

                            //наука
                            //string strScience = _driver.FindElement(By.CssSelector("div.subheader>h1")).Text; 
                            
                            //получить Науку, Предмет и Категорию
                            foreach (Science sc in scienses2)
                            {
                                foreach (Subject sb in sc.subjects)
                                {
                                    foreach (string cat in sb.Category)
                                    {
                                        //получить аббревиатуру категории
                                        Regex regexCat = new Regex(@"\S+\.\S+");
                                        Match matchCat = regexCat.Match(cat);

                                        if (matchCat.Value != "")
                                        {
                                            //если содержит аббревиатуру в названии категории
                                            if (article.Category.Contains(matchCat.Value))
                                            {
                                                //выбрать название  категории
                                                string catStr = cat.Substring(matchCat.Length + 2, cat.Length - matchCat.Length - 2);

                                                //получить категорию предмета
                                                article.Category = catStr;

                                                //получить предмет
                                                article.Subject = sb.Name;

                                                //получить науку
                                                article.Science = sc.Name;

                                                flagReturn = true;
                                            }
                                        }

                                        if (flagReturn) break;
                                    }

                                    if (flagReturn) break;
                                }

                                if (flagReturn) break;
                            }

                            //заголовок
                            article.Title = _driver.FindElement(By.CssSelector("#abs > h1")).Text;

                            //авторы                            
                            //article.Autors = _driver.FindElement(By.CssSelector(".authors > a")).Text;
                            var lstElem = _driver.FindElements(By.CssSelector(".authors > a"));
                            article.Autors = "";
                            foreach (IWebElement elem in lstElem)
                                article.Autors += elem.Text + ", ";

                            //дата публикации                              
                            string strDate = _driver.FindElement(By.CssSelector(".submission-history")).Text;                            
                            Regex regex = new Regex(@"\d{1,2}\s\D{3}\s\d{4}\s\d{2}\W\d{2}\W\d{2}"); // шаблон даты публикации
                            Match match = regex.Match(strDate);   //выбрать соответствие шаблону                                              
                            article.Publication_Date = DateTime.Parse(match.Value);

                            //Цитата
                            article.Quotation = _driver.FindElement(By.CssSelector("#abs > blockquote")).Text;

                            //ссылка на документ
                            article.Document_Source = _driver.FindElement(By.CssSelector("div.full-text > ul > li > a")).GetAttribute("href") + ".pdf";                                                        
                            regex = new Regex(@"(pdf)\/\d{4}");
                            match = regex.Match(article.Document_Source);

                            //если корректная ссылка на файл документа
                            if (match.Value != "")
                            {
                                //папка ГОД
                                y = match.Value.Substring(4, 2);

                                //папка МЕСЯЦ
                                m = match.Value.Substring(6, 2);

                                //ссылка на папку                            
                                DirectoryInfo dir = new DirectoryInfo(_pathPDF + @"\" + y + @"\" + m);
                                if (!dir.Exists && _pathPDF != string.Empty)
                                {
                                    //создать папку
                                    dir.Create();
                                }
                                
                                //имя ФАЙЛА
                                Regex regexFile = new Regex(@"\d{4}\W\d{4,5}\W(pdf)");
                                Match matchFile = regexFile.Match(article.Document_Source);

                                //локальный путь к сохраняемому файлу     
                                string document_Local = _pathPDF + @"\" + y + @"\" + m + @"\" + matchFile.Value;
                                article.Document_Local = @"\" + y + @"\" + m + @"\" + matchFile.Value;

                                if (_flagPDF) //разрешить сохранять PDF
                                {
                                    try
                                    {
                                        //загрузить файл статьи
                                        using (var client = new WebClient())
                                        {
                                            client.Headers.Add("User-Agent: Other");
                                            client.DownloadFile(article.Document_Source, document_Local);
                                            //признак загрузки PDF
                                            //article.Download = 1;

                                            _count = 0; //сбросить счетчик перезагрузки модема
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        if (e.Message.Contains("Запрещено")) // если блокировка - перезагрузить модем
                                        {
                                            contextSync.Send(OnGetSource_Exception, "\nUnable to download file. Try REBOOT MODEM (2 min)"
                                                                                    + article.Article_Source + " Error : " + e.Message + "\n");
                                            Thread.Sleep(120000);

                                            flagReboot = true; //перезагрузить модем
                                            _count = 0; //сбросить счетчик перезагрузки модема
                                            //признак загрузки PDF
                                            //article.Download = 0;

                                            index = --index;
                                            if (_cancel) break;

                                            continue;
                                        }
                                        else
                                        {
                                            contextSync.Send(OnGetSource_Exception, "Unable to download file (wait 1 min)"
                                                                                    + article.Article_Source + " Error : " + e.Message);
                                            Thread.Sleep(60000);
                                        }

                                        //article.Document_Source = "";
                                        //article.Document_Local = "";

                                        //признак загрузки PDF
                                        //article.Download = 0;
                                    }
                                }
                                else
                                {
                                    //article.Download = 0;
                                }
                            }
                            else
                            {
                                //нет на сайте файла PDF
                                article.Document_Local = "";
                                article.Document_Source = "";
                                //признак загрузки PDF
                                //article.Download = 0;
                            }                            

                            //дата записи
                            article.Recording_Date = DateTime.Now;                            
                        }
                        catch (Exception e)
                        {     
                            
                            if (e.Message.Contains("no such element") || e.Message.Contains("timed out"))
                            {                                
                                contextSync.Send(OnGetSource_Exception, $"No connection! {article.Article_Source} Error : {e.Message}");
                                
                                Thread.Sleep(10000);
                                
                                _count++;
                                
                                if (_count == 12)
                                {                                    
                                    flagReboot = true; //перезагрузить модем
                                    _count = 0; //сбросить счетчик перезагрузки модема
                                }

                                index = --index;
                                if (_cancel) break;

                                continue;
                            }
                            else
                            {
                                contextSync.Send(OnGetArticles_Exception, $" Ошибка получения данных со страницы. Error : {e.Message}");
                                index = --index;
                                //прерываем работу всех циклов
                                _cancel = true;
                                break;
                            }                            
                        }

                        if (_flagArticles) //разрешить сохранять данные со страницы
                        {
                            try
                            {
                                //сохранить в базу
                                dal.WriteDBSQL(article);
                            }
                            catch (Exception e)
                            {
                                contextSync.Send(OnGetArticles_Exception, " Ошибка сохранения в БД. Error : " + e.Message);

                                //прерываем работу всех циклов
                                _cancel = true;

                                index = --index;
                                break;
                            }
                        }                                               

                        //нажата кнопка Stop или возникло исключение
                        if (_cancel) break;
                    }

                    //нажата кнопка Stop или возникло исключение
                    if (_cancel) break;

                    //установить стартовый месяцев для перехода на следующий год
                    if (month == 12) _monthStart = 1;                    
                }

                //нажата кнопка Stop или возникло исключение
                if (_cancel) break;
            }

            
            _yearStart = year;
            _monthStart = month;
            _indexStart = ++index;

            lstSettings = new List<int>();
            lstSettings.Add(_yearStart);
            lstSettings.Add(_monthStart);
            lstSettings.Add(_indexStart);

            contextSync.Send(OnGetSettings, lstSettings);
            contextSync.Send(OnGetArticles_Cancel, true);

            //завершить все процессы ChromeDriver
            //_browser.ExitDriver();  
        }

        private string RebootModem()
        {
            try
            {
                //перезагрузить модем
                ServerSocket serSoc = new ServerSocket("192.168.1.1", 23);
                return "\nREBOOT MODEM!\n";
            }
            catch (Exception e)
            {
                return "\nОшибка обращения к модему. Error : " + e.Message + "\n";
            }
        }

        private bool PageNotFound()
        {
            try
            {
                //если загрузилась страница "Article xxxx.xxxx not found"                
                if (_driver.FindElements(By.CssSelector("#content > h1")).Count != 0)
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

        public void OnGetArticles_Exception(object ex)
        {
            if (GetArticlesException != null)
                GetArticlesException((string)ex);
        }
        
        private void OnGetSource_Exception(object source)
        {
            if (GetSource_Exception != null)
                GetSource_Exception((string)source);
        }
        
        private void OnGetSource(object sourceInfo)
        {
            if (GetSource != null)
                GetSource((string)sourceInfo);
        }

        private void OnGetSettings(object lst)
        {
            if (GetSettings != null)
                GetSettings((List<int>)lst);
        }
        
        private void OnGetArticles_Cancel(object cancel)
        {
            if (GetArticles_Cancel != null)
                GetArticles_Cancel((bool)cancel);
        }



        public event Action<string> GetArticlesException;
        public event Action<List<int>> GetSettings;
        public event Action<string> GetSource;
        public event Action<string> GetSource_Exception;
        public event Action<bool> GetArticles_Cancel;        
    }
}
