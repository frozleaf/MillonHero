using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using MillionHero.Definitions;
using System.Windows.Forms;
using Microsoft.Win32;
using TDLib.Utils;

namespace MillionHero.Services.AIServices
{
    [ComVisible(true)]
    public class BaiDuAIService : IAIService
    {
        private AIResult _answerArrived = null;
        private object _locker = new object();
        private string _url = null;
        private WebBrowser _browser = null;
        private string _htmlFileName = null;

        static BaiDuAIService()
        {
            /*
对于64位程序
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION
对于32位程序
HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION
             */

            try
            {
                //设置webbrowser的ie版本
                RegistryKey subKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                string[] names = new string[]
                {
                    fileName + ".exe",
                    fileName + ".vshost.exe"
                };
                foreach (var name in names)
                {
                    subKey.SetValue(name, (int)IEVersion.IE10, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MsgBox.Error("设置IE版本异常", ex);
            }
        }

        public BaiDuAIService(WebBrowser browser, string htmlFileName)
        {
            // file:///E:/Projects/C%23/MillonHero/MillonHero/bin/Debug/简单搜索(百万英雄专场).html

            _browser = browser;
            _htmlFileName = htmlFileName;
        }

        public void Init()
        {
            string filePath = Application.StartupPath + "\\" + _htmlFileName;
            string tempFilePath = System.IO.Path.GetDirectoryName(filePath) + "\\temp\\" + _htmlFileName;
            string tempFileDir = System.IO.Path.GetDirectoryName(tempFilePath);
            if (!System.IO.Directory.Exists(tempFileDir))
            {
                System.IO.Directory.CreateDirectory(tempFileDir);
            }
            string content = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
            string flag = "('answer', function(data) {";
            int insertIndex = content.IndexOf(flag);
            if (insertIndex == -1)
            {
                throw new Exception("未找到('answer', function(data) {部分");
            }
            insertIndex += flag.Length;
            string insertStr = @"
                    var json = JSON.stringify(data);
					try{
						window.external.test(json);
					}
					catch(err){}
					return;";
            content = content.Insert(insertIndex, insertStr);
            System.IO.File.WriteAllText(tempFilePath, content, Encoding.UTF8);
            _url = "file:///" + tempFilePath.Replace("\\", "/");

            _browser.ObjectForScripting = this;
            _browser.Navigate(_url);
        }

        public AIResult Fetch()
        {
            lock (_locker)
            {
                return _answerArrived;
            }
        }

        public void test(string str)
        {
            #region json结果
            /*
             * {
                "app": "zhishichaoren",
                "question": {
                    "text": "中国历史上对长城的大规模的修建分别是秦朝和哪个朝代?",
                    "url": "",
                    "questionId": "58"
                },
                "answers": [
                    {
                        "text": "唐朝",
                        "url": "",
                        "count": 0,
                        "prop": 58.25,
                        "dqa": 2
                    },
                    {
                        "text": "宋朝",
                        "url": "",
                        "count": 0,
                        "prop": 58.25,
                        "dqa": 1
                    },
                    {
                        "text": "明朝",
                        "url": "",
                        "count": 0,
                        "prop": 116.5,
                        "dqa": 100
                    }
                ],
                "sn": 58,
                "tips": "我决定选择C试试，应该OK",
                "step": 2,
                "status": 0,
                "result": 2
            }
             */
            
            #endregion

            try
            {
                Console.WriteLine(str);

                BaiDuAIResult result = JsonConvert.DeserializeObject<BaiDuAIResult>(str);
                if (result.step == 1 || result.step==2)
                {
                    // 过滤“复活卡3305237人”错误信息
                    if (result.question.text.Contains("复活卡") && result.question.text.Contains("人"))
                    {
                        return;
                    }
                    lock (_locker)
                    {
                        _answerArrived = new AIResult();
                        _answerArrived.Confirmed = result.step == 2;
                        _answerArrived.Round = result.question.questionId;
                        _answerArrived.Correct = result.result;
                        _answerArrived.Title = result.question.text;
                        _answerArrived.Options = new List<AIResultOption>();
                        for (int i = 0; i < result.answers.Count; i++)
                        {
                            var answer = result.answers[i];
                            _answerArrived.Options.Add(new AIResultOption()
                            {
                                title = answer.text,
                                score = answer.prop,
                                confidence = answer.dpa
                            });
                        }
                    }
                }
             }
            catch (Exception ex)
            {
                Console.WriteLine("异常:" + ex.Message);
            }
        }
    }

    class BaiDuAIResult
    {
        public string app;
        public BaiDuAIResultQuestion question;
        public List<BaiDuAIResultOption> answers;
        public int sn;
        public string tips;
        public int step;
        public int status;
        public int result;
    }

    class BaiDuAIResultQuestion
    {
        public string text;
        public string url;
        public int questionId;
    }

    class BaiDuAIResultOption
    {
        public string text;
        public string url;
        public int count;
        public double prop;
        public int dpa;
    }
}
