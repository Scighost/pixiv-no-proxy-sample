20240320，[Pixeval](https://github.com/Pixeval/Pixeval)发布了全新的版本 [4.1.0](https://github.com/Pixeval/Pixeval/releases/tag/4.1.0)。截止到此版本，该项目仍然使用的是统一的自签名 SSL 证书直连 Pixiv，用户需要在自己的设备上信任它，我相信其开发者不会泄露私钥或滥用证书，但是安全隐患是客观存在的。

本 Sample 旨在尝试使用非代理和无需信任证书的方式直连 Pixiv，仅用于测试方法的可行性，可能无法应对长期使用中可能会发生的奇怪问题。

---

两个神奇的域名：

- `www.pixivision.net`
- `s.pximg.net`