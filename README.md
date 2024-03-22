
基于 WebView2 的免代理 Pixiv 套壳客户端，内置 [Powerful Pixiv Downloader](https://github.com/xuejianxianzun/PixivBatchDownloader) 扩展。

20240320，[Pixeval](https://github.com/Pixeval/Pixeval) 发布了全新的版本 [4.1.0](https://github.com/Pixeval/Pixeval/releases/tag/4.1.0)。截止到此版本，该项目仍然使用的自签名 SSL 证书反代 Pixiv，开发者持有私钥，用户必须在自己的设备上信任证书，我相信其开发者不会泄露私钥或滥用证书，但是安全隐患是客观存在的。

本 Sample 旨在尝试使用非代理的方式直连 Pixiv，仅用于测试方法的可行性，可能无法应对长期使用中可能会发生的奇怪问题。

两个神奇的域名：

- `www.pixivision.net`
- `s.pximg.net`