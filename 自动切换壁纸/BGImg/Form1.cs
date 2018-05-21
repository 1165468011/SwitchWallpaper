using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BGImg
{
    public partial class Form1 : Form
    {
        int NewImgLenth = 0;   //图片数量
        string NewImgPath = "";     //图片地址
        System.IO.DirectoryInfo NewImg;
        string path = "c://Windows//BGImg.txt"; //配置文件地址：主要是更换壁纸的速度
        int Interval = 2000;    //换壁纸的间隔
        string NowPath = "";  //当前图片路径
        FileInfo[] fi;  //获取到的文件数组
        byte[] keys;        //转字节之后的数组
        public Form1()
        {
            InitializeComponent();
            if (File.Exists(path))
            { 
                using (StreamReader sr = new StreamReader(path))
                    {
                        try
                        {
                        //读取配置文件中的速度              
                        Interval = int.Parse(sr.ReadLine());
                        }
                        catch (Exception)
                        {
                            //读取错误时，默认2000速度
                            Interval = 2000;
                        }
                    }
            }
            else 
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                    {
                        //将当前时间写入配置文件
                        sw.Write(Interval);
                    }
                }
            }
            #region 注册热键
            bool result =  Hotkey.RegisterHotKey(Handle, 204800, Hotkey.KeyModifiers.Shift | Hotkey.KeyModifiers.Alt, Keys.PageUp);
            result = Hotkey.RegisterHotKey(Handle, 204900, Hotkey.KeyModifiers.Shift | Hotkey.KeyModifiers.Alt, Keys.PageDown);
            result = Hotkey.RegisterHotKey(Handle, 205000, Hotkey.KeyModifiers.Shift | Hotkey.KeyModifiers.Alt, Keys.Delete);
            result = Hotkey.RegisterHotKey(Handle, 205100, Hotkey.KeyModifiers.Shift | Hotkey.KeyModifiers.Alt, Keys.Home);
            #endregion
            this.Visible = false;
            this.WindowState = FormWindowState.Minimized;
            SetStart();
            NewImgPath = Application.StartupPath + "/NewImg";  //图片文件夹路径
            NewImg = new System.IO.DirectoryInfo(NewImgPath);
            SystemParametersInfo(20, 0, NewImg.GetFiles()[ new Random().Next(NewImg.GetFiles().Length) ].FullName, 0x2); //设置初始壁纸
            new Thread( ( new ThreadStart( new Action(() => 
            {
                while (true)
                {
                    fi = NewImg.GetFiles(); //获取图片
                    keys = new byte[fi.Length];
                    (new Random()).NextBytes(keys);
                    Array.Sort(keys, fi);  //对图片排序
                    List<FileInfo> list = new List<FileInfo>(fi);  //排序后的图片
                    try
                    {
                        foreach (var item in list)  //循环将图片设置为壁纸
                        {
                            if (File.Exists(item.FullName)) //当文件存在时，设置壁纸，防止图片突然被删除的可能
                            {
                                SystemParametersInfo(20, 0, item.FullName, 0x2);
                                NowPath = item.FullName; //记录当前图片
                                Thread.Sleep(Interval);
                            }
                               
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    //初始化操作
                    list.Clear(); 
                    list = null;
                    fi = null;
                    keys = null;
                }
                
            })))).Start();
        }
        /// <summary>
        /// 设置壁纸的系统API
        /// </summary>
        /// <param name="uAction"></param>
        /// <param name="uParam"></param>
        /// <param name="lpvParam"></param>
        /// <param name="fuWinIni"></param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern int SystemParametersInfo(
         int uAction,
         int uParam,
         string lpvParam,
         int fuWinIni
         );
        /// <summary>
        /// 隐藏窗体
        /// </summary>
        /// <param name="value"></param>
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }
        /// <summary>
        /// 监听消息
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)     //如果当前Windows消息为热键信息
            {
                switch (m.WParam.ToInt32())
                {
                    case 204800: //加快切换速度
                        try
                        {

                            if (Interval >= 1000)
                            {
                                Interval -= 500;
                                using (FileStream fs = new FileStream(path, FileMode.Create))
                                {
                                    using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                                    {
                                        sw.Write(Interval);
                                    }
                                }
                            }

                        }
                        catch
                        {
                        }
                            break;
                    case 204900:    //减缓切换速度
                            try
                            {

                                Interval += 500;
                                using (FileStream fs = new FileStream(path, FileMode.Create))
                                {
                                    using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                                    {
                                        sw.Write(Interval);
                                    }
                                }
                            }
                            catch
                            {
                            }
                        break;
                    case 205000:    //删除当前壁纸
                        try
                        {
                            FileInfo fi = new FileInfo(NowPath);
                        fi.Delete();
                        NowPath = NewImg.GetFiles()[new Random().Next(NewImg.GetFiles().Length)].FullName;
                        SystemParametersInfo(20, 0, NowPath, 0x2);
                        }
                        catch
                        {
                        }
                        
                        break;

                    case 205100:     //打开当前图片的目录并选中到图片
                        try
                        {
                        string args = string.Format("/Select, {0}", NowPath);
                        ProcessStartInfo pfi = new ProcessStartInfo("Explorer.exe", args);
                        System.Diagnostics.Process.Start(pfi);
                        }
                        catch 
                        {
                        }
                        break;
                }
            }
            base.WndProc(ref m);
        }
        /// <summary>
        /// 设置开机自启动
        /// </summary>
        private void SetStart()
        {
            try
            {
                string path = Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                rk2.SetValue("BGImg", path);
                rk2.Close();
                rk.Close();
            }
            catch
            {
            }
        }
        /// <summary>
        /// 当程序退出时，注销热键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
            Hotkey.UnregisterHotKey(Handle, 204800);
            Hotkey.UnregisterHotKey(Handle, 204900);
            Hotkey.UnregisterHotKey(Handle, 205000);
            Hotkey.UnregisterHotKey(Handle, 205100);
            }
            catch
            {
            }
        }

    }
}
