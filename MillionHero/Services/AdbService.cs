using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MillionHero.Services
{
    class AdbService
    {
        private string _adbFilePath = null;

        public AdbService(string adbFilePath)
        {
            _adbFilePath = adbFilePath;
        }

        /// <summary>
        /// 从手机上拉取文件
        /// </summary>
        /// <param name="srcPath">源路径，如：/sdcard/autojump.png</param>
        /// <param name="dstPath">目标路径，如：.\\captures\\autojump.png</param>
        public void Pull(string srcPath, string dstPath)
        {
            RunAdb("pull " + srcPath + " " + dstPath);
        }

        /// <summary>
        /// 截屏
        /// </summary>
        /// <param name="outputPath">截屏保存位置，如：/sdcard/autojump.png</param>
        public void CaptureScreen(string outputPath)
        {
            RunAdb("shell screencap -p " + outputPath);
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect(string ip, int port)
        {
            RunAdb(string.Format("connect {0}:{1}",ip,port));
        }

        /// <summary>
        /// 安装App
        /// </summary>
        /// <param name="apkFilePath">apk文件路径</param>
        public void InstallApp(string apkFilePath)
        {
            RunAdb(string.Format("install {0}", apkFilePath));
        }

        /// <summary>
        /// 卸载App
        /// </summary>
        /// <param name="packageName">包名</param>
        public void UninstallApp(string packageName)
        {
            RunAdb(string.Format("uninstall {0}", packageName));
        }

        /// <summary>
        /// 启动App
        /// </summary>
        /// <param name="packageName">包名，如：com.inke.trivia</param>
        /// <param name="activeActivityName">启动窗体名，如：com.inke.trivia.splash.SplashActivity</param>
        public void StartApp(string packageName, string activeActivityName)
        {
            RunAdb(string.Format("shell am start -n {0}/{1}", packageName, activeActivityName));
        }

        /// <summary>
        /// 强制停止App
        /// </summary>
        /// <param name="packageName">包名</param>
        public void ForceStopApp(string packageName)
        {
            RunAdb(string.Format("shell am force-stop {0}", packageName));
        }

        /// <summary>
        /// 按压指定时间
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="milliseconds"></param>
        public void Push(int x, int y, long milliseconds)
        {
            // 注意：该命令会阻塞adb.exe进程milliseconds毫秒
            RunAdb(string.Format("shell input swipe {0} {1} {2} {3} {4}", x, y, x+1, y+1, milliseconds));
        }

        /// <summary>
        /// 点击指定位置
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Tap(int x, int y)
        {
            RunAdb(string.Format("shell input tap {0} {1}", x, y));
        }

        /// <summary>
        /// 运行adb
        /// </summary>
        /// <param name="cmd">参数</param>
        private string RunAdb(string cmd)
        {
            Process p = new Process();
            p.StartInfo.FileName = _adbFilePath;
            p.StartInfo.Arguments = cmd;
            p.StartInfo.UseShellExecute = false; // 使用redirect时必须设置为false
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true; // 对于console程序有效
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(); // 等待进程结束

            return output;
        }
    }
}
