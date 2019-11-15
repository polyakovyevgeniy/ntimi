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
    class ArticlesOld
    {
        //IWebDriver _driver;
        //IWebDriver _driver2;

        public ChromeDrv chromeDrv;

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

        public ArticlesOld(int yearStart, int yearStop, int monthStart, int monthStop, int index, bool chkDnldModem, string pathPDF, bool flagPDF, bool flagArticles)
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
            DataAccess dal = new DataAccess();
            SciencesList sciencesList = new SciencesList();

            int year = 0;
            int month = 0;
            int index = 0;

            string y = "";
            string m = "";
            
            try
            {
                chromeDrv = new ChromeDrv();                
            }
            catch (Exception e)
            {
                chromeDrv.ExitDriver();
                contextSync.Send(OnGetArticles_Exception, "Error : " + e.Message);
            }  

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
                            //ссылка на страницу статьи (0704.0001)
                            article.Article_Source = articleSource = string.Format("{0}{1:d2}{2:d2}.{3:d5}",
                                                                    _pathFirst, int.Parse(year.ToString().Substring(2, 2)), month, index);
                            //вывести в textBox
                            contextSync.Send(OnGetSource, article.Article_Source);

                            chromeDrv.ChromeGoToURL(article.Article_Source);                             

                            //если нет такой страницы, переходим в следующий месяц
                            if (chromeDrv.IsPageNotFound())
                            {
                                //вывести в textBox и записать в лог
                                contextSync.Send(OnGetSource_Exception, "Page not found : " + article.Article_Source);
                                //для следующего месяца начать индекс
                                _indexStart = 1;
                                break;
                            }

                            //id записи статьи                           
                            article.Id = 0;

                            //данные со страницы статьи
                            List<string> lstArticleInfo = chromeDrv.GetArticleInfo();
                            _count = 0; //сбросить счетчик перезагрузки модема если нет исключения "no such element" 

                            //запретить прерывание циклов
                            bool flagReturn = false;

                            List<Science> lstSciences = sciencesList.GetSciencesList();

                            article.Science = "";
                            article.Subject = "";
                            article.Category = "";

                            Science scn2;
                            Subject sb2;
                            string cat2;
                            Match matchCat;
                            Match matchCat2;
                            string matchStr;

                            //получить Науку, Предмет и Категорию
                            foreach (Science scn in lstSciences)
                            {
                                foreach (Subject sb in scn.subjects)
                                {
                                    foreach (string cat in sb.Category)
                                    {
                                        //получить аббревиатуру категории
                                        //Regex regexCat = new Regex(@"\S+\.\S+");
                                        Regex regexCat = new Regex(@"\(\S+\.\S+\)");
                                        matchCat = regexCat.Match(lstArticleInfo[0]);
                                        //категория без аббревиатуры
                                        string catStr = lstArticleInfo[0].Substring(0, lstArticleInfo[0].Length - matchCat.Length).Trim();

                                        //ВТОРОЙ СЛУЧАЙ
                                        //получить аббревиатуру категории
                                        Regex regexCat2 = new Regex(@"\(\S+\-\S+\)");                                        
                                        matchCat2 = regexCat2.Match(lstArticleInfo[0]);
                                        //категория без аббревиатуры
                                        string catStr2 = lstArticleInfo[0].Substring(0, lstArticleInfo[0].Length - matchCat2.Length).Trim();

                                        if (matchCat.Value != "")
                                        {
                                            //убрать скобки
                                            matchStr = matchCat.Value.Substring(1, matchCat.Length - 2);

                                            //если содержит аббревиатуру в названии категории
                                            if (cat.Contains(matchStr))
                                            {
                                                //получить категорию предмета
                                                article.Category = catStr;//cat.Substring(matchStr.Length + 2, cat.Length - matchStr.Length - 2).Trim();

                                                //получить предмет
                                                article.Subject = sb.Name;

                                                //получить науку
                                                article.Science = scn.Name;

                                                flagReturn = true;                                                
                                            }
                                        }
                                        else
                                        {
                                            if (matchCat2.Value != "")
                                            {

                                                if (cat.Contains(matchCat2.Value))
                                                {
                                                    //получить категорию предмета
                                                    article.Category = catStr2;

                                                    //получить предмет
                                                    article.Subject = sb.Name;

                                                    //получить науку
                                                    article.Science = scn.Name;

                                                    flagReturn = true;
                                                }
                                                else
                                                {                                                 

                                                    if (sb.Name.Contains(catStr2))
                                                    {
                                                        //получить категорию предмета
                                                        article.Category = sb.Name;

                                                        //получить предмет
                                                        article.Subject = sb.Name;

                                                        //получить науку
                                                        article.Science = scn.Name;

                                                        flagReturn = true;
                                                    }
                                                }
                                            }
                                        }

                                        scn2 = scn;
                                        sb2 = sb;
                                        cat2 = cat;

                                        if (flagReturn) break;
                                    }

                                    if (flagReturn) break;
                                }

                                if (flagReturn) break;
                            }

                            ////заголовок
                            article.Title = lstArticleInfo[1];

                            ////авторы                            
                            article.Autors = lstArticleInfo[2];                            

                            ////Цитата
                            article.Quotation = lstArticleInfo[3];
                                                                                     
                            string strDate = lstArticleInfo[4];
                            Regex regex = new Regex(@"\d{1,2}\s\D{3}\s\d{4}\s\d{2}\W\d{2}\W\d{2}"); // шаблон даты публикации
                            Match match = regex.Match(strDate);   //выбрать соответствие шаблону 
                            ////дата публикации
                            article.Publication_Date = DateTime.Parse(match.Value);
                            
                            ////ссылка на документ
                            article.Document_Source = lstArticleInfo[5];

                            //дата записи
                            article.Recording_Date = DateTime.Now;

                            //имя ФАЙЛА
                            Regex regexFile = new Regex(@"\d{4}\W\d{4,5}\W(pdf)");
                            Match matchFile = regexFile.Match(article.Document_Source);

                            regex = new Regex(@"(pdf)\/\d{4}");
                            match = regex.Match(article.Document_Source);

                            //если корректная ссылка на файл документа
                            if (match.Value != "")
                            {
                                //папка ГОД
                                y = match.Value.Substring(4, 2);

                                //папка МЕСЯЦ
                                m = match.Value.Substring(6, 2);

                                //локальный путь к сохраняемому файлу
                                article.Document_Local = /*@"\" + y + */@"\ArxivOrg\" + matchFile.Value;                                
                            }
                            else
                            {
                                //нет на сайте файла PDF
                                article.Document_Local = "";
                                article.Document_Source = "";
                            }

                            if (_flagPDF) //разрешить сохранять PDF
                            {                            
                                
                                //ссылка на папку                            
                                DirectoryInfo dir = new DirectoryInfo(_pathPDF + @"\ArxivOrg\");

                                if (!dir.Exists && _pathPDF != string.Empty)
                                {
                                    //создать папку
                                    dir.Create();
                                }

                                string document_Local = _pathPDF + article.Document_Local;

                                try
                                {
                                    //загрузить файл статьи
                                    using (var client = new WebClient())
                                    {
                                        client.Headers.Add("User-Agent: Other");
                                        client.DownloadFile(article.Document_Source, document_Local);

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
                                }
                            }                        
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
