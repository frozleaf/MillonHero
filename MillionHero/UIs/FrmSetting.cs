using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TDLib.UIs.Forms;
using TDLib.Utils;
using System.Diagnostics;

namespace MillionHero.UIs
{
    public partial class FrmSetting : DialogBase
    {
        public FrmSetting()
        {
            InitializeComponent();
            base.LoadIconFromRes("app.ico");
            this.textBox1.Text = AppGlobals.BaiduApiKey;
            this.textBox2.Text = AppGlobals.BaiduSecretKey;
            this.checkBox1.Checked = AppGlobals.IsDebug;
            this.checkBox2.Checked = AppGlobals.ShowCutImage;
        }

        public override bool OnOk()
        {
            string apiKey = this.textBox1.Text.Trim();
            string secretKey = this.textBox2.Text.Trim();
            bool isDebug = this.checkBox1.Checked;
            bool showCutImage = this.checkBox2.Checked;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MsgBox.Warning("百度ApiKey不能为空！");
                return false;
            }
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                MsgBox.Warning("百度SecretKey不能为空！");
                return false;
            }
            AppGlobals.SaveBaiduApiKey(apiKey);
            AppGlobals.SaveBaiduSecretKey(secretKey);
            AppGlobals.SaveIsDebug(isDebug);
            AppGlobals.SaveShowCutImage(showCutImage);

            return true;
        }

        /// <summary>
        /// “百度OCR”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://console.bce.baidu.com/ai/#/ai/ocr/overview/index");
        }
    }
}
