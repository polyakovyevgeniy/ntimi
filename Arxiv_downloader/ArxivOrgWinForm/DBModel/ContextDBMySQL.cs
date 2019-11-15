using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace ArxivOrgWinForm.DBModel
{   

    class ContextDBMySQL : DbContext
    {
        public DbSet<ArticleModel> Articles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;UserId=root;Password=Developer1982;database=articlestemp;");
        }
    }
}
