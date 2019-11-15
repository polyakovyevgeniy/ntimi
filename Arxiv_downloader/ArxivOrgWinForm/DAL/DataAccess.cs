using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArxivOrgWinForm.DBModel;

namespace ArxivOrgWinForm.DAL
{
    public class DataAccess
    {
        public void WriteDBSQL(ArticleModel article)
        {
            using (ContextDBMySQL context = new ContextDBMySQL())
            {
                //обязательно проверяется наличие БД (если ее нет, то пересоздается)
                context.Database.EnsureCreated();
                
                context.Articles.Add(article);
                
                context.SaveChanges();
            }
        }

    }
}
