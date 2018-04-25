using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MillionHero.Services;
using TDLib.UIs.Forms;
using TDLib.Utils;
using MillionHero.Definitions;
using MillionHero.Services.AIServices;
using System.Globalization;

namespace MillionHero.UIs
{
    public partial class FrmAuto : FrmBase
    {
        private static ILog _logger = LoggerManager.GetLogger<FrmAuto>();
        private WebBrowser webBrowser1 = new WebBrowser();
        private MillionHeroService _millionHeroService = new MillionHeroService(null);
        private RunningStatus _autoRunningStatus = RunningStatus.Stopped;
        private BackgroundWorker _worker;
        private Dictionary<string, Dictionary<string, IAIService>> _serviceDic = new Dictionary<string, Dictionary<string, IAIService>>();
        private Dictionary<string, QuestionAppType> _appParamsDic = new Dictionary<string, QuestionAppType>();
        private IAIService _aiService = null;
        private QuestionAppType _appType = QuestionAppType.BaiWanYingXiong;
        private bool _isDoSelectWhenConfirmed = true;
        private Dictionary<string, QuestionAppType> _planTimes = new Dictionary<string, QuestionAppType>();
        private HashSet<string> _baiWanYingXiongPlanTimes = new HashSet<string>();
        private HashSet<string> _zhiShiChaoRenPlanTimes = new HashSet<string>();

        public FrmAuto()
        {
            InitializeComponent();
            base.LoadIconFromRes("app.ico");
            this.webBrowser1.Location = new Point(1, 1);
            this.webBrowser1.Size = new Size(30, 30);
            this.webBrowser1.ObjectForScripting = _aiService as BaiDuAIService;

            // 服务
            _serviceDic.Add("百万英雄", new Dictionary<string, IAIService>() { 
                {"UC答题助手", new UCAIService()},
                {"简单搜索", new BaiDuAIService(this.webBrowser1, "简单搜索(百万英雄专场).html") }
            });
            _serviceDic.Add("芝士超人", new Dictionary<string, IAIService>() { 
                {"简单搜索", new BaiDuAIService(this.webBrowser1, "简单搜索(芝士超人专场).html") }
            });
            // 参数
            _appParamsDic.Add("百万英雄", QuestionAppType.BaiWanYingXiong);
            _appParamsDic.Add("芝士超人", QuestionAppType.ZhiShiChaoRen);
            foreach (var item in _serviceDic)
            {
                this.toolStripComboBox1.Items.Add(item.Key);
            }
            this.toolStripComboBox1.SelectedIndex = 0;
            this.toolStripComboBox3.SelectedIndex = 1;
            _baiWanYingXiongPlanTimes.Add("13:00");
            _baiWanYingXiongPlanTimes.Add("14:00");
            _baiWanYingXiongPlanTimes.Add("20:00");
            _baiWanYingXiongPlanTimes.Add("21:00");
            _baiWanYingXiongPlanTimes.Add("22:00");
            _baiWanYingXiongPlanTimes.Add("23:00");

            _zhiShiChaoRenPlanTimes.Add("12:30");
            _zhiShiChaoRenPlanTimes.Add("19:30");
            _zhiShiChaoRenPlanTimes.Add("20:30");
            _zhiShiChaoRenPlanTimes.Add("21:30");
            _zhiShiChaoRenPlanTimes.Add("22:30");
        }

        /// <summary>
        /// “开始自动识别”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            if (_autoRunningStatus == RunningStatus.Running)
            {
                this.toolStripButtonStart.Text = "正在停止自动识别";
                _autoRunningStatus = RunningStatus.Stopping;
                return;
            }
            if (!DoAuto())
            {
                return;
            }

            this.toolStripButtonStartPlan.Enabled = false;
            this.toolStripButtonStart.Text = "停止自动识别";
            _planTimes.Clear();
            if (this.toolStripComboBox1.SelectedIndex == 0)
            {
                foreach (var item in _baiWanYingXiongPlanTimes)
                {
                    _planTimes.Add(item, QuestionAppType.BaiWanYingXiong);
                }
            }
            else
            {
                foreach (var item in _zhiShiChaoRenPlanTimes)
                {
                    _planTimes.Add(item, QuestionAppType.ZhiShiChaoRen);
                }
            }
            _planTimes = _planTimes.OrderBy(s => s.Key).ToDictionary(s => s.Key, s => s.Value);
        }

        /// <summary>
        /// “开始计划任务”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonStartPlan_Click(object sender, EventArgs e)
        {
            if (_autoRunningStatus == RunningStatus.Running)
            {
                this.toolStripButtonStartPlan.Text = "正在停止计划任务";
                _autoRunningStatus = RunningStatus.Stopping;
                return;
            }
            if (!DoAuto())
            {
                return;
            }

            this.toolStripButtonStart.Enabled = false;
            this.toolStripButtonStartPlan.Text = "停止计划任务";
            _planTimes.Clear();
            foreach (var item in _baiWanYingXiongPlanTimes)
            {
                _planTimes.Add(item, QuestionAppType.BaiWanYingXiong);
            }
            foreach (var item in _zhiShiChaoRenPlanTimes)
            {
                _planTimes.Add(item, QuestionAppType.ZhiShiChaoRen);
            }
            _planTimes = _planTimes.OrderBy(s => s.Key).ToDictionary(s => s.Key, s => s.Value);
        }

        private bool DoAuto()
        {
            if (_worker != null)
            {
                MsgBox.Warning("正在停止中，请稍后！");
                return false;
            }

            if (_aiService == null)
            {
                MsgBox.Warning("请先选择【答题大会类型】和【答题助手类型】！");
                return false;
            }
            try
            {
                _aiService.Init();
            }
            catch (Exception ex)
            {
                MsgBox.Error("错误：" + ex.Message);
                worker_RunWorkerCompleted(null, null);
                return false;
            }

            _autoRunningStatus = RunningStatus.Running;
            this.toolStripComboBox1.Enabled = false;
            this.toolStripComboBox2.Enabled = false;
            this.toolStripComboBox3.Enabled = false;

            _worker = new BackgroundWorker();
            _worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            _worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            _worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            _worker.WorkerReportsProgress = true;
            _worker.RunWorkerAsync();

            return true;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DoOutput("已停止！");
            this.toolStripComboBox1.Enabled = true;
            this.toolStripComboBox2.Enabled = true;
            this.toolStripComboBox3.Enabled = true;
            this.toolStripButtonStart.Enabled = true;
            this.toolStripButtonStart.Text = "开始自动识别";
            this.toolStripButtonStartPlan.Enabled = true;
            this.toolStripButtonStartPlan.Text = "开始计划任务";
            _autoRunningStatus = RunningStatus.Stopped;
            _worker = null;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                var info = e.UserState.ToString();
                DoOutput(info);
            }
            else if (e.ProgressPercentage == 2)
            {
                var info = e.UserState.ToString();
                this.textBox2.Text = string.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), info);
            }
            else if (e.ProgressPercentage == 3)
            {
                QuestionAppType appType = (QuestionAppType)e.UserState;
                if (appType == QuestionAppType.BaiWanYingXiong)
                {
                    this.toolStripComboBox1.SelectedIndex = 0;
                }
                else if (appType == QuestionAppType.ZhiShiChaoRen)
                {
                    this.toolStripComboBox1.SelectedIndex = 1;
                }
                else
                {
                    return;
                }
                _aiService.Init();
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(2000);
            BackgroundWorker worker = sender as BackgroundWorker;

            // 计划任务
            worker.ReportProgress(1, "计划任务：");
            foreach (var item in _planTimes)
            {
                worker.ReportProgress(1, item.Key + "->" + item.Value);
            }

            int lastRound = -1;
            string info = string.Empty;
            HashSet<int> restartTimes = new HashSet<int>();
            QuestionAppType lastAppType = QuestionAppType.None;
            string lastTimeKey = null;
            while (_autoRunningStatus == RunningStatus.Running)
            {
                Thread.Sleep(500);

                try
                {
                    if (lastAppType != _appType)
                    {
                        ReloadAppParams(worker);
                        lastAppType = _appType;
                    }
                    string timeKey = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
                    if (_planTimes.ContainsKey(timeKey))
                    {
                        if (lastTimeKey != timeKey)
                        {
                            // 切换app类型
                            worker.ReportProgress(1, "切换应用" + _planTimes[timeKey]);
                            worker.ReportProgress(3, _planTimes[timeKey]);
                            Thread.Sleep(3000);
                            if (lastAppType != _appType)
                            {
                                // 重新载入应用参数
                                worker.ReportProgress(1, "重新载入应用参数" + _planTimes[timeKey]);
                                ReloadAppParams(null);
                                lastAppType = _appType;
                            }
                            Thread.Sleep(3000);
                            // 重启app
                            worker.ReportProgress(1, "重启应用" + _planTimes[timeKey]);
                            RestartApp();

                            lastTimeKey = timeKey;
                        }
                    }
                    // 获取答案
                    var result = _aiService.Fetch();
                    if (result != null && result.Options.Count != 0)
                    {
                        if (lastRound == result.Round)
                        {
                            worker.ReportProgress(2, "等待下一题");
                            continue;
                        }
                        else
                        {
                            if (_isDoSelectWhenConfirmed && result.Confirmed == false)
                            {
                                continue;
                            }
                            lastRound = result.Round;
                        }
                        // 显示题目
                        info = "问题：" + result.Round + "." + result.Title;
                        worker.ReportProgress(1, info);
                        _logger.Info(info);
                        for (int i = 0; i < result.Options.Count; i++)
                        {
                            var opt = result.Options[i];
                            worker.ReportProgress(1, (i + 1) + opt.title + "(" + opt.score + ")");
                        }
                        // 正确选项
                        int correct = Convert.ToInt32(result.Correct);
                        info = "选：" + (correct + 1);
                        if (result.Confirmed)
                        {
                            info += "【已确认】";
                        }
                        else
                        {
                            info += "【未确认】";
                        }
                        worker.ReportProgress(1, info);
                        _logger.Info(info);
                        // 模拟选择
                        for (int i = 0; i < 2; i++)
                        {
                            Point p = _millionHeroService.TouchOption(correct);
                            worker.ReportProgress(1, "已选择!");
                            _logger.Info("Touch：" + p);

                            Thread.Sleep(800);
                        }
                    }
                    else
                    {
                        worker.ReportProgress(2, "答题未开始");
                    }
                }
                catch (Exception ex)
                {
                    worker.ReportProgress(1, "异常" + ex.Message);
                }
            }
        }

        /// <summary>
        /// 重新载入应用程序参数
        /// </summary>
        /// <param name="worker"></param>
        private void ReloadAppParams(BackgroundWorker worker)
        {
            // 重新载入坐标参数
            var resolution = _millionHeroService.GetScreenResolution();
            if (worker!=null)
            {
                worker.ReportProgress(1, string.Format("屏幕分辨率：{0}x{1}", resolution.Width, resolution.Height)); 
            }
            // 检查是否存在当前分辨率的参数
            var paras = AppGlobals.GetQuestionAppParamsList(_appType);
            bool found = false;
            foreach (var item in paras)
            {
                if (item.ScreenSize == resolution)
                {
                    found = true;
                    MillionHeroService.SetQuestionAppParams(item);
                    if (worker!=null)
                    {
                        worker.ReportProgress(1, "已找到分辨率参数！"); 
                    }
                    break;
                }
            }
            if (found == false)
            {
                MillionHeroService.SetQuestionAppParams(paras[0]);
                _millionHeroService.ChangeResolution(resolution);
                if (worker!=null)
                {
                    worker.ReportProgress(1, "未找到分辨率参数，已启用自动适配！"); 
                }
            }
        }

        /// <summary>
        /// 重启APP
        /// </summary>
        private void RestartApp()
        {
            string packageName = AppGlobals.QuestionAppApkDic[_appType][0];
            string activeName = AppGlobals.QuestionAppApkDic[_appType][1];
            _millionHeroService.ForceStopApp(packageName);
            _millionHeroService.StartApp(packageName, activeName);
            var resolution = _millionHeroService.GetScreenResolution();
            Thread.Sleep(5000);
            _millionHeroService.Touch(resolution.Width / 2, resolution.Height - 30);
        }

        /// <summary>
        /// 输出函数
        /// </summary>
        /// <param name="info"></param>
        private void DoOutput(string info)
        {
            this.textBox1.AppendText(string.Format("\r\n{0} {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), info));
            this.textBox1.ScrollToCaret();
        }

        /// <summary>
        /// “置顶”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmAuto_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MsgBox.Question("确定要退出自动模式?", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 答题大会种类调整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string key = this.toolStripComboBox1.SelectedItem.ToString();

            // 答题大会种类
            _appType = _appParamsDic[key];

            // 填充答题助手种类
            var services = _serviceDic[key];
            this.toolStripComboBox2.Items.Clear();
            foreach (var item in services)
            {
                this.toolStripComboBox2.Items.Add(item.Key);
            }
            _aiService = null;
            if (services.Count > 0)
            {
                this.toolStripComboBox2.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 答题助手调整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 获取答题助手类型
            try
            {
                string key1 = this.toolStripComboBox1.SelectedItem.ToString();
                string key2 = this.toolStripComboBox2.SelectedItem.ToString();
                _aiService = _serviceDic[key1][key2];
            }
            catch (Exception ex)
            {
                _aiService = null;
            }
        }

        /// <summary>
        /// 切换是否确认后才能答题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripComboBox3_DropDownStyleChanged(object sender, EventArgs e)
        {
            this._isDoSelectWhenConfirmed = this.toolStripComboBox3.SelectedIndex == 0;
        }

        /// <summary>
        /// “连接夜神模拟器”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripStatusLabelConnectYeShen_Click(object sender, EventArgs e)
        {
            try
            {
                _millionHeroService.ConnectYeShenSimulator();
                MsgBox.Info("连接夜神模拟器成功！");
            }
            catch (Exception ex)
            {
                MsgBox.Error("连接夜神模拟器失败", ex);
                return;
            }
        }

        /// <summary>
        /// “添加参数”-“添加百万英雄参数”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 添加百万英雄参数ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new FrmPicker(QuestionAppType.BaiWanYingXiong);
            frm.Owner = this;
            frm.Show();
        }

        /// <summary>
        /// “添加参数”-“添加芝士超人参数”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 添加芝士超人参数ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new FrmPicker(QuestionAppType.ZhiShiChaoRen);
            frm.Owner = this;
            frm.Show();
        }

        /// <summary>
        /// “添加参数”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripStatusLabel2_ButtonClick(object sender, EventArgs e)
        {
            ToolStripSplitButton btn = sender as ToolStripSplitButton;
            btn.ShowDropDown();
        }
    }
}
