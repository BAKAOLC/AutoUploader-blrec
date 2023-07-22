## 说明
纯自用，为了让 [blrec](https://github.com/acgnhiki/blrec) 能支持自动上传随便写的简单webhook

## 使用方式
1. 下载 [biliup-rs](https://github.com/biliup/biliup-rs) 并将命令行工具放置在环境变量可以查找到的位置（也可以直接放在本程序路径），并命名为 `biliup` (windows下可为 `biliup.exe`)
2. 在本程序路径下使用 `biliup login` 进行登录 (或者将 `cookie.json` 复制到本程序路径下)
3. 启动程序，将 `http://127.0.0.1:6780/webhook` 填写进 blrec 的 webhook 设置中

## 注意
程序只处理转码完成事件，只处理 mp4 文件
录制路径模板需配置为 `{roomid}/{year}-{month}-{day}/【{uname}】{title} ({year}年{month}月{day}日{hour}时{minute}分{second}秒)`
请启用保存封面并将封面保存策略配置为默认