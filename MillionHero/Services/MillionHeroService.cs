using System;
using System.Collections.Generic;
using System.Text;
using TDLib.Extensions;
using System.Drawing;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TDLib.Utils;
using MillionHero.Definitions;
using System.Text.RegularExpressions;
using MillionHero.Services.Ocrs;
using MillionHero.Services.QuestionAnalyzers;
using MillionHero.Exceptions;

namespace MillionHero.Services
{
    class MillionHeroService
    {
        private IOcr _ocr = null;
        /// <summary>
        /// 基础屏幕尺寸
        /// </summary>
        private static SizeF BaseScreenSize = new SizeF(1080f, 1920f);
        /// <summary>
        /// 基础裁切点，依次代表左上角坐标、右下角坐标
        /// </summary>
        private static Point[] BaseCutPoints = new Point[] { 
            new Point(47, 272), 
            new Point(1006, 1261)
        };
        /// <summary>
        /// 基础选项点，依次代表选项A坐标、选项B坐标、选项C坐标
        /// </summary>
        private static Point[] BaseOptionPoints = new Point[] { 
            new Point(540, 769),
            new Point(540, 966),
            new Point(540, 1161)
        };
        /// <summary>
        /// 坐标已更新事件
        /// </summary>
        private static event EventHandler OrdinatesUpdated;

        /// <summary>
        /// 当前使用的裁切点，依次代表左上角坐标、右下角坐标
        /// </summary>
        private Point[] _cutPoints = new Point[] { 
        };
        /// <summary>
        /// 当前使用的选项点，依次代表选项A坐标、选项B坐标、选项C坐标
        /// </summary>
        private Point[] _optionPoints = new Point[] { 
        };
        private AdbService _adbServices = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ocr"></param>
        public MillionHeroService(IOcr ocr)
        {
            _adbServices = new AdbService(Application.StartupPath + "\\adb\\adb.exe");
            _ocr = ocr;
            _cutPoints = BaseCutPoints;
            _optionPoints = BaseOptionPoints;
            OrdinatesUpdated += new EventHandler(MillionHeroService_OrdinatesUpdated);
        }

        /// <summary>
        /// 绑定OrdinatesUpdated事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MillionHeroService_OrdinatesUpdated(object sender, EventArgs e)
        {
            this.UpdateOrdinates();
        }

        /// <summary>
        /// 更新坐标
        /// </summary>
        public void UpdateOrdinates()
        {
            _cutPoints = BaseCutPoints;
            _optionPoints = BaseOptionPoints;
        }

        #region 调整屏幕分辨率

        public Size GetScreenResolution()
        {
            string file = this.CaptureMobileScreen();
            Image img = Image.FromFile(file);
            int width = img.Width;
            int height = img.Height;
            img.Dispose();

            var newResolution = new Size(width, height);

            return newResolution;
        }

        /// <summary>
        /// 调整屏幕分辨率
        /// </summary>
        public void ChangeResolution(Size newResolution)
        {
            _cutPoints[0] = ChangeResolution(BaseCutPoints[0], newResolution);
            _cutPoints[1] = ChangeResolution(BaseCutPoints[1], newResolution);
            _optionPoints[0] = ChangeResolution(BaseOptionPoints[0], newResolution);
            _optionPoints[1] = ChangeResolution(BaseOptionPoints[1], newResolution);
            _optionPoints[2] = ChangeResolution(BaseOptionPoints[2], newResolution);
        }

        private Point ChangeResolution(Point old, Size newResolution)
        {
            float newX = old.X / BaseScreenSize.Width * newResolution.Width;
            float newY = old.Y / BaseScreenSize.Height * newResolution.Height;
            return new Point((int)newX, (int)newY);
        } 
        #endregion

        /// <summary>
        /// 结束其他adb进程
        /// </summary>
        public void KillOtherAdbProcesses()
        {
            string[] pros = new string[] 
            { 
                "360MobileMgr"
            };
            foreach (var p in pros)
            {
                ProcessUtil.KillProcess(p);
            }
        }

        /// <summary>
        /// 尝试连接手机
        /// </summary>
        /// <returns></returns>
        public bool TryConnectMobile()
        {
            try
            {
                CaptureMobileScreen();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 捕获手机屏幕
        /// </summary>
        /// <returns></returns>
        public string CaptureMobileScreen()
        {
            string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".png";
            return this.CaptureMobileScreen(fileName);
        }


        /// <summary>
        /// 启动App
        /// </summary>
        /// <param name="packageName">包名，如：com.inke.trivia</param>
        /// <param name="activeActivityName">启动窗体名，如：com.inke.trivia.splash.SplashActivity</param>
        public void StartApp(string packageName, string activeActivityName)
        {
            _adbServices.StartApp(packageName, activeActivityName);
        }

        /// <summary>
        /// 强制停止App
        /// </summary>
        /// <param name="packageName">包名</param>
        public void ForceStopApp(string packageName)
        {
            _adbServices.ForceStopApp(packageName);
        }

        /// <summary>
        /// 设置答题参数
        /// </summary>
        /// <param name="appParams"></param>
        public static void SetQuestionAppParams(QuestionAppParams appParams)
        {
            BaseScreenSize = appParams.ScreenSize;
            BaseOptionPoints[0] = appParams.OptionPoints[0];
            BaseOptionPoints[1] = appParams.OptionPoints[1];
            BaseOptionPoints[2] = appParams.OptionPoints[2];
            BaseCutPoints[0] = appParams.CutPoints[0];
            BaseCutPoints[1] = appParams.CutPoints[1];
            if (OrdinatesUpdated != null)
            {
                OrdinatesUpdated(new object(), EventArgs.Empty);
            }
        }

        /// <summary>
        /// 截取题目和选择区域
        /// </summary>
        /// <param name="imgFile"></param>
        /// <returns></returns>
        private Image Resize(string imgFile)
        {
            Image imgIn = Image.FromFile(imgFile);
            Rectangle cutRect = new Rectangle(_cutPoints[0].X, _cutPoints[0].Y, _cutPoints[1].X - _cutPoints[0].X, _cutPoints[1].Y - _cutPoints[0].Y);
            Bitmap imgOut = new Bitmap(cutRect.Width, cutRect.Height);
            using (var g = Graphics.FromImage(imgOut))
            {
                g.DrawImage(imgIn, new Rectangle(0, 0, imgOut.Width, imgOut.Height), cutRect, GraphicsUnit.Pixel);

                if (AppGlobals.ShowCutImage)
                {
                    // 显示截图
                    string dir = System.IO.Path.GetDirectoryName(imgFile);
                    string filename = System.IO.Path.GetFileNameWithoutExtension(imgFile);
                    string extension = System.IO.Path.GetExtension(imgFile);
                    string filepath = dir + "\\" + filename + "_cut" + extension;
                    imgOut.Save(filepath); 
                }

                imgIn.Dispose();

                return imgOut;
            }
        }

        /// <summary>
        /// 识别题目内容
        /// </summary>
        /// <param name="imgFile1">屏幕截图文件</param>
        /// <param name="ocrTryTimes">尝试识别次数</param>
        /// <returns></returns>
        public Subject AnalyseQuestion(string imgFile1, int ocrTryTimes)
        {
            Subject subject = new Subject();

            string[] keywords = null;
            // 切割题目的区域
            using (Image imgResized = Resize(imgFile1))
            {
                // 多次文字识别
                Exception lastException = null;
                for (int i = 0; i < ocrTryTimes; i++)
                {
                    lastException = null;
                    try
                    {
                        // 文字识别
                        keywords = _ocr.Ocr(imgResized);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        keywords = null;
                    }
                    if (keywords != null)
                    {
                        // 已识别文字
                        break;
                    }
                }
                if (lastException != null)
                {
                    throw lastException;
                }
            }

            // 拆分出问题和选项
            return SplitSubject(keywords);
        }

        /// <summary>
        /// 拆分出题目
        /// </summary>
        /// <param name="keywords"></param>
        /// <returns></returns>
        private Subject SplitSubject(string[] keywords)
        {
            Subject subject = new Subject();

            /*方法一：用问号拆分问题和选项*/
            int questionFoundIndex = -1;
            for (int i = 0; i < keywords.Length; i++)
            {
                var keyword = keywords[i].Trim();
                subject.Question += keyword;
                if (keyword.EndsWith("?") || keyword.EndsWith("？"))
                {
                    questionFoundIndex = i + 1;
                    break;
                }
            }
            if (questionFoundIndex != -1)
            {
                subject.Options = new List<string>();
                for (int i = questionFoundIndex; i < keywords.Length; i++)
                {
                    var keyword = keywords[i].Trim();
                    subject.Options.Add(keyword);
                }
            }
            else
            {
                /*方法二：假设选项只有3个，那么最后3个为选项，其他为问题*/
                subject.Question = string.Empty;
                subject.Options = new List<string>();
                questionFoundIndex = keywords.Length - 3;
                for (int i = 0; i < questionFoundIndex; i++)
                {
                    var keyword = keywords[i].Trim();
                    subject.Question += keyword;
                }
                for (int i = questionFoundIndex; i < keywords.Length; i++)
                {
                    var keyword = keywords[i].Trim();
                    subject.Options.Add(keyword);
                }
            }
            if (subject.Options.Count > 3)
            {
                // 修正选项中，某个选项存在换行的情况
                List<int> removeIndexes = new List<int>();
                for (int i = 1; i < subject.Options.Count; i++)
                {
                    var last = subject.Options[i - 1];
                    var cur = subject.Options[i];
                    if (last.Length - cur.Length > 6)
                    {
                        subject.Options[i - 1] += cur;
                        removeIndexes.Add(i);
                        i++;
                    }
                }
                for (int i = removeIndexes.Count - 1; i >= 0; i--)
                {
                    var index = removeIndexes[i];
                    subject.Options.RemoveAt(index);
                }
            }
            
            return subject;
        }

        /// <summary>
        /// 查询答案
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public Answer QueryAnswer(Subject subject, IQuestionAnalyzer query)
        {
            return query.Analyze(subject);
        }

        /// <summary>
        /// 查询答案
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="query"></param>
        /// <param name="tryTimes"></param>
        /// <returns></returns>
        public Answer QueryAnswer(Subject subject, IQuestionAnalyzer query, int tryTimes)
        {
            Answer answer = null;

            // 多次查询
            Exception lastException = null;
            for (int i = 0; i < tryTimes; i++)
            {
                lastException = null;
                try
                {
                    answer = QueryAnswer(subject, query);
                }
                catch (NotFoundOptionException ex)
                {
                    // 如果搜索结果中就都不存在选项，则不需要再尝试了
                    lastException = ex;
                    answer = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    answer = null;
                }
                if (answer != null)
                {
                    // 已找到答案
                    break;
                }
            }
            if (lastException != null)
            {
                throw lastException;
            }

            return answer;
        }

        /// <summary>
        /// 捕获手机屏幕
        /// </summary>
        /// <returns></returns>
        private string CaptureMobileScreen(string fileName)
        {
            if (!Directory.Exists("captures"))
                Directory.CreateDirectory("captures");
            string localFilePath = ".\\captures\\" + fileName;

            // 截屏并拉取文件
            _adbServices.CaptureScreen("/sdcard/questionAI.png");
            _adbServices.Pull("/sdcard/questionAI.png", ".\\captures\\" + fileName);

            return localFilePath;
        }

        /// <summary>
        /// 连接夜神模拟器
        /// </summary>
        public void ConnectYeShenSimulator()
        {
            _adbServices.Connect("127.0.0.1", 62001);
        }

        /// <summary>
        /// 触摸屏幕指定点
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        public void Touch(int x, int y)
        {
            _adbServices.Tap(x, y);
        }

        /// <summary>
        /// 选择选项
        /// </summary>
        /// <param name="index">选项序号，从0开始</param>
        public Point TouchOption(int index)
        {
            if (index < 0)
            {
                index = 0;
            }
            if (index > 2)
            {
                index = 2;
            }

            Touch(_optionPoints[index].X, _optionPoints[index].Y);

            return _optionPoints[index];
        }
    }
}
