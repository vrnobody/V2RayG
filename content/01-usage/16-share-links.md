---
title: "各种分享链接"
date: 2020-02-01T22:27:56+08:00
draft: false
weight: 16
---

##### ss://...
仅支持`ss://(base64)#name`形式的分享链接及SIP002链接  

##### trojan://...
仅支持[trojan-url](https://github.com/trojan-gfw/trojan-url)定义的分享链接标准  

##### v2cfg://...
这是本软件自创的一种分享链接。它直接把整个config.json进行base64编码得出，主要用于备份/还原数据。因为v2ray功能过于强大，有可能被有心人利用，通过revers把本地端口暴露到公网，所以这种链接除了`主窗口`-`文件`-`从剪切板导入`外，其他地方都不能导入。  

##### vless://...
支持Xray-core [issues 91](https://github.com/XTLS/Xray-core/issues/91)提出的vless分享链接标准  

##### vmess://...
仅支持v2rayN的vmess(ver2)分享链接，不支持其他vmess分享链接  
