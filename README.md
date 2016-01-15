## WeChat Helper -- Windows App

### 功能简介

1. 检测谁删除了你
2. 检测你在谁的黑名单中

### 使用方法

1. 在 https://github.com/freedombird9/wechat-deletion-check/releases 中点击WeChatHelper.exe进行下载，下载之后可以直接运行。如果被SmartScreen警告为有安全威胁，是因为开发仓促，对微软新的签名机制没有来得及做详细了解，故使用的证书可能不被SmartScreen认可。但是软件绝对没有病毒，请放心使用。我会尽快在后续开发中解决这个问题。

2. 进入程序后，点击开始按钮，然后请跟随程序指示操作。如果好友过多，后续的请求可能会被微信block掉，程序会显示操作过于频繁，请稍后再试。会在后续版本中处理此问题。

3. 截图

<div><img src='https://raw.githubusercontent.com/freedombird9/wechat-deletion-check/master/assets/start_screen.png'/></div>

<div><img src='https://raw.githubusercontent.com/freedombird9/wechat-deletion-check/master/assets/login.png'/></div>

<div><img src='https://raw.githubusercontent.com/freedombird9/wechat-deletion-check/master/assets/in_app.png'/></div>

### 原理

1. 创建群组并加人，如果被联系人删除或者屏蔽，则无法加其入群。
2. 因为只创建群不发消息的话，群里的人并不会收到提示，所以此方法不会被好友发现。

### 其他

1. 如果有任何问题，请在本项目issue里提交。

2. 感谢 [@0x5e](https://github.com/0x5e) 同学的 [wechat-deleted-friends](https://github.com/0x5e/wechat-deleted-friends)，非常棒的idea。

3. Chrome插件版：
https://chrome.google.com/webstore/detail/bdfbkchemknlpmmopkncahjdmocnambd/

4. 我的博客：
http://blog.yongfengzhang.com


