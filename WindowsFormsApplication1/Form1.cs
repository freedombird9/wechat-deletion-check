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
            anchorControls(info_display);
            qrcode_img.Anchor = AnchorStyles.Top;
            start_btn.Anchor = AnchorStyles.Top;
            linkLabel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.MinimumSize = new System.Drawing.Size(333, 495);

            cookieContainer = new CookieContainer();
#if (DEBUG)
            pass_ticket = "6v2gj8oihiKV%2B%2FzFx31fc4zQ8ZB4aNvDfcfavgdseMo8EybRul8OscZylnts%2BKSZ";
            skey = "@crypt_3ca759_e9b5bc9e6cfb79a07a9f49541a9d912f";
            wxsid = "ZHu/WOz9i7GZXcFN";
            wxuin = "2622149902";
            base_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/";
            self = new Dictionary<string, dynamic>();
            //self.Add("UserName", "@b8a8bddd76a0b0e7c98225c86962de1a780a81b595ad18ae4383e647b3b8227f"); // Liao's
            self.Add("UserName", "@7b04f82ebadc2829a6b7c156bb44e411b79413f2861b5e682eb9bec55658f108"); // me
#endif
        }

        private void anchorControls(Control control)
        {
            control.Anchor =
                    AnchorStyles.Bottom |
                    AnchorStyles.Right |
                    AnchorStyles.Top |
                    AnchorStyles.Left;
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

            updateUITextLine(info_display, "正在读取二维码，请稍等……", Environment.NewLine, Color.Black);
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

                    updateUITextLine(info_display, "请使用微信扫描二维码以登录", Environment.NewLine, Color.Black);                 
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
                updateUITextLine(info_display, "扫描成功,请在手机上点击确认以登录", Environment.NewLine, Color.Black);
                tip = 0;
            } else if(code == "200")
            {

                updateUITextLine(info_display, "正在登录", Environment.NewLine, Color.Black);
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
                info_display.Size = new Size(333, 463);
                anchorControls(info_display);
                info_display.Clear();
                info_display.AppendText("正在扫描……" + Environment.NewLine);
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
                updateUITextLine(info_display, ErrMsg, Environment.NewLine, Color.Red);
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
            cookieContainer.Add(new Cookie("webwx_data_ticket", "AQcsqhDcQuYzt1F1QvsvBV9W") { Domain = target.Host });
            cookieContainer.Add(new Cookie("wxuin", "2622149902") { Domain = target.Host });
            cookieContainer.Add(new Cookie("wxsid", "ZHu/WOz9i7GZXcFN") { Domain = target.Host });
            cookieContainer.Add(new Cookie("wxloadtime", "1452642551") { Domain = target.Host });
            cookieContainer.Add(new Cookie("webwxuvid", "20e90937e5f3173d94c75ee2c3805a26a0da27cb6eb2a7f692f9b26c09eb4a2d68ac2e5055bac129bc2e653ddc9b3312") { Domain = target.Host });
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

        private Tuple<string, List<string>, List<string>, string> createChatRoom(List<string> user_names)
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
            var blocked_list = new List<string>();
            member_list.ForEach(member =>
            {
                if ((int) member["MemberStatus"] == 4)
                {
                    deleted_list.Add(member["UserName"]);
                }
                else if ((int) member["MemberStatus"] == 3)
                {
                    blocked_list.Add(member["UserName"]);
                }
            });

            string err_msg = dic["BaseResponse"]["ErrMsg"];
            return Tuple.Create(room_name, deleted_list, blocked_list,err_msg);
        }

        private string unpack(string sep, List<string> ls)
        {
            return string.Join(sep, ls);
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
                DelMemberList = unpack(",", deleted_list)
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

        private Tuple<List<String>, List<string>, string> addMember(string room_name, List<string> user_names)
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
                AddMemberList = unpack(",", user_names)
            });
            Console.WriteLine(payload);
            var response = getPostResponse(http, payload);
            var dic = deserilizeJson(response);           
            var member_list = new List<Dictionary<string, dynamic>>(dic["MemberList"].
                                                           ToArray(typeof(Dictionary<string, dynamic>)));
            var deleted_list = new List<string>();
            var blocked_list = new List<string>();
            member_list.ForEach(member =>
            {
                if ((int) member["MemberStatus"] == 4)
                {
                    deleted_list.Add(member["UserName"]);
                }
                else if ((int) member["MemberStatus"] == 3)
                {
                    blocked_list.Add(member["UserName"]);
                }
            });         
            return Tuple.Create(deleted_list, blocked_list, dic["BaseResponse"]["ErrMsg"]);
        }

        private string UTF8encode(string str)
        {
            byte[] bytes = Encoding.Default.GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

        private void updateUITextLine(RichTextBox control, string text, string end, Color color)
        {
            control.Invoke(new Action(() =>
            {
                control.SelectionColor = color;
                control.AppendText(text + end);
                control.ScrollToCaret();
            }));
        }

        private void conclude(List<string> result, string msg, Dictionary<string, dynamic> d)
        {
            
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

            updateUITextLine(info_display, String.Format(msg + "(共{0}人):", result.Count), Environment.NewLine, Color.Black);
            string pattern = "<span.+/span>";
            string replacement = "";
            Regex rgx = new Regex(pattern);
            result_names.ForEach(name =>
                rgx.Replace(name, replacement)
            );
            if (result_names.Count > 0)
            {
                int i = 0;
                result_names.ForEach(name =>
                {
                    if (i != COL_NUM - 1 && i != result_names.Count - 1)
                    {
                        updateUITextLine(info_display, name, " ,", Color.Red);
                    }
                    else
                    {
                        updateUITextLine(info_display, name, Environment.NewLine, Color.Red);
                        i = 0;
                    }
                    i++;
                });
            }
            else
            {
                updateUITextLine(info_display, "无", Environment.NewLine, Color.Black);
            }
            updateUITextLine(info_display, "--------------------------------------", Environment.NewLine, Color.Black);
        }

        private void result_update(List<string> deleted_list, Dictionary<string, dynamic> d, string msg)
        {
            updateUITextLine(info_display, String.Format("新发现你被{0}人{1}：" + Environment.NewLine,
                                        deleted_list.Count, msg), Environment.NewLine, Color.Black);
            for (int k = 0; k < deleted_list.Count; k++)
            {
                if (!String.IsNullOrEmpty(d[deleted_list[k]].Item2))
                {
                    updateUITextLine(info_display, String.Format("{0}", d[deleted_list[k]].Item2), Environment.NewLine, Color.Red);
                }
                else
                {
                    updateUITextLine(info_display, String.Format("{0}", d[deleted_list[k]].Item1), Environment.NewLine, Color.Red);
                }
            }
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
                    updateUITextLine(info_display, "获取uuid失败", Environment.NewLine, Color.Red);             
                    return;
                }
                showQRImage();
                while (waitForLogin() != "200") ;

                if (!login())
                {
                    updateUITextLine(info_display, "登录失败", Environment.NewLine, Color.Red);
                    return;
                }

                if (!webwxinit())
                {
                    updateUITextLine(info_display, "初始化失败", Environment.NewLine, Color.Red);
                    return;
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
                if(member_list.Count == 0)
                {
                    updateUITextLine(info_display, "好友列表为空", Environment.NewLine, Color.Red);
                    return;
                }

                Console.WriteLine("member_list: ");
                foreach(var member in member_list)
                {
                    Console.WriteLine(member["UserName"]);
                }
                
                var member_count = member_list.Count;
                updateUITextLine(info_display, String.Format("通讯录共{0}位好友", member_count), Environment.NewLine, Color.Black);
                var room_name = "";
                List<string> result = new List<string>();
                List<string> rst_blk = new List<string>();
                var d = new Dictionary<string, dynamic>();
                member_list.ForEach(member =>
                {
                    d[member["UserName"]] = Tuple.Create<string, string>((member["NickName"]),
                                            (member["RemarkName"]));
                });
                updateUITextLine(info_display, "开始查找...", Environment.NewLine, Color.Black);
                var group_num = (int)Math.Ceiling(member_count / (float) MAX_GROUP_NUM);
                Console.WriteLine("***************** group number: " + group_num);
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
                    List<string> blocked_list;
                    if (String.IsNullOrEmpty(room_name))
                    {
                        var tuple = createChatRoom(usernames);
                        room_name = tuple.Item1;
                        if (String.IsNullOrEmpty(room_name))
                        {
                            if (tuple.Item4.Equals("Too many attempts. Try again later."))
                            {
                                updateUITextLine(info_display, "操作过于频繁，请稍后再试", Environment.NewLine, Color.Red);
                                return;
                            } else
                            {
                                Console.WriteLine("***** no chatroom created");
                            }
                        }
                        deleted_list = tuple.Item2;
                        blocked_list = tuple.Item3;
                    }
                    else
                    {
                        var tuple = addMember(room_name, usernames);
                        deleted_list = tuple.Item1;
                        blocked_list = tuple.Item2;
                        var err_msg = tuple.Item3;
                        if (err_msg.Equals("Too many attempts. Try again later."))
                        {
                            updateUITextLine(info_display, "操作过于频繁，请稍后再试", Environment.NewLine, Color.Red);
                            return;  
                        }
                    }

                    if (deleted_list.Count > 0)
                    {
                        result.AddRange(deleted_list);
                    }
                    if (blocked_list.Count > 0)
                    {
                        rst_blk.AddRange(blocked_list);
                    }
                    if (string.IsNullOrEmpty(room_name))
                    {
                        // TO_DO: if failed to create the chatroom 
                    }
                    if(!String.IsNullOrEmpty(room_name) && !deleteMember(room_name, usernames))
                    {
                        updateUITextLine(info_display, "操作过于频繁，请稍后再试", Environment.NewLine, Color.Red);
                        return;
                    }
                    result_update(deleted_list, d, "删除");
                    result_update(blocked_list, d, "拉黑");
                    
                    if (i != group_num - 1)
                    {
                        updateUITextLine(info_display, "", Environment.NewLine, Color.Black);
                        updateUITextLine(info_display, "30秒后继续查找,请耐心等待...", Environment.NewLine, Color.Black);
                        System.Threading.Thread.Sleep(30000); // 30s interval
                    }
                }
                updateUITextLine(info_display, Environment.NewLine + "结果汇总完毕,30s后可重试...", Environment.NewLine, Color.Black);
                conclude(result, "以下联系人已将你删除", d);
                conclude(rst_blk, "以下联系人已将你拉黑", d);
            }          
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
            info_display.AppendText(Environment.NewLine + "正在发送请求……" + Environment.NewLine);
            info_display.ScrollToCaret();
            bw.RunWorkerAsync();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.LinkVisited = true;
            // Navigate to a URL.
            System.Diagnostics.Process.Start("http://blog.yongfengzhang.com");
        }
    }
}
