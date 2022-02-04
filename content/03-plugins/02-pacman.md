---
title: "Pacman"
date: 2020-02-01T17:46:32+08:00
draft: false
weight: 2
---

这个插件用于将多个服务器合并成一个服务器包。

首先在主窗口中钩选你想合并的服务器，然后点“拉取”小按钮。  
也可以直接从主窗口拖服务器到“内容”中的空白区。  
{{< figure src="../../images/plugins/plugin_pacman.png" >}}

然后点“打包”或“串连”，此时主窗口会多出一个和设置同名的服务器
{{< figure src="../../images/plugins/form_main_pkgv5.png" >}}

想知道原理可以点开Json编辑器，查看具体配置。  

###### 打包
利用v2ray的balancer，将多个服务器合成带负载均衡功能的服务器包。  

###### 串连
将多个服务器串成一条代理链。  
