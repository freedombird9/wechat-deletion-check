using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EasyHttp;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Xml;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

        private String                           uuid;
        private int                              tip;
        private String                           timestamp;
        private String                           base_url;
        private String                           redirect_url;
        private String                           skey;
        private String                           wxsid;
        private String                           wxuin;
        private String                           pass_ticket;
        private String                           deviceId = "e000000000000000";
        private List<String>                     contactList;
        private List<String>                     self;

        private CookieContainer                  cookieContainer;

        public Form1()
        {
            InitializeComponent();
            timestamp = Convert.ToInt32((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
            info_display.ReadOnly = true;
            cookieContainer = new CookieContainer();
        }

        private void startbtn_Click(object sender, EventArgs e)
        {
            main();
        }

        private Tuple<String, WebResponse> getResponseText(HttpWebRequest http)
        {
            var response = http.GetResponse();
            var encoding = ASCIIEncoding.ASCII;

            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                return Tuple.Create(reader.ReadToEnd(), response);
            }
        }

        private Boolean getUUID()
        {
            var url = String.Format("https://login.weixin.qq.com/jslogin?appid=wx782c26e4c19acffb&fun=new&lang=zh_CN&_={0}", timestamp);
            var http = WebRequest.Create(url) as HttpWebRequest;
            // var param = new { appid = "wx782c26e4c19acffb", fun = "new", lang = "zh_CN", _ = timestamp };
            // var response = http.Get(url, param);
            var responseText = getResponseText(http).Item1;
            Console.WriteLine(responseText);
            var regex = @"window.QRLogin.code = (\d+); window.QRLogin.uuid = ""(\S+?)""";
            var r = new Regex(regex, RegexOptions.IgnoreCase);
            Match m = r.Match(responseText);
            Console.WriteLine(m.Success);

            var code = m.Groups[1].Value;
            uuid = m.Groups[2].Value;
            Console.WriteLine(code);
            Console.WriteLine(uuid);

            if (code == "200")
            {
                return true;
            }

            return false;
        }

        private void showQRImage()
        {
            var url = String.Format("https://login.weixin.qq.com/qrcode/" + uuid + "?t=webwx&_={0}", timestamp);
            Console.WriteLine(url);
            tip = 1;
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    Image image = Image.FromStream(stream);
                    qrcode_img.Image = image;
                    qrcode_img.Height = image.Height;
                    qrcode_img.Width = image.Width;
                    this.Controls.Remove(start_btn);
                    start_btn.Dispose();

                    info_display.Invoke(new Action( ()=>
                    {
                        info_display.AppendText(String.Format("请使用微信扫描二维码以登录{0}", Environment.NewLine));
                        info_display.ScrollToCaret();
                    }));                   
                }
            }
        }

        private String waitForLogin()
        {
            var url = String.Format("https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip={0}&uuid={1}&_={2}", tip, uuid, timestamp);
            Console.WriteLine(url);
            var http = WebRequest.Create(url) as HttpWebRequest;
            var tuple = getResponseText(http);
            var text = tuple.Item1;
            Console.WriteLine(text);
            var regex = @"window.code=(\d+);";
            var r = new Regex(regex, RegexOptions.None);
            Match m = r.Match(text);
            var code = m.Groups[1].Value;

            // cookie?
            var response = (HttpWebResponse)tuple.Item2;
            Console.WriteLine("test cookie");
            Console.WriteLine(String.Format("code: {0}", code));
            foreach (Cookie cookie in response.Cookies)
            {
                Console.WriteLine(String.Format("Cookie_in_wait_login: {0}, {1}", cookie.Name,cookie.Value));
            }
            
            if(code == "201")
            {
                info_display.Invoke(new Action(() =>
                {
                    info_display.AppendText(String.Format("扫描成功,请在手机上点击确认以登录{0}", Environment.NewLine));
                    info_display.ScrollToCaret();
                }
                ));
                tip = 0;
            } else if(code == "200")
            {
                info_display.Invoke(new Action( () => 
                {
                    info_display.AppendText(String.Format("成功扫描,请在手机上点击确认以登录{0}", Environment.NewLine));
                    info_display.ScrollToCaret();
                    info_display.AppendText(String.Format("正在登录...{0}", Environment.NewLine));
                    info_display.ScrollToCaret();
                }
                ));
                regex = @"window.redirect_uri=""(\S+?)"";";
                r = new Regex(regex, RegexOptions.None);
                m = r.Match(text);
                redirect_url = m.Groups[1] + "&fun=new";
                for (int i = redirect_url.Length - 1; i >= 0; i--)
                {
                    if(redirect_url[i] == '/')
                    {
                        base_url = redirect_url.Substring(0, i + 1);
                        break;
                    }
                }           
            } else if(code == "408")
            {
                // do nothing
            }
            return code;
        }

        private Boolean login()
        {
            this.Controls.Remove(qrcode_img);
            qrcode_img.Dispose();
            info_display.Location = new System.Drawing.Point(0, 0);
            info_display.Size = new Size(333, 438);
            var http = WebRequest.Create(redirect_url) as HttpWebRequest;
            var tuple = getResponseText(http);
            var data = tuple.Item1;
            Console.WriteLine("in login...");
            Console.WriteLine(data);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            var root = doc.DocumentElement;
            skey = root.SelectSingleNode("skey").InnerText;
            wxsid = root.SelectSingleNode("wxsid").InnerText;
            wxuin = root.SelectSingleNode("wxuin").InnerText;
            pass_ticket = root.SelectSingleNode("pass_ticket").InnerText;
            Console.WriteLine(String.Format("skey: {0}, wxsid: {1}, wxuin: {2}, pass_ticket: {3}", skey, wxsid, wxuin, pass_ticket));
            // cookie?
            var response = (HttpWebResponse)tuple.Item2;
            foreach (Cookie cookie in response.Cookies)
            {
                Console.WriteLine(String.Format("Cookie_in_login: {0}, {1}", cookie.Name, cookie.Value));
            }

            if (String.IsNullOrEmpty(skey) || String.IsNullOrEmpty(wxsid) || String.IsNullOrEmpty(wxuin) || String.IsNullOrEmpty(pass_ticket))
            {
                return false;
            }
            return true;
        }

        private Boolean webwxinit()
        {

            var url = base_url + String.Format("/webwxinit?pass_ticket={0}&skey={1}&r={2}", pass_ticket, skey, timestamp);
            var http = new EasyHttp.Http.HttpClient();
            var BaseRequest = new { Uin = int.Parse(wxuin), Sid = wxsid, Skey = skey, DeviceID = deviceId};
            var response = http.Post(url, new { BaseRequest = BaseRequest }, "application/json; charset=UTF-8");
            var dic = response.DynamicBody;
            contactList = dic.ContactList;
            self = dic.User;
            var ErrMsg = dic["BaseResponse"]["ErrMsg"];
            if (ErrMsg.Length() > 0)
            {
                info_display.Invoke(new Action( () =>
                {
                    info_display.AppendText(ErrMsg + Environment.NewLine);
                } ));
            }
            if( dic["BaseResponse"]["Ret"] != 0)
            {
                return false;
            }

            Console.WriteLine(dic);
            return true;
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                if (!getUUID())
                {
                    info_display.Invoke( new Action( () =>
                    {
                        info_display.AppendText(String.Format("获取uuid失败{0}", Environment.NewLine));
                        info_display.ScrollToCaret();
                    }));             
                    return;
                }
                showQRImage();
                while (waitForLogin() != "200") ;
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!login())
            {
                info_display.AppendText(String.Format("登录失败{0}", Environment.NewLine));
                info_display.ScrollToCaret();
                return;
            }
        }

        private void main()
        {
            if (!getUUID())
            {
                info_display.AppendText(String.Format("获取uuid失败{0}", Environment.NewLine));
                info_display.ScrollToCaret();
                return;
            }
            showQRImage();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync();
        }


        private void info_display_TextChanged(object sender, EventArgs e)
        {

        }


    }
}
