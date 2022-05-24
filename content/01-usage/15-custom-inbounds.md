---
title: "自定义Inbounds"
date: 2020-02-01T17:39:27+08:00
draft: false
weight: 15
---

这个软件导入链接时的默认inbound是http://127.0.0.1:8080，你可以在选项窗口修改这个设置。
{{< figure src="../../images/forms/form_option_defaults.png" >}}
其中“http/socks”模式表示将inbounds修改为http/socks协议。  
“Config”表示不修改inbounds，直接使用config.json中的inbounds设置。  
“自定义”表示使用“自定义inbounds”中的内容替换inbounds设置。  


上面的设置仅对新导入的服务器生效，已经导入的服务器可以在“设定及二维码”面板中修改Inbound设置。  
{{< figure src="../../images/forms/form_settings_and_qrcode.png" >}}
提示：点击青色的 (h8080) 标签可以快速调出“设定及二维码”窗口。

同时修改多个服务器时，在多选之后点击“主窗口”-“服务器”-“批量修改”即可。 
{{< figure src="../../images/forms/form_batch_modify.png" >}}

单看文字描述可能比较抽象，在修改上面的设置后可以点开“查看最终配置”观察配置的变化。  
注意：“查看最终配置”只能看不能改，改了也不能保存。  