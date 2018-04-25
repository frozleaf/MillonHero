using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MillionHero.Services;
using TDLib.UIs.Forms;
using TDLib.Utils;
using MillionHero.Definitions;

namespace MillionHero.UIs
{
    public partial class FrmPicker : FrmBase
    {
        private MillionHeroService _service = new MillionHeroService(null);
        private QuestionAppParams _paras = new QuestionAppParams();
        private QuestionAppType _appType = QuestionAppType.BaiWanYingXiong;

        public FrmPicker(QuestionAppType appType)
        {
            InitializeComponent();
            base.LoadIconFromRes("app.ico");
            _appType = appType;
        }

        private void toolStripButtonLoadImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*";
            ofd.FilterIndex = 1;
            ofd.InitialDirectory = Application.StartupPath + "\\captures";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    this.pictureBox1.Image = Image.FromFile(ofd.FileName);
                    int width = this.pictureBox1.Image.Width;
                    int height = this.pictureBox1.Image.Height;
                    this.toolStripStatusLabelResolution.Text =
                        string.Format("屏幕分辨率：{0},{1}", width, height);

                    _paras = new QuestionAppParams();
                    _paras.CutPoints = new Point[2];
                    _paras.OptionPoints = new Point[3];
                    _paras.ScreenSize = new Size(width, height);
                }
                catch (Exception ex)
                {
                    MsgBox.Error("加载图像失败", ex);
                }
            }
            else
            {
                return;
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Items.Add("取点", null, (o, args) =>
            {
                this.toolStripTextBoxPickPosition.Text = string.Format("{0},{1}", e.Location.X, e.Location.Y);
                this.toolStripTextBoxPickPosition.Tag = e.Location;
            });
            cms.Items.Add("设置切割点-左上角坐标", null, (o, args) =>
            {
                _paras.CutPoints[0] = e.Location;
            });
            cms.Items.Add("设置切割点-右上角坐标", null, (o, args) =>
            {
                _paras.CutPoints[1] = e.Location;
            });
            cms.Items.Add("设置选项A坐标", null, (o, args) =>
            {
                _paras.OptionPoints[0] = e.Location;
            });
            cms.Items.Add("设置选项B坐标", null, (o, args) =>
            {
                _paras.OptionPoints[1] = e.Location;
            });
            cms.Items.Add("设置选项C坐标", null, (o, args) =>
            {
                _paras.OptionPoints[2] = e.Location;
            });
            cms.Items.Add("保存参数", null, (o, args) =>
            {
                AppGlobals.SaveQuestionAppParams(_appType, _paras);
                AppGlobals.ReadAppParams();
                MsgBox.Info("参数已保存！");
            });
            cms.Show(this.pictureBox1,e.Location);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            this.toolStripStatusLabelCurPosition.Text = string.Format("坐标：{0},{1}", e.Location.X, e.Location.Y);
        }

        private void toolStripButtonTouch_Click(object sender, EventArgs e)
        {
            if (this.toolStripTextBoxPickPosition.Tag == null)
            {
                MsgBox.Warning("请先选点！");
                return;
            }
            var p = (Point)this.toolStripTextBoxPickPosition.Tag;
            try
            {
                _service.Touch(p.X,p.Y);
                MsgBox.Info("模拟点击成功！");
            }
            catch (Exception ex)
            {
                MsgBox.Error("模拟点击失败", ex);
            }
        }

        private void toolStripStatusLabelConnectYeShen_Click(object sender, EventArgs e)
        {
            try
            {
                _service.ConnectYeShenSimulator();
                MsgBox.Info("连接夜神模拟器成功！");
            }
            catch (Exception ex)
            {
                MsgBox.Error("连接夜神模拟器失败", ex);
                return;
            }
        }
    }
}
