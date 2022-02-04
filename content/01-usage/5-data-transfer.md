---
title: "数据迁移"
date: 2020-02-06T11:05:55+08:00
draft: false
weight: 5
---

V2RayGCon（旧版）迁移到V2RayG（新版）时，直接复制userSettings.json就可以。但是由于v2ray-core v5的服务器配置格式和v4的不同，所以全部服务器都不可用。这时要删除全部服务器，然后从旧版全选复制ss/vmess/vless链接，再在新版中导入。由于v5的配置项和v4不完全相同，所以有些服务器可能无法导入或者导入后无法使用。