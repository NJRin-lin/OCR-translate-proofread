# OCR 翻译校对工具

macOS 原生 OCR 翻译学习工具，支持截图/图片上传、Vision 框架文字识别、多模型 AI 翻译与语法分析。

## 功能

- **OCR 识别**：macOS Vision 框架本地识别，支持中/日/英，无需网络
- **AI 翻译**：DeepSeek / ChatGPT / Gemini 多模型支持，翻译 + 句子成分分析
- **两种分析模式**：
  - 校对模式 — 语法树拆解句子成分，辅助核对翻译准确性
  - 学习模式 — 语法树 + 语法点详解（附例句）+ 词汇注解（N1~N2）
- **图片框选**：拖拽创建选区 + 8 手柄精调，缩放/平移，识别范围精准映射
- **词汇查询**：内嵌查词输入框，读音/释义/词性/JLPT/例句，支持 Cmd+K 快捷聚焦
- **TTS 朗读**：`AVSpeechSynthesizer` 本地日语语音朗读
- **截图按钮**：一键框选屏幕区域进行 OCR

## 系统要求

- macOS 14.0 (Sonoma) 或更高版本
- Xcode 15+（构建）

## 快速开始

1. 用 Xcode 打开 `OCR-Translate.xcodeproj`
2. 按 Cmd+R 运行
3. 截图或上传图片
4. 框选需要识别的区域 → 确认 → OCR 自动识别
5. 点击「开始翻译分析」→ 翻译 + 句子分析
6. 在设置中配置 API Key（可选，不影响 OCR）

## 技术栈

SwiftUI + Swift · macOS 14+ · Vision 框架 · DeepSeek/OpenAI/Gemini API · AVFoundation

## 许可证

开发者：NJRin  
技术支持：虹之咲学园 Vibe Coding 同好会
