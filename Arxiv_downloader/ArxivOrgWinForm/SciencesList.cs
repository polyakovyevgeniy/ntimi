using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArxivOrgWinForm.Settings;
using OpenQA.Selenium;

namespace ArxivOrgWinForm
{
    public class SciencesList
    {
        ChromeDrv chromeDrv;
        ChromeDrv chromeDrv2;
        List<Science> scienses;

        public SciencesList()
        {
            chromeDrv = new ChromeDrv();
            chromeDrv2 = new ChromeDrv();
            CreateSciencesList();

            chromeDrv.ExitDriver();
            chromeDrv2.ExitDriver();
        }

        private void CreateSciencesList()
        {            
            Scien scien2 = new Scien();

            //если есть XML файл с категориями - считываем
            if (scien2.ReadXml())
            {
                scienses = scien2.Fields.Sciences;
            }
            else //иначе создаем XML файл с категориями
            {
                //получить актуальные перечни наук, предметов и категорий
                string archive = "https://arxiv.org/archive/";
                chromeDrv.ChromeGoToURL(archive);

                //из первого ul выбрать все li (c аббревиатурой) и ссылки 
                Dictionary<string, IWebElement> lstElem_a = chromeDrv.GetLstElem_a();

                List<Subject> subjects = new List<Subject>();

                foreach (KeyValuePair<string, IWebElement> el in lstElem_a)
                {
                    Subject subject = new Subject();

                    //перейти по ссылке в атрибуте "href"
                    chromeDrv2.ChromeGoToURL(el.Value.GetAttribute("href"));

                    //получить название категории
                    subject.Category = chromeDrv2.GetCategory(el.Key);

                    //получить название предмета
                    subject.Name = el.Value.Text;

                    //список предметов
                    subjects.Add(subject);
                }

                scienses = new List<Science>();
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

                Scien scien = new Scien();
                scien.Fields.Sciences = scienses;
                scien.WriteXml();
            }
        }

        public List<Science> GetSciencesList()
        {      
            return scienses;
        }
    }
}
