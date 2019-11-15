using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Media;
using System.Threading;
using ArxivOrgWinForm.Settings;

namespace ArxivOrgWinForm
{
    public partial class MainForm : Form
    {
        Articles articles;
        ArticlesOld articlesOld;
        SynchronizationContext _contextSync;
        Thread thread;
        Prop prop;
        Log log;

        SoundPlayer sp;

        int _yearStart, _yearStop; //года
        int _monthStart, _monthStop; // месяцы
        //int _index;

        List<Control> lstControls;
        
        bool _cancelled = true;

        public MainForm()
        {
            InitializeComponent();

            prop = new Prop(); //структура для settings.xml
            log = new Log();

            sp = new SoundPlayer();
            sp.SoundLocation = Environment.CurrentDirectory + @"\danger.wav";
            sp.Load();

            lstControls = new List<Control>();

            lstControls.Add(rdBtnUpTo07);
            lstControls.Add(rdBtnFrom07);
            lstControls.Add(cmbBoxMonthStart);
            lstControls.Add(cmbBoxMonthStop);
            lstControls.Add(cmbBoxYearStart);
            lstControls.Add(cmbBoxYearStop);
            lstControls.Add(txBxStartIndex);
            lstControls.Add(chkBxDnldModem);
            lstControls.Add(btnBrowse);
            lstControls.Add(chkBxGetPDF);
            lstControls.Add(chkBxGetArticles);


        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _contextSync = SynchronizationContext.Current;

            btn_Stop.Enabled = false;
            rdBtnFrom07.Checked = true;

            txBxSavePDF_Enabled();
            
            LoadSettings();
            log.LogWrite("Приложение запущено\n\n");
        }

        private void LoadSettings()
        {            
            //если нет xml-файл с параметрами (создаем новый)
            if (!prop.ReadXml())
            {
                MessageBox.Show("No initial configuration data available ");
            }

            cmbBoxYearStart.DataSource = DateList.GetYears();

            cmbBoxYearStop.DataSource = DateList.GetYears();

            cmbBoxMonthStart.DataSource = DateList.GetMonths();

            cmbBoxMonthStop.DataSource = DateList.GetMonths();

            //получить параметры
            cmbBoxYearStart.SelectedItem = prop.Fields.YearStart;
            cmbBoxYearStop.SelectedItem = prop.Fields.YearStop;
            cmbBoxMonthStart.SelectedItem = _monthStart = prop.Fields.MonthStart;
            cmbBoxMonthStop.SelectedItem = _monthStop = prop.Fields.MonthStop;
            //_index = prop.Fields.Index;
            txBxStartIndex.Text = prop.Fields.Index.ToString();
            txBxSavePDF.Text = prop.Fields.PathPDF;
            chkBxGetPDF.Checked = prop.Fields.GetPDF;
            chkBxGetArticles.Checked = false;//prop.Fields.GetArticles;

            if (prop.Fields.rdBtnUpTo07)
                rdBtnUpTo07.Checked = true;
            else
                rdBtnFrom07.Checked = true;
        }

        private void SaveSettings(List<int> lst)
        {           
            cmbBoxYearStart.SelectedItem = prop.Fields.YearStart = _yearStart = lst[0];
            prop.Fields.YearStop = _yearStop;
            cmbBoxMonthStart.SelectedItem = prop.Fields.MonthStart = _monthStart = lst[1];
            prop.Fields.MonthStop = _monthStop;
            prop.Fields.Index = lst[2];
            txBxStartIndex.Text = lst[2].ToString();
            prop.Fields.PathPDF = txBxSavePDF.Text;
            prop.Fields.GetPDF = chkBxGetPDF.Checked;
            prop.Fields.GetArticles = chkBxGetArticles.Checked;

            if (rdBtnUpTo07.Checked)
                prop.Fields.rdBtnUpTo07 = true;
            else
                prop.Fields.rdBtnUpTo07 = false;

            //записать параметры в xml- файл
            prop.WriteXml();
        }

        private void EnabledTools()
        {
            if (btn_Start.Enabled)
            {
                //rdBtnUpTo07.Enabled = true;
                //rdBtnFrom07.Enabled = true;
                //cmbBoxMonthStart.Enabled = true;
                //cmbBoxMonthStop.Enabled = true;
                //cmbBoxYearStart.Enabled = true;
                //cmbBoxYearStop.Enabled = true;
                //txBxStartIndex.Enabled = true;
                //chkBxDnldModem.Enabled = true;                
                //btnBrowse.Enabled = true;
                //chkBxGetPDF.Enabled = true;
                //chkBxGetArticles.Enabled = true;

                foreach (Control control in lstControls)
                    control.Enabled = true;

                chkBxGetArticles.Enabled = false;
                cmbBoxYearStart.Enabled = false;
                cmbBoxMonthStart.Enabled = false;

                txBxSavePDF_Enabled();                
            }
            else
            {
                //rdBtnUpTo07.Enabled = false;
                //rdBtnFrom07.Enabled = false;
                //cmbBoxMonthStart.Enabled = false;
                //cmbBoxMonthStop.Enabled = false;
                //cmbBoxYearStart.Enabled = false;
                //cmbBoxYearStop.Enabled = false;
                //txBxStartIndex.Enabled = false;
                //chkBxDnldModem.Enabled = false;
                //txBxSavePDF.Enabled = false;
                //btnBrowse.Enabled = false;
                //chkBxGetPDF.Enabled = false;
                //chkBxGetArticles.Enabled = false;

                foreach (Control control in lstControls)
                    control.Enabled = false;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (chkBxGetPDF.Checked || chkBxGetArticles.Checked)
            {
                btn_Start.Enabled = false;
                _cancelled = false;

                Start_Try();
            }
            else
            {
                MessageBox.Show("Put a tick in the checkbox \"Get PDF\" or \"Get articles\"", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        //private void StartUpTo07_Try()
        //{

        //    btn_Stop.Enabled = true;
        //}

        private void Start_Try()
        {
            if (chkBxGetPDF.Checked)
            {
                if (new DirectoryInfo(txBxSavePDF.Text != string.Empty ? txBxSavePDF.Text : "_").Exists)
                {
                    StartEx();
                }
                else
                {
                    MessageBox.Show("Specify the save path", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    btn_Start.Enabled = true;
                    _cancelled = true;
                    //активировать поля на форме
                    EnabledTools();
                }
            }
            else
            {
                txBxSavePDF.Text = string.Empty;
                StartEx();
            }     
        }

        private void StartEx()
        {
            if (rdBtnUpTo07.Checked)
            {
                StartUpTo07();
            }
            else
            {
                StartFrom07();
            }
        }

        private void StartUpTo07()
        {
            int result;

            //до 01.04.2007
            if (
                (((int)cmbBoxYearStart.SelectedItem == 2007 && (int)cmbBoxMonthStart.SelectedItem >= 4) || (int)cmbBoxYearStart.SelectedItem > 2007)
                &&
                (int)cmbBoxYearStart.SelectedItem <= (int)cmbBoxYearStop.SelectedItem
                &&
                int.TryParse(txBxStartIndex.Text, out result)
                )
            {
                articlesOld = new ArticlesOld(_yearStart, _yearStop, _monthStart, _monthStop, int.Parse(txBxStartIndex.Text), chkBxDnldModem.Checked,
                                        txBxSavePDF.Text, chkBxGetPDF.Checked, chkBxGetArticles.Checked);
                articlesOld.GetArticlesException += Articles_GetArticlesException;
                articlesOld.GetSource_Exception += Articles_GetSourceException;
                articlesOld.GetSource += Articles_GetSource;
                articlesOld.GetSettings += GetSettings_Param;
                articlesOld.GetArticles_Cancel += Articles_Cancel;

                thread = new Thread(new ParameterizedThreadStart(articlesOld.GetSubjects));
                thread.Start(_contextSync);

                btn_Stop.Enabled = true;
                //деактивировать поля на форме
                EnabledTools();

                log.LogWrite("Start hoarding\n");
                txBxInfo.Text = "Start hoarding\n";
            }
            else
            {
                MessageBox.Show("Enter the correct data!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                btn_Start.Enabled = true;
                _cancelled = true;
                //активировать поля на форме
                EnabledTools();
            }
        }

        private void StartFrom07()
        {
            int result;

            //c 01.04.2007
            if (
                (((int)cmbBoxYearStart.SelectedItem == 2007 && (int)cmbBoxMonthStart.SelectedItem >= 4) || (int)cmbBoxYearStart.SelectedItem > 2007)
                &&
                (int)cmbBoxYearStart.SelectedItem <= (int)cmbBoxYearStop.SelectedItem
                &&
                int.TryParse(txBxStartIndex.Text, out result)                
                )
            {
                articles = new Articles(_yearStart, _yearStop, _monthStart, _monthStop, int.Parse(txBxStartIndex.Text), chkBxDnldModem.Checked,
                                        txBxSavePDF.Text, chkBxGetPDF.Checked, chkBxGetArticles.Checked);
                articles.GetArticlesException += Articles_GetArticlesException;
                articles.GetSource_Exception += Articles_GetSourceException;
                articles.GetSource += Articles_GetSource;
                articles.GetSettings += GetSettings_Param;
                articles.GetArticles_Cancel += Articles_Cancel;

                thread = new Thread(new ParameterizedThreadStart(articles.GetSubjects));
                thread.Start(_contextSync);

                btn_Stop.Enabled = true;
                //деактивировать поля на форме
                EnabledTools();

                log.LogWrite("Start hoarding\n");
                txBxInfo.Text = "Start hoarding\n";
            }
            else
            {
                MessageBox.Show("Enter the correct data!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                btn_Start.Enabled = true;
                _cancelled = true;
                //активировать поля на форме
                EnabledTools();
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (rdBtnUpTo07.Checked)
            {
                if (articlesOld != null)
                    articlesOld.Cancel();                
            }
            else
            {
                if (articles != null)
                    articles.Cancel();                     
            }

            btn_Stop.Enabled = false;
        }
        
        private void Articles_GetArticlesException(string info)
        {            
            MessageBox.Show("Error : " + info);
            log.LogWrite("Error : " + info + "\n");            
        }
        
        private void Articles_GetSourceException(string source)
        {
            if (chkBxAlarm.Checked)
            {
                sp.Stop();
                sp.Play();
            }

            txBxInfo.Text = source;
            log.LogWrite(source + "\n");            
        }
        
        private void Articles_GetSource(string sourceInfo)
        {
            txBxInfo.Text = sourceInfo;
        }

        private void GetSettings_Param(List<int> lst)
        {
            SaveSettings(lst);                     
            log.LogWrite("Stop hoarding\n");
            txBxInfo.Text = "Stop hoarding";

            btn_Start.Enabled = true;
            //активировать поля на форме
            EnabledTools();
        }
        private void CmbBoxYearStart_SelectedIndexChanged(object sender, EventArgs e)
        {
            _yearStart = int.Parse(cmbBoxYearStart.SelectedItem.ToString());
        }

        private void CmbBoxYearStop_SelectedIndexChanged(object sender, EventArgs e)
        {
            _yearStop = int.Parse(cmbBoxYearStop.SelectedItem.ToString());
        }

        private void CmbBoxMonthStart_SelectedIndexChanged(object sender, EventArgs e)
        {
            _monthStart = (int)cmbBoxMonthStart.SelectedItem;
        }

        private void CmbBoxMonthStop_SelectedIndexChanged(object sender, EventArgs e)
        {
            _monthStop = (int)cmbBoxMonthStop.SelectedItem;
        }

        private void ChkBxGetPDF_CheckedChanged(object sender, EventArgs e)
        {
            txBxSavePDF_Enabled();
        }

        private void txBxSavePDF_Enabled()
        {
            if (chkBxGetPDF.Checked)
                txBxSavePDF.Enabled = true;
            else
                txBxSavePDF.Enabled = false;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            txBxSavePDF.Text = folderBrowserDialog1.SelectedPath;
        }

        private void Articles_Cancel(bool cancel)
        {
            //если создан экземпляр
            if (articles != null)
                articles._browser.ExitDriver(); //завершить все процессы ChromeDriver

            //если создан экземпляр
            if (articlesOld != null)
                articlesOld.chromeDrv.ExitDriver(); //завершить все процессы ChromeDriver

            _cancelled = cancel;
        }
        
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(_cancelled)
            {
                if (MessageBox.Show("Do you want to quit the app??", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    

                    e.Cancel = false;
                }
                else
                    e.Cancel = true;
            }
            else
            {
                if (btn_Stop.Enabled == false)
                {
                    MessageBox.Show("Дождитесь завершения выполнения операции.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
                else
                {
                    MessageBox.Show("Для выхода из приложения завершите выполнение операции.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        }
    }
}
