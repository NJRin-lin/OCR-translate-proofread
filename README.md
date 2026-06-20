# NGM proofread

日语 OCR 文字识别 + AI 翻译 + 语法校对桌面工具。支持 macOS (SwiftUI) 和 Windows (WPF/.NET 8) 双平台。

---

## 功能

- **图片识别**：截图或上传图片（PNG/JPEG/BMP/TIFF），图片缩放/平移（1x–5x），框选裁剪（8 手柄 + 遮罩叠加层）
- **文字输入**：手动输入/粘贴日文原文，跳过 OCR 直接翻译分析
- **本地 OCR**：macOS Vision 框架 / Windows.Media.Ocr，日语优先，分段/连贯视图，置信度颜色编码（绿 ≥80% / 橙 60–79% / 红 <60%）
- **AI 翻译**：日→中，DeepSeek / ChatGPT / Gemini 三模型支持，术语表强制执行
- **两种分析模式**：
  - **校对模式** — 语法树拆解句子成分
  - **学习模式** — 语法树 + 语法点详解 + 词汇注解（N1–N2）+ 总体备注
- **词汇查询**：内嵌 AI 词典查询，读音/释义/词性/JLPT/例句
- **术语表**：自定义词汇翻译映射，AI 翻译严格遵循
- **TTS 朗读**：日语语音朗读（macOS AVSpeechSynthesizer / Windows System.Speech）

---

## Windows 版

### 系统要求

- Windows 10 19041.0 或更高版本（x64）
- .NET 8 Desktop Runtime（安装器会自动检测并引导安装）

### 安装

#### 推荐：安装器（~4MB）

从 [Releases](../../releases) 下载 `NGMproofread_Setup_x.x.x.exe`，运行后自动安装。

#### 备选：自包含 EXE（~180MB，无需安装运行时）

下载 `NGMproofread.Windows.exe`，直接双击运行。

### 技术栈

WPF (.NET 8) · Windows.Media.Ocr · System.Speech · DeepSeek/OpenAI/Gemini API · HttpClient

### 项目结构

```
win/
├── NGMproofread.Windows.sln
└── NGMproofread.Windows/
    ├── Models/       AIProvider, OCRResult, TranslationResult, AnalysisResult
    ├── Services/     AIService, TranslationService, AnalysisService, OCRService, VocabularyService
    ├── Utilities/    APIKeyStore, GlossaryStore, ImageLoader, ScreenshotManager
    └── Views/        MainWindow, CroppableImageView, ZoomableImageView,
                      OCRResultView, TranslationResultView, AnalysisView,
                      VocabularyLookupView, SettingsWindow
```

### 构建

```bash
# 调试构建
dotnet build

# 框架依赖发布（用于制作安装器）
dotnet publish -c Release -r win-x64 --self-contained false -o ../publish-fd

# 自包含单文件发布
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../publish

# 生成安装器 (需安装 NSIS)
cd win/installer && makensis setup.nsi
```

---

## macOS 版

### 系统要求

- macOS 14.0 (Sonoma) 或更高版本

### 安装

从 [Releases](../../releases) 下载 DMG，双击打开，将 NGM proofread 拖入 Applications 文件夹。

### 技术栈

SwiftUI + Swift · macOS 14+ · Vision 框架 · DeepSeek/OpenAI/Gemini API · AVFoundation

---

## 许可

开发者：NJRin  
技术支持：虹之咲学园 Vibe Coding 同好会
