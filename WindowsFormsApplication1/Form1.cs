#define DEBUG
#undef DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Xml;
using System.Web.Script.Serialization;
using System.Collections;
using System.Linq;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

        private String                           uuid;
        private int                              tip;
        private String                           base_url;
        private String                           redirect_url;
        private String                           skey;
        private String                           wxsid;
        private String                           wxuin;
        private String                           pass_ticket;
        private String                           deviceId = "e000000000000021";
        private Dictionary<string, dynamic>      self;
        private int                              MAX_GROUP_NUM = 35;
        private int                              COL_NUM = 4;

        private CookieContainer                  cookieContainer;

        public Form1()
        {
            InitializeComponent();
            info_display.ReadOnly = true;
            cookieContainer = new CookieContainer();
#if (DEBUG)
            pass_ticket = "qEOsxz2YgLm%2FxYHFE4nYLQck9aLFhMfLn%2Bd%2F6zeN3Q47NHu%2Fc3i3nLdPk3nlQErU";
            skey = "@crypt_3ca759_44370b5c2fb4f06d53b6bad843aefd1d";
            wxsid = "AcaXGkKgYVusu7b/";
            wxuin = "2622149902";
            base_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/";
            self = new Dictionary<string, dynamic>();
            //self.Add("UserName", "@b8a8bddd76a0b0e7c98225c86962de1a780a81b595ad18ae4383e647b3b8227f"); // Liao's
            self.Add("UserName", "@7f745a02f3c4f0361103dd85f73d9e65d8591c4fb7d2ac6d4aba8c07382db8f8"); // me
#endif
        }

        private void startbtn_Click(object sender, EventArgs e)
        {
            main();
        }

        private string get_timestamp()
        {
            return Convert.ToInt32((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
        }

        private Tuple<String, HttpWebResponse> getResponseText(HttpWebRequest http, Encoding enc)
        {
            var response = (HttpWebResponse) http.GetResponse(); // should catch timeout exception

            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), enc))
            {
                return Tuple.Create(reader.ReadToEnd(), response);
            }
        }

        private HttpWebResponse getPostResponse(HttpWebRequest request, string payload)
        {
            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(payload);
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            return (HttpWebResponse) request.GetResponse();
        }

        private Dictionary<string, dynamic> deserilizeJson(HttpWebResponse response)
        {
            Console.WriteLine((int)response.StatusCode);        
            string serverResponse = "";
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream != null)
                {
                    using (var responseStreamReader = new StreamReader(responseStream))
                    {
                        serverResponse = responseStreamReader.ReadToEnd();
                    }
                }
            }
            var deserializer = new JavaScriptSerializer();
            var dic = deserializer.Deserialize<Dictionary<string, dynamic>>(serverResponse);
            return dic;
        }

        private Boolean getUUID()
        {
            var url = String.Format("https://login.weixin.qq.com/jslogin?appid=wx782c26e4c19acffb&fun=new&lang=zh_CN&_={0}", get_timestamp());
            var http = WebRequest.Create(url) as HttpWebRequest;
            http.CookieContainer = cookieContainer;
            // var param = new { appid = "wx782c26e4c19acffb", fun = "new", lang = "zh_CN", _ = get_timestamp() };
            // var response = http.Get(url, param);
            var responseText = getResponseText(http, new UTF8Encoding(true, true)).Item1;
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
            var url = String.Format("https://login.weixin.qq.com/qrcode/" + uuid + "?t=webwx&_={0}", get_timestamp());
            Console.WriteLine(url);
            tip = 1;
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            info_display.Invoke(new Action(() =>
            {
                info_display.AppendText(String.Format("正在读取二维码，请稍等……{0}", Environment.NewLine));
                info_display.ScrollToCaret();
            }));
            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {              
                    Image image = Image.FromStream(stream); // should catch timeout exception                   
                    qrcode_img.Invoke(new Action( ()=>
                    {
                        qrcode_img.Image = image;
                        qrcode_img.Height = image.Height;
                        qrcode_img.Width = image.Width;
                    }));

                    start_btn.Invoke(new Action( ()=>
                    {
                        this.Controls.Remove(start_btn);
                        start_btn.Dispose();
                    }));
                                  
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
            var url = String.Format("https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?tip={0}&uuid={1}&_={2}", tip, uuid, get_timestamp());
            Console.WriteLine(url);
            var http = WebRequest.Create(url) as HttpWebRequest;
            http.CookieContainer = cookieContainer;
            var tuple = getResponseText(http, new UTF8Encoding(true, true));
            var text = tuple.Item1;
            Console.WriteLine(text);
            var regex = @"window.code=(\d+);";
            var r = new Regex(regex, RegexOptions.None);
            Match m = r.Match(text);
            var code = m.Groups[1].Value;

            var response = (HttpWebResponse)tuple.Item2;
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
            qrcode_img.Invoke(new Action(() =>
            {
                this.Controls.Remove(qrcode_img);
                qrcode_img.Dispose();
            }));

            info_display.Invoke(new Action(() =>
            {
                info_display.Location = new System.Drawing.Point(0, 0);
                info_display.Size = new Size(333, 438);
            }));            
            var http = WebRequest.Create(redirect_url) as HttpWebRequest;
            http.CookieContainer = cookieContainer;
            var tuple = getResponseText(http, new UTF8Encoding(true, true));
            var data = tuple.Item1;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            var root = doc.DocumentElement;
            skey = root.SelectSingleNode("skey").InnerText;
            wxsid = root.SelectSingleNode("wxsid").InnerText;
            wxuin = root.SelectSingleNode("wxuin").InnerText;
            pass_ticket = root.SelectSingleNode("pass_ticket").InnerText;
            var response = (HttpWebResponse)tuple.Item2;
            foreach (Cookie cookie in response.Cookies)
            {
                Console.WriteLine(String.Format("Cookie_in_login: {0}: {1}", cookie.Name, cookie.Value));
            }
            if (String.IsNullOrEmpty(skey) || String.IsNullOrEmpty(wxsid) || String.IsNullOrEmpty(wxuin) || String.IsNullOrEmpty(pass_ticket))
            {
                return false;
            }
            return true;
        }

        private Boolean webwxinit()
        {

            var url = base_url + String.Format("webwxinit?pass_ticket={0}&skey={1}&r={2}", pass_ticket, skey, get_timestamp());
            Console.WriteLine(String.Format("base URL: {0}", base_url));
            var http = WebRequest.Create(url) as HttpWebRequest;
            http.CookieContainer = cookieContainer;
            http.ContentType = "application/json; charset=UTF-8";
            http.Method = "POST";
            var serializer = new JavaScriptSerializer();
            var base_req_param = new { Uin = Int64.Parse(wxuin), Sid = wxsid, Skey = skey, DeviceID = deviceId };
            var BaseRequest = serializer.Serialize(new { BaseRequest = base_req_param });
            Console.WriteLine(String.Format("BaseRequest: {0}", BaseRequest));
            var response = getPostResponse(http, BaseRequest);
            var dic = deserilizeJson(response);
            Console.WriteLine(dic);
            self = dic["User"];
            var ErrMsg = dic["BaseResponse"]["ErrMsg"];
            if (ErrMsg.Length > 0)
            {
                updateUITextLine(info_display, ErrMsg, Environment.NewLine);
            }
            if( dic["BaseResponse"]["Ret"] != 0)
            {
                return false;
            }     
            return true;
        }

        private List<Dictionary<string, dynamic>> webwxgetcontact()
        {
            var url = base_url + String.Format("webwxgetcontact?pass_ticket={0}&skey={1}&r={2}", pass_ticket, skey, get_timestamp());
            var http = WebRequest.Create(url) as HttpWebRequest;
#if (DEBUG)
            Uri target = new Uri("https://wx.qq.com");
            cookieContainer.Add(new Cookie("mm_lang", "zh_CN") { Domain = target.Host });
            cookieContainer.Add(new Cookie("webwx_data_ticket", "AQcxgRoS/+4kJjhWYZkp9B/K") { Domain = target.Host });
            cookieContainer.Add(new Cookie("wxuin", "2622149902") { Domain = target.Host });
            cookieContainer.Add(new Cookie("wxsid", "AcaXGkKgYVusu7b/") { Domain = target.Host });
            cookieContainer.Add(new Cookie("wxloadtime", "1452586739") { Domain = target.Host });
            cookieContainer.Add(new Cookie("webwxuvid", "0ae0eea9a9a07799bff91e2f6c7a80a648ed11392f720614e0ca1ce1c8c2248c0026864c65eb9e1fafadfeb6a8e66485") { Domain = target.Host });
#endif
            http.CookieContainer = cookieContainer;
            http.ContentType = "application/json; charset=UTF-8";
            http.Method = "GET";
            var response = http.GetResponse() as HttpWebResponse;
            var dic = deserilizeJson(response);
            List<Dictionary<string, dynamic>> memberlist = new List<Dictionary<string, dynamic>>(dic["MemberList"].
                                                           ToArray(typeof(Dictionary<string, dynamic>)));
            var special_users = new List<string>()
            {
                "newsapp",
                "fmessage",
                "filehelper",
                "weibo",
                "qqmail",
                "tmessage",
                "qmessage",
                "qqsync",
                "floatbottle",
                "lbsapp", "shakeapp","medianote", "qqfriend", "readerapp",
                "blogapp", "facebookapp","masssendapp", "meishiapp", "feedsapp", "voip", "blogappweixin","weixin",
                "brandsessionholder", "weixinreminder","wxid_novlwrv3lqwv11", "gh_22b87fa7cb3c", "officialaccounts","notification_messages",
                "wxitil", "userexperience_alarm"
            };

            for (int i = memberlist.Count - 1; i > -1; i--)
            {
                Dictionary<string, dynamic> member = memberlist[i];
                if ((8 & Convert.ToInt32(member["VerifyFlag"])) != 0)
                {
                    memberlist.Remove(member);
                }
                else if (special_users.Contains(member["UserName"]))
                {
                    memberlist.Remove(member);
                }
                else if (member["UserName"].Contains("@@"))
                {
                    memberlist.Remove(member);
                }
                else if (member["UserName"] == self["UserName"])
                {
                    memberlist.Remove(member);
                }
            }

            return memberlist;

        }

        private Tuple<string, List<string>, string> createChatRoom(List<string> user_names)
        {
            var member_list = new List<Dictionary<string, dynamic>>();
            user_names.ForEach(username => 
            {
                var mem = new Dictionary<string, dynamic>();
                mem.Add("UserName", username);
                member_list.Add(mem);
            });

            var url = base_url + String.Format("webwxcreatechatroom?pass_ticket={0}&r={1}", pass_ticket, get_timestamp());
            var http = WebRequest.Create(url) as HttpWebRequest;
            http.CookieContainer = cookieContainer;
            http.ContentType = "application/json; charset=UTF-8";
            http.Method = "POST";
            var serializer = new JavaScriptSerializer();
            var base_req_param = new { Uin = Int64.Parse(wxuin), Sid = wxsid, Skey = skey, DeviceID = deviceId };
            var payload = serializer.Serialize(new { BaseRequest = base_req_param, MemberCount = member_list.Count,
                                                         MemberList = member_list, Topic = ""});
            Console.WriteLine(payload);
            var response = getPostResponse(http, payload);
            var dic = deserilizeJson(response);
            var room_name = dic["ChatRoomName"];
            member_list = new List<Dictionary<string, dynamic>>(dic["MemberList"].
                                                           ToArray(typeof(Dictionary<string, dynamic>)));
            var deleted_list = new List<string>();
            member_list.ForEach(member =>
            {
                if ((int) member["MemberStatus"] == 4)
                {
                    deleted_list.Add(member["UserName"]);
                }
            });

            string err_msg = dic["BaseResponse"]["ErrMsg"];
            return Tuple.Create(room_name, deleted_list, err_msg);
        }

        private string unpack(string delimiter, List<string> ls)
        {
            string rst = "";
            for (int i = 0; i < ls.Count; i++)
            {
                if (i != ls.Count - 1)
                {
                    rst += rst + ls[i] + delimiter;
                } else
                {
                    rst += rst + ls[i];
                }              
            }
            return rst;
        }

        private Boolean deleteMember(string room_name, List<string> deleted_list)
        {
            var url = base_url + String.Format("webwxupdatechatroom?fun=delmember&pass_ticket={0}", pass_ticket);
            var http = WebRequest.Create(url) as HttpWebRequest;
            http.CookieContainer = cookieContainer;
            http.ContentType = "application/json; charset=UTF-8";
            http.Method = "POST";
            var serializer = new JavaScriptSerializer();
            var base_req_param = new { Uin = Int64.Parse(wxuin), Sid = wxsid, Skey = skey, DeviceID = deviceId };
            var payload = serializer.Serialize(new
            {
                BaseRequest = base_req_param,
                ChatRoomName = room_name,
                DelMemberList = deleted_list //unpack(",", deleted_list)
            });
            Console.WriteLine(payload);
            var response = getPostResponse(http, payload);
            var dic = deserilizeJson(response);
            var err_msg = dic["BaseResponse"]["ErrMsg"];
            var ret = dic["BaseResponse"]["Ret"];
            if ((int) ret != 0)
            {
                return false;
            }
            return true;
        }

        private List<String> addMember(string room_name, List<string> user_names)
        {
            var url = base_url + String.Format("webwxupdatechatroom?fun=addmember&pass_ticket={0}", pass_ticket);
            var http = WebRequest.Create(url) as HttpWebRequest;
            http.CookieContainer = cookieContainer;
            http.ContentType = "application/json; charset=UTF-8";
            http.Method = "POST";
            var serializer = new JavaScriptSerializer();
            var base_req_param = new { Uin = Int64.Parse(wxuin), Sid = wxsid, Skey = skey, DeviceID = deviceId };
            var payload = serializer.Serialize(new
            {
                BaseRequest = base_req_param,
                ChatRoomName = room_name,
                AddMemberList = user_names //unpack(",", user_names)
            });
            Console.WriteLine(payload);
            var response = getPostResponse(http, payload);
            var dic = deserilizeJson(response);           
            var member_list = new List<Dictionary<string, dynamic>>();
            var deleted_list = new List<string>();

            if ((int)dic["BaseResponse"]["Ret"] != 0)
            {
                //deleted_list.Add("@@@"); // denote an error
                //return deleted_list;
            }

            member_list.ForEach(memebr =>
            {
                if ((int) memebr["MemberStatus"] == 4)
                {
                    deleted_list.Add(memebr["UserName"]);
                }
            });         
            return deleted_list;
        }

        private string UTF8encode(string str)
        {
            byte[] bytes = Encoding.Default.GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

        private void updateUITextLine(RichTextBox control, string text, string end)
        {
            control.Invoke(new Action(() =>
            {
                control.AppendText(text + end);
                control.ScrollToCaret();
            }));
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
#if (!DEBUG)
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
                if (!login())
                {
                    info_display.Invoke(new Action(() =>
                  {
                      info_display.AppendText(String.Format("登录失败{0}", Environment.NewLine));
                      info_display.ScrollToCaret();
                      return;
                  }));
                }

                if (!webwxinit())
                {
                    info_display.Invoke(new Action(() =>
                    {
                        info_display.AppendText(String.Format("初始化失败{0}", Environment.NewLine));
                        info_display.ScrollToCaret();
                        return;
                    }));
                }
#endif

#if (DEBUG)
                start_btn.Invoke(new Action(() =>
                {
                    this.Controls.Remove(start_btn);
                    start_btn.Dispose();
                }));
                qrcode_img.Invoke(new Action(() =>
                {
                    this.Controls.Remove(qrcode_img);
                    qrcode_img.Dispose();
                }));

                info_display.Invoke(new Action(() =>
                {
                    info_display.Location = new System.Drawing.Point(0, 0);
                    info_display.Size = new Size(333, 438);
                }));
#endif
                var member_list = webwxgetcontact();

                Console.WriteLine("member_list: ");
                foreach(var member in member_list)
                {
                    Console.WriteLine(member["UserName"]);
                }
                
                var member_count = member_list.Count;
                info_display.Invoke(new Action(() => 
                {
                    info_display.AppendText(String.Format("通讯录共{0}位好友{1}", member_count, Environment.NewLine));
                    info_display.ScrollToCaret();
                }));

                var room_name = "";
                List<string> result = new List<string>();
                var d = new Dictionary<string, dynamic>();
                member_list.ForEach(member =>
                {
                    d[member["UserName"]] = Tuple.Create<string, string>((member["NickName"]),
                                            (member["RemarkName"]));
                });
                info_display.Invoke( new Action( ()=>
                {
                    info_display.AppendText(String.Format("开始查找...{0}", Environment.NewLine));
                    info_display.ScrollToCaret();
                }));
                var group_num = (int)Math.Ceiling(member_count / (float) MAX_GROUP_NUM);              
                for (int i = 0; i < group_num; i++)
                {
                    var usernames = new List<string>();
                    for (int j = 0; j < MAX_GROUP_NUM; j++)
                    {
                        if (i * MAX_GROUP_NUM + j >= member_count)
                        {
                            break;
                        }
                        var member = member_list[i * MAX_GROUP_NUM + j];
                        usernames.Add(member["UserName"]);
                    }
                    List<string> deleted_list;
                    if (String.IsNullOrEmpty(room_name))
                    {
                        var tuple = createChatRoom(usernames);
                        room_name = tuple.Item1;
                        if (String.IsNullOrEmpty(room_name))
                        {
                            if (tuple.Item3.Equals("Too many attempts. Try again later."))
                            {
                                updateUITextLine(info_display, "操作过于频繁，请稍后再试", Environment.NewLine);
                                return;
                            }
                        }
                        Console.WriteLine("room name: " + room_name);
                        deleted_list = tuple.Item2;
                    }
                    else
                    {
                        deleted_list = addMember(room_name, usernames);
                        if(deleted_list[deleted_list.Count - 1].Contains("@@@"))
                        {
                            //updateUITextLine(info_display, "操作过于频繁，请稍后再试", Environment.NewLine);
                            //return;
                        }
                    }

                    if (deleted_list.Count > 0)
                    {
                        result.AddRange(deleted_list);
                    }

                    if(!deleteMember(room_name, usernames))
                    {
                        //updateUITextLine(info_display, "操作过于频繁，请稍后再试", Environment.NewLine);
                        //return;
                    }
                    updateUITextLine(info_display, String.Format("新发现你被{0}人删除：" + Environment.NewLine,
                                        deleted_list.Count), Environment.NewLine);
                    int k = 0;
                    for (k = 0; k < deleted_list.Count; k++)
                    {
                        if (!String.IsNullOrEmpty(d[deleted_list[k]].Item2))
                        {
                            updateUITextLine(info_display, String.Format("{0}", d[deleted_list[k]].Item2), Environment.NewLine);
                        }
                        else
                        {
                            updateUITextLine(info_display, String.Format("{0}", d[deleted_list[k]].Item1), Environment.NewLine);
                        }
                    }
                    if (k != group_num - 1)
                    {
                        updateUITextLine(info_display, "", Environment.NewLine);
                        updateUITextLine(info_display, "正在继续查找,请耐心等待...", Environment.NewLine);
                        System.Threading.Thread.Sleep(16000);
                    }
                }
                updateUITextLine(info_display, Environment.NewLine + "结果汇总完毕,20s后可重试...", Environment.NewLine);
                var result_names = new List<string>();
                result.ForEach(r =>
                {
                    if (!String.IsNullOrEmpty(d[r].Item2))
                    {
                        result_names.Add(d[r].Item2);
                    }
                    else
                    {
                        result_names.Add(d[r].Item1);
                    }
                });

                updateUITextLine(info_display, String.Format("被删除的好友列表(共{0}人):", result.Count), Environment.NewLine);
                string pattern = "<span.+/span>";
                string replacement = "";
                Regex rgx = new Regex(pattern);
                result_names.ForEach(name =>             
                    rgx.Replace(name, replacement)
                );
                if (result_names.Count > 0)
                {
                    result_names.ForEach(name => 
                    {
                        for (int i = 0; i < COL_NUM; i++)
                        {
                            if (i != COL_NUM - 1)
                            {
                                updateUITextLine(info_display, name, " ,");
                            }
                            else
                            {
                                updateUITextLine(info_display, name, Environment.NewLine);
                            }
                        }
                    } ); 
                } else
                {
                    updateUITextLine(info_display, "无", Environment.NewLine);
                }
            }
            updateUITextLine(info_display, "--------------------------------------", "");
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        private void main()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            info_display.AppendText("正在发送请求……" + Environment.NewLine);
            info_display.ScrollToCaret();
            bw.RunWorkerAsync();
        }
    }
}
