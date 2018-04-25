using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TDLib.UIs.Forms;
using MillionHero.Services;
using MillionHero.Definitions;
using MillionHero.Services.Ocrs;
using MillionHero.Services.QuestionAnalyzers;
using TDLib.Utils;
using TDLib.UIs.Extensions;
using System.Runtime.InteropServices;

namespace MillionHero.UIs
{
    public partial class FrmMain : FrmBase
    {
        private string _selectedImgFileInDebug = null;
        private BaiduOcr _baiduOcr = null;
        private MillionHeroService _services = null;
        private EasySearchService _easySearchService = null;
        private Stopwatch _sw = new Stopwatch();
        private TextBox _questionTextBox = null;
        private TextBox[] _optionTextBoxs = null;
        private HotKeyManager _hotkeyManager = null;
        private IQuestionAnalyzer _questionAnalyzer = null;

        public FrmMain()
        {
            InitializeComponent();
            base.LoadIconFromRes("app.ico");
            // 初始化百度OCR
            _baiduOcr = new BaiduOcr(AppGlobals.BaiduApiKey, AppGlobals.BaiduSecretKey);
            // 初始化MillionHeroService
            _services = new MillionHeroService(_baiduOcr);
            // 初始化EasySearchService
            _easySearchService = new EasySearchService();
            // 初始化HotKeyManager
            _hotkeyManager = new HotKeyManager();
            _hotkeyManager.Register(Keys.F3, TDLib.Utils.Win32.UnsafeNativeMethods.Consts.ModifierKeys.None);
            _hotkeyManager.KeyPressed += new EventHandler<HotKeyManager.KeyPressedEventArgs>(_hotkeyManager_KeyPressed);
            // 初始化变量
            _questionTextBox = this.textBox1;
            _optionTextBoxs = new TextBox[] { 
                this.textBox2,
                this.textBox3,
                this.textBox4
            };
            // 初始化控件状态
            this.toolStripComboBox1.SelectedIndex = 0;
            this.toolStripComboBox2.SelectedIndex = 0;
            if (AppGlobals.IsDebug)
            {
                this.Text += "【调试模式】";
            }
            this.notifyIcon1.Icon = this.Icon;
            this.notifyIcon1.Text = this.Text;
            // 初始化简单搜索助手
            Dictionary<string, string> easySearchFiles = new Dictionary<string, string>();
            easySearchFiles.Add("百万英雄专场", EasySearchService.EasySearch_BaiWanYingXiong_HtmlFile);
            easySearchFiles.Add("冲顶大会专场", EasySearchService.EasySearch_ChongDingDaHui_HtmlFile);
            easySearchFiles.Add("花椒直播专场", EasySearchService.EasySearch_HuaJiaoZhiBo_HtmlFile);
            easySearchFiles.Add("芝士超人专场", EasySearchService.EasySearch_ZhiShiChaoRen_HtmlFile);
            foreach (var item in easySearchFiles)
            {
                string text = item.Key;
                string filename = item.Value;
                var dropdownItem = this.toolStripSplitButtonEasySearch.DropDownItems.Add(text, null, (o, args) => {
                    ToolStripItem tsi = o as ToolStripItem;
                    Process.Start(tsi.Tag.ToString());
                });
                dropdownItem.Tag = filename;
            }
            // 初始化UC助手
            Dictionary<string, string> ucFiles = new Dictionary<string, string>();
            // http://answer.sm.cn/answer/index?activity=million#/million
            // http://answer.sm.cn/answer/index?activity=million#/match/million
            ucFiles.Add("百万英雄专场", "http://answer.sm.cn/answer/index?activity=million#/match/million");
            foreach (var item in ucFiles)
            {
                string text = item.Key;
                string filename = item.Value;
                var dropdownItem = this.toolStripSplitButtonUC.DropDownItems.Add(text, null, (o, args) =>
                {
                    ToolStripItem tsi = o as ToolStripItem;
                    Process.Start(tsi.Tag.ToString());
                });
                dropdownItem.Tag = filename;
            }
            // 绑定调试模式调整事件
            AppGlobals.DebugModeChanged += (o, args) => {
                this.Text = "百万英雄助手";
                if (AppGlobals.IsDebug)
                {
                    this.Text += "【调试模式】";
                }
            };
            // 初始化百度OCR授权，为了解决第一次太慢的问题
            RunTaskThread(() => {
                try
                {
                    _baiduOcr.Ocr(new byte[] { 0x01, 0x02 });
                }
                catch (Exception ex)
                {
                }
            });
        }

        void _hotkeyManager_KeyPressed(object sender, HotKeyManager.KeyPressedEventArgs e)
        {
            if (e.HotKey.Key == Keys.F3 && e.HotKey.Modifiers == TDLib.Utils.Win32.UnsafeNativeMethods.Consts.ModifierKeys.None)
            {
                toolStripButton1_Click(null, null);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _services.KillOtherAdbProcesses();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (AppGlobals.IsDebug)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Multiselect = false;
                ofd.Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*";
                ofd.FilterIndex = 1;
                ofd.InitialDirectory = Application.StartupPath + "\\captures";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedImgFileInDebug = ofd.FileName;
                }
                else
                {
                    return;
                }
            }

            _sw.Reset();
            _sw.Start();

            for (int i = 0; i < this.Controls.Count; i++)
            {
                if (this.Controls[i] is TextBox)
                {
                    (this.Controls[i] as TextBox).Clear();
                    (this.Controls[i] as TextBox).BackColor = Color.White;
                }

                if (this.Controls[i] is RichTextBox)
                {
                    (this.Controls[i] as RichTextBox).Clear();
                    (this.Controls[i] as RichTextBox).BackColor = Color.White;
                }
            }

            this.toolStripButton1.Enabled = false;
            this.toolStripButton1.Text = "正在识别";
            this.toolStripStatusLabel1.Text = "正在识别...";
            this.toolStripStatusLabel1.ForeColor = Color.Black;
            this.toolStripComboBox1.Enabled = false;
            this.toolStripComboBox2.Enabled = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _sw.Stop();
            this.toolStripButton1.Enabled = true;
            this.toolStripButton1.Text = "开始识别(F3)";
            this.toolStripLabel1.Text = "用时:" + _sw.Elapsed.TotalSeconds.ToString("0.000") + "秒";
            this.toolStripComboBox1.Enabled = true;
            this.toolStripComboBox2.Enabled = true;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            try
            {
                string imgFile = "";
                if (AppGlobals.IsDebug)
                {
                    imgFile = _selectedImgFileInDebug;
                }
                else
                {
                    imgFile = _services.CaptureMobileScreen();
                };
                var subject = _services.AnalyseQuestion(imgFile, 2);
                worker.ReportProgress(1, subject);
                Answer answer = _services.QueryAnswer(subject, _questionAnalyzer, 2);
                worker.ReportProgress(1, answer);
                if(answer.BestOption==-1)
                {
                    throw new Exception("未找到最佳答案");
                }
                worker.ReportProgress(1, "识别完成!");
            }
            catch (Exception ex)
            {
                worker.ReportProgress(1,"识别失败：" + ex.Message);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is Subject)
            {
                var q = e.UserState as Subject;
                try
                {
                    _questionTextBox.Text = q.Question;
                    for (int i = 0; i < q.Options.Count; i++)
			        {
                        var opt = q.Options[i];
                        _optionTextBoxs[i].Text = opt;
			        }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (e.UserState is Answer)
            {
                var a = e.UserState as Answer;
                try
                {
                    // 显示提示信息
                    this.richTextBox1.Text = string.Empty;
                    if (a.Tips.Count > 0)
                    {
                        string tip = a.Tips[0];
                        this.richTextBox1.Text = tip;
                        var opts = a.Subject.GetFixedOptions();
                        for (int i = 0; i < opts.Count; i++)
                        {
                            var opt = opts[i];
                            if (opt == null || opt.Length == 0)
                                continue;

                            int index = tip.IndexOf(opt);
                            if (index == -1)
                                continue;
                            this.richTextBox1.Select(index, opt.Length);
                            this.richTextBox1.SelectionColor = Color.Red;
                        }
                        this.richTextBox1.Select(tip.Length, 1);
                    }
                    // 标注比例
                    for (int i = 0; i < a.OptionRate.Count; i++)
			        {
                        // 目前只支持3个选项的情况
                        if (i > 2)
                            break;
                        var rate = a.OptionRate[i];
                        _optionTextBoxs[i].Text += "(" + rate + "%)";
			        }
                    // 高亮
                    if (a.BestOption != -1)
                    {
                        _optionTextBoxs[a.BestOption].BackColor = Color.FromArgb(40, 193, 225);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (e.UserState is string)
            {
                string info = e.UserState.ToString();
                if (info.Contains("失败"))
                {
                    this.toolStripStatusLabel1.ForeColor = Color.Red;
                }
                else
                {
                    this.toolStripStatusLabel1.ForeColor = Color.Green;
                }
                this.toolStripStatusLabel1.Text = info;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
            if (this.TopMost)
            {
                this.toolStripButton2.Text = "取消置顶";
            }
            else
            {
                this.toolStripButton2.Text = "置顶";
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.toolStripComboBox1.SelectedIndex == 0)
            {
                MillionHeroService.SetQuestionAppParams(AppGlobals.BaiWanYingXiongs[0]);
            }
            else
            {
                MillionHeroService.SetQuestionAppParams(AppGlobals.ZhiShiChaoRens[0]);
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MsgBox.Question("确定要退出" + this.Text + "?", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            try
            {
                _hotkeyManager.Dispose();
            }
            catch (Exception ex)
            {
                
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            try
            {
                _easySearchService.UpdateHtmlFiles();
                MsgBox.Info("更新成功!");
            }
            catch (Exception ex)
            {
                MsgBox.Error("更新失败，" + ex.Message);
            }
        }

        private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.toolStripComboBox2.SelectedIndex == 0)
            {
                _questionAnalyzer = new OptionHappenTimesAnalyzer(); 
            }
            else
            {
                _questionAnalyzer = new PMIAnalyzer(); 
            }
        }

        /// <summary>
        /// “设置”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripStatusLabel8_Click(object sender, EventArgs e)
        {
            var frm = new FrmSetting();
            frm.StartPosition = FormStartPosition.CenterScreen;
            frm.TopMost = true;
            frm.Owner = this;
            frm.Show();
        }

        private void toolStripSplitButtonEasySearch_ButtonClick(object sender, EventArgs e)
        {
            ToolStripSplitButton btn = sender as ToolStripSplitButton;
            btn.ShowDropDown();
        }

        private void toolStripStatusLabelAutoMode_Click(object sender, EventArgs e)
        {
            var frm = new FrmAuto();
            this.Hide();
            frm.ShowDialog();
            frm.Dispose();
            this.Show();
        }
    }
}
