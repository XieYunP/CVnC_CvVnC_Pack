# ZH CVnC/CvVnC Phonemizer & ZH YUE CVnC/CvVnC Phonemizer
## 音素化器技术文档

---

## 一、项目概览

本插件为 OpenUtau 提供两个音素化器：

| # | 类名 | Tag | 语言 |
|---|------|-----|------|
| 1 | `ZHCVnCCvVnCPhonemizer` | `ZH CVnC/CvVnC` | ZH |
| 2 | `ZHYUECVnCCvVnCPhonemizer` | `ZH YUE CVnC/CvVnC` | ZH-YUE |

两者共享同一套音素分配逻辑，唯一区别：

- **普通话版**：`Romanize` 使用 `BaseChinesePhonemizer.Romanize`（汉字 → 无声调拼音）
- **粤语版**：`Romanize` 使用 `Pinyin.Jyutping.Instance.HanziToPinyin`（汉字 → 无声调粤拼）

**适用声库**：CVnC / CvVnC 拆音方案（VCCV 风格，含介母、韵尾衔接部）

**依赖**：

- `OpenUtau.dll` / `OpenUtau.Core.dll` / `OpenUtau.Plugin.Builtin.dll`
- `Serilog.dll`
- `Pinyin.dll`（仅粤语版需要 Jyutping）

**目标框架**：

- net8 版：`net8.0-windows`（兼容旧版 OpenUtau）
- net10 版：`net10.0-windows`（兼容新版 OpenUtau）

---

## 二、文件组成

**普通话版**：

- `ZH_CVnC_CvVnC_Phonemizer.cs`
- `ZH_CVnC_CvVnC_Phonemizer.csproj`（net8）
- `ZH_CVnC_CvVnC_Phonemizer.net10.csproj`（net10）

**粤语版**：

- `ZH_YUE_CVnC_CvVnC_Phonemizer.cs`
- `ZH_YUE_CVnC_CvVnC_Phonemizer.csproj`（net8）
- `ZH_YUE_CVnC_CvVnC_Phonemizer.net10.csproj`（net10）

**项目文件 (.csproj) 参考**：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="OpenUtau">
      <HintPath>...\OpenUtau.dll</HintPath>
    </Reference>
    <Reference Include="OpenUtau.Core">
      <HintPath>...\OpenUtau.Core.dll</HintPath>
    </Reference>
    <Reference Include="OpenUtau.Plugin.Builtin">
      <HintPath>...\OpenUtau.Plugin.Builtin.dll</HintPath>
    </Reference>
    <Reference Include="Serilog">
      <HintPath>...\Serilog.dll</HintPath>
    </Reference>
    <!-- 仅粤语版需要 -->
    <Reference Include="Pinyin">
      <HintPath>...\Pinyin.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

**安装**：将编译好的 `.dll` 复制到 `OpenUtau安装目录\Plugins\` 后重启。

**注意**：net8 版与 net10 版的 `[Phonemizer]` Tag 相同，**同一个 OpenUtau 的 `Plugins\` 目录里只能放一个版本**，否则会冲突。

---

## 三、scheme.ini 规则文件格式

**位置优先级**：

1. 声库目录 `\<singer.Location>\scheme.ini`
2. 插件目录 `\scheme.ini`

**每一行格式**：

```
pinyin_or_jyutping = Prefix, Main, "Transition1,Transition2,...", Ending
```

**字段说明**：

| 字段 | 说明 |
|------|------|
| `Prefix` | 开头/连接辅音（如 `b`、`ch`、`x`），可为空 |
| `Main` | 主音素（CV 或元音部分，如 `bA`、`xi_`、`la`） |
| `Transition` | 过渡音素，用逗号分隔，可多个，用双引号包裹，可为空 |
| `Ending` | 尾音（如 `u~`、`n~`、`ng~`），可为空 |

**命名后缀约定**：

| 命名 | 性质 | 示例 |
|------|------|------|
| 以 `_` 结尾 | 介母衔接部（拉伸） | `ie'_`、`uo_`、`iA_` |
| 以 `_` 开头 | 韵尾衔接部（固定） | `_Au`、`_e'n`、`_ing` |
| 不以 `_` 结尾的 Main | 拉伸（元音主体） | `bA`、`la` |
| 以 `_` 结尾的 Main | 固定 | `su_`、`xi_`、`ti_` |

**示例行**：

```ini
xian   = x, xi_, "ie'_,_e'n", n~
suo    = s, su_, "uo_", o
bao    = b, bA, "_Au", u~
la     = l, la, "", a
ni     = n, ni, "", i
```

---

## 四、核心概念

### 4.1 音符组 (Note[] notes)

OpenUtau 会把一个主音符以及它后面连续的 `+` 延音合并成一个 `Note[]` 数组传给 `Process`：

- `notes[0]`：主音符（含真实歌词）
- `notes[1..]`：所有 `+` 延音
- `totalDuration = sum(notes[i].duration)`：整个音符合并后的总时长

因此代码里不需要单独处理 `+`，只把 `totalDuration` 当作整体。

### 4.2 固定音素 vs 拉伸音素

**固定音素**：

- 长度锁死在 oto 物理值上，不随 `totalDuration` 变化
- 例：`_Au`、`_e'n`、`_ing`、`n~ R`、以 `_` 结尾的 Main（`su_`、`xi_`）

**拉伸音素**：

- 长度 = 上下两个固定锚点之间的空隙，自动填充
- 例：`bA`、`la`、`ie'_`、`uo_`、`iA_`

### 4.3 oto 字段

`UOto` 包含：

| 字段 | 说明 |
|------|------|
| `Offset` | 音频起点偏移 |
| `Consonant` | 辅音时长（从音素起点到元音开始） |
| `Cutoff` | 右边界（负值，从音频末端往前数的距离） |
| `Preutter` | 预发声时长 |
| `Overlap` | 交叉淡入点 |

**关键派生量**：

- `voiceLen = (-Cutoff) - Preutter` — 预发声到右边界长度
- `consonant = Consonant` — 辅音物理长度
- `medialLen = (-Cutoff) - Preutter` — 对 `_` 结尾的 Main 而言，代表它采样里"介母段"的长度

### 4.4 toneShift 类型差异

| OpenUtau 版本 | `PhonemeAttributes.toneShift` 类型 | 写法 |
|---|---|---|
| net8 | `int` | `note.tone + attr.toneShift` |
| net10 | `int?` | `note.tone + (attr.toneShift ?? 0)` |

同一份源码不能同时用于两个版本。net8 与 net10 项目需各自维护对应的 `.cs` 文件。

---

## 五、音素分配算法

### 5.1 整体流程

```
Process():
  1. 读取主音符、attr0、attr1
  2. totalDuration = sum(notes[i].duration)
  3. 查 scheme.ini 得到 currentRule
  4. hasPrev / hasNext 判定
  5. 开头音 → ProcessOnset
     非开头 → ProcessContinuation
  6. ProcessTransitions 放置过渡音素
  7. 最后一个音 → ProcessEnding 加尾音到静音
```

### 5.2 开头音 ProcessOnset

优先级：

1. **尝试 `- Prefix`（VC 音素）**
   - `vcVoice = -Cutoff - Preutter`（VC 自身的预发声到右边界）
   - `mainVoice = -Cutoff - Preutter`（Main 的预发声到右边界）
   - `avgMs = (vcVoice + mainVoice) / 2`，但 `avgMs` 不超过 `vcVoice`（自身真实长度）
   - `vcLength = MsToTick(avgMs) × consonantStretchRatio`（用户可调）
   - 下限 20 tick
   - → 加 `VC(position = -vcLength)` + `Main(position = 0)`

2. **尝试 `- Main`（整音开头）**
   - → 只加一个音素 `(position = 0)`，不再加 Main

3. **回退**：只加 `Main(position = 0)`

### 5.3 非开头音 ProcessContinuation

**有 Prefix**：

1. **ncv 优先**：`"prevEnding Main"`（如 `"n~ zA"`）
   - 若命中，只加这一个音素 `(position = 0)`，取代 VC + Main，直接 `return`
2. **候选链**：`"prevEnding Prefix"` → `"Prefix"` → `"Main"`
   - `VC 长度 = ComputeVcLength()`
     = `-Cutoff - Preutter`（预发声点到右边界），最小 30
   - **平均法**：`vcLength = (vcLength + prevEndVoice - prevOverlap) * 2/3`
   - **三重上限**（取最小）：
     - `prevSpan / 3`（`prevSpan = max(前音符 duration, 时间跨度)`）
     - 自身 `-Cutoff - Preutter`（`vcOto` 的预发声到右边界）
     - 空隙 `gap`（若 `gap > 0`）
   - 下限：10 tick
   - → 加 `VC(position = -vcLength)` + `Main(position = 0)`

**无 Prefix**：

- 候选链：`"prevEnding Main"` → `"Main"`
- → 加连接别名或 `Main(position = 0)`

### 5.4 过渡音素 ProcessTransitions

步骤：

1. **收集有效过渡音素**（必须存在于声库）
   - 以 `_` 结尾且不以 `_` 开头 → 介母衔接部
   - 以 `_` 开头 → 韵尾衔接部（固定）

2. **尾音预留**：`endingReserve = min(T/6, 60)`，最小 20

3. **计算 mainRaw**：
   - **若 Main 以 `_` 结尾（固定，如 `ji_`）**：
     - `a = -Cutoff(Main) - Preutter(Main)` ← Main 采样里介母部分的长度
     - `b = Preutter(下一个介母衔接部)` ← med 采样里介母部分的长度
     - `mainRaw = (a + b) / 2` ← 平均法，两者物理对等
     - 若 a、b 都取不到，回退 `voiceLen(Main)`
     - 下限：`if (mainRaw < 20) mainRaw = 20`
   - **若 Main 不以 `_` 结尾（拉伸，如 `gO`）**：
     - `mainRaw = voiceLen(Main)` ← 供总和估算，不固定位置
     - 下限：`if (mainRaw < 20) mainRaw = 20`

4. **计算介母期望时长** `medialExpected`：
   - 每个介母 = `max(Preutter, Overlap)`，最小 20

5. **计算所需总时长**：
   - `fixedSum = endingReserve + mainRaw（若 main 固定）+ 所有韵尾 rawDur`
   - `stretchSum = mainRaw（若 main 拉伸）+ 所有介母 medialExpected`
   - `totalNeeded = fixedSum + stretchSum`
   - `scale = min(1, T / totalNeeded)`

6. **应用 scale**：
   - `actualMain = mainRaw × scale`（仅当 main 固定；拉伸型 main 不主动缩）
   - `actualEnding = endingReserve × scale`
   - `actualTrans[i] = rawDur 或 medialExpected × scale`

7. **布局**：
   - **6a. 从右往左排韵尾（非介母）**：
     - `cursor = T - actualEnding`
     - 倒序遍历，跳过介母，依次 `cursor -= actualTrans[i]`
     - 下限 0，记录 `positions[i]`，`placed[i] = true`
   - **6b. 介母紧贴 main 右边缘，占据剩余空间**：
     - `newPos = actualMain`
     - 下一锚点 = 后面第一个 `placed[j]` 的 `positions[j]`（若无则 `T - actualEnding`）
     - 若 `newPos ≥ nextAnchor`，限制到 `nextAnchor - 10`
     - `positions[i] = newPos`，`placed[i] = true`

8. 添加音素

### 5.5 时长分配策略

设：

- `fixedSum = endingReserve + mainRaw（若 main 固定）+ 所有韵尾 rawDur`
- `stretchSum = mainRaw（若 main 拉伸）+ 所有介母 medialExpected`
- `totalNeeded = fixedSum + stretchSum`

**统一缩放**：

- 若 `totalDuration >= totalNeeded`：`scale = 1`，各音素保持原时长
- 若 `totalDuration < totalNeeded`：`scale = totalDuration / totalNeeded`，所有音素等比缩

**应用**：

- `actualMain = mainRaw × scale`（仅当 main 固定；拉伸型 main 不主动缩，由布局自动处理）
- `actualEnding = endingReserve × scale`
- `actualTrans[i] = rawDur 或 medialExpected × scale`
- 结果：所有部分的视觉总和 ≤ `totalDuration`，不溢出

**注**：main 固定时，其视觉长度在布局阶段会被"拉伸"到介母起点（6b），这是引擎采样尾部填充的结果，视觉表现符合"元音拉伸"。

### 5.6 尾音到静音 ProcessEnding

- **条件**：无下一个音符 且 `rule.Ending` 非空
- **音素**：`"Ending R"` 优先，找不到用 `"Ending -"`
- **长度**：`min(totalDuration / 6, 60)`
- **位置**：`totalDuration - endingLength`

### 5.7 介母布局

- 介母（以 `_` 结尾的过渡音素）起点 = `mainRaw`
- 即紧贴 main 右边缘
- 剩余空间（`mainRaw` → 下一锚点）由介母占据
- 视觉长度 = `nextAnchor − mainRaw`
- 若起点越过下一锚点，限制到 `nextAnchor − 10`
- **目的**：遇到介母就固定起点，遇到元音就让元音拉伸

### 5.8 main 下限

- 只有硬下限 `mainRaw = max(mainRaw, 20)`
- 不再随 T 变（取消 `T/10` 和 `T/3` 的比例限制）
- 目的：让固定音素真正"固定"，不被比例撑大
- 布局 6a 的 `cursor` 最低可为 0，不再保留 `minMain` 区

---

## 六、G2P（汉字转拼音）

### 6.1 触发时机

OpenUtau 在渲染前调用 `SetUp()` 一次：

1. 收集所有音符的主歌词
2. 调用 `Romanize()` 批量转换
3. 通过 `ChangeLyric()` 把拼音写回 `lyrics`

之后 `Process()` 拿到的 `lyric` 已是拼音。

### 6.2 普通话版

```csharp
protected virtual string[] Romanize(IEnumerable<string> lyrics)
{
    return BaseChinesePhonemizer.Romanize(lyrics);
}
```

- 使用 `OpenUtau.Core` 内置字典
- 输出无声调拼音（如 `"ni"`、`"hao"`）
- 支持简体 + 繁体汉字

### 6.3 粤语版

```csharp
protected virtual string[] Romanize(IEnumerable<string> lyrics)
{
    return Pinyin.Jyutping.Instance.HanziToPinyin(
        lyrics.ToList(),
        Pinyin.CanTone.Style.NORMAL,
        Pinyin.Error.Default
    ).Select(res => res.pinyin).ToArray();
}
```

- 使用 Pinyin 库的 Jyutping 转换器
- 输出无声调粤拼（如 `"nei"`、`"hou"`）
- 支持简体 + 繁体汉字

### 6.4 注意事项

- `scheme.ini` 的键必须与 G2P 输出一致（无声调）
- 若 G2P 抛异常，音素器会回退到把汉字当音素名直接发送

---

## 七、参数与属性

### 7.1 音符属性 attr0 / attr1

从 `note.phonemeAttributes` 读取，`index` 区分：

- `index 0` → 主音素（`voiceColor`、`toneShift`、`alternate`）
- `index 1` → 辅音（`consonantStretchRatio`、`voiceColor`）

**字段**：

| 字段 | 说明 |
|------|------|
| `voiceColor` | 音色后缀（如 `"Soft"`），会附加到音素名 |
| `toneShift` | 音高偏移（net8 是 `int`，net10 是 `int?`） |
| `alternate` | 替代音源编号 |
| `consonantStretchRatio` | 辅音拉伸倍率（仅对 VC 开头音生效） |

### 7.2 音色后缀 AppendVoiceColor

规则：

- 若音素名已以 `" <color>"` 结尾，不重复添加
- 否则追加 `" <color>"`

---

## 八、常见问题排查

> **注意**：音素器目前没有日志输出，所有问题只能从音素轨道上的视觉效果和听感反推。

### 8.1 使用者常见问题

**[U1] 输入汉字没反应**

A：`scheme.ini` 的键必须与 G2P 输出一致（无声调拼音 / 粤拼）。若声库目录下没有 `scheme.ini`，会回退到插件目录的 `scheme.ini`。

**[U2] 音素器不显示在列表**

A：

- 检查 OpenUtau 的 `Plugins\` 目录，DLL 是否复制进去
- 确认 OpenUtau 版本与 DLL 目标框架匹配（net8 版对 net8 版 OpenUtau，net10 版对 net10 版）
- **同一个 `Plugins\` 目录里不能同时放 net8 和 net10 两个版本**，Tag 相同会冲突，只保留当前 OpenUtau 版本对应的那份

**[U3] 音符被唱出奇怪的效果**

A：逐项排查：

- 音素未发声 → 检查 `scheme.ini` 的 `Main` 字段是否拼写正确、声库 oto 里是否有该别名
- 辅音太长挤掉元音 → 检查 oto 里该 VC 的 `Cutoff` 是否过大（`-Cutoff - Preutter` 决定 VC 视觉长度）
- 介母被拉伸过长 → 检查 oto 里 `Preutter` 和 `Overlap`，`mainRaw = (a + b) / 2` 决定 main 视觉长度
- 短音时音素挤成一团 → 属于极限情况，所有音素被等比缩，无解

### 8.2 开发者常见问题

**[D1] 音符总时长判定异常**

A：OpenUtau 会把主音符和它后面所有 `+` 合并成 `notes[]`，`totalDuration = sum(notes[i].duration)`。若处理孤立 `+`（`notes[0].lyric == "+"`），建议返回 `MakeSimpleResult("-", attr0)`。

**[D2] net8 编译时提示 `??` 不可套用至 'int'**

A：net8 的 `toneShift` 是 `int`，net10 是 `int?`。全局替换：

- net8 源码：`(attr.toneShift ?? 0)` → `attr.toneShift`
- net10 源码：保持 `(attr.toneShift ?? 0)`

**[D3] 音素视觉效果不符合预期**

A：无日志时按下列顺序肉眼诊断：

1. **main 视觉过短** → `mainRaw` 被 `min(a, b)` 或平均法算小 → 检查 oto 里 `Preutter` 和 `Cutoff`
2. **main 视觉过长** → 检查 `minMain` 或比例上限是否被误加（应只有 `max(20, mainRaw)`）
3. **介母视觉过短** → 6b 中 `newPos ≥ nextAnchor` 被触发 → 检查 6a 韵尾位置
4. **整体溢出音符范围** → 检查 `scale = min(1, T / totalNeeded)` 是否被误删
5. **VC 视觉过长** → 检查三重上限（`prevSpan / 3`、自身 `-Cutoff - Preutter`、空隙 `gap`）

**[D4] 两个版本（net8 / net10）行为不一致**

A：源码需各自独立维护。改一处逻辑（如 `mainRaw` 公式）时，**两份 `.cs` 都要同步改**，尤其注意 `toneShift` 写法差异。

**[D5] 编译通过但音素器加载后无效**

A：

- 检查 `[Phonemizer]` 的 Tag 是否与其他插件重复
- 检查 `class` 是否被 `public` 修饰
- 检查 DLL 是否被 OpenUtau 识别（看 OpenUtau 日志或 `Plugins\` 目录下的 `.pdb` 是否加载）

---

## 九、开发历程与关键决策

**阶段 1：工程版本**

探索与试错阶段，验证音素器架构可行性。

- 失败：`SetUp` 里手动合并 `+` 到前音符，与 OpenUtau 架构冲突
- 失败：空音符数组返回导致渲染崩溃
- 重构：删除 `SetUp` 里的合并逻辑，改为信任 `notes[]` 数组；孤立 `+` 返回静音占位符
- 引入以 `_` 结尾 / 开头判断，分离"拉伸音素"与"固定音素"
- 从右往左布局固定音素，介母、韵尾、整音分别处理
- oto 字段迭代：试过 `Consonant`、`Preutter`、`-Cutoff-Preutter`、平均值
- 缩短策略迭代：失败过"全部等比缩"、"拉伸先缩 20%"、"T >= fixedSum 判定"
- 长度限制：开头 VC 不超过自身真实长度；中间 VC 有平均法 + `prevSpan/3` 上限 + 空隙避让

**阶段 2：发布版本**

在工程版本基础上重构、修正与扩充，形成稳定发布版。

- **过渡音素布局重构**：
  - 原问题：介母、韵尾、main 混排，长音时 main 视觉被不合理撑大
  - 重构：引入 `placed[]` 追踪已定位音素，拆成 6a（韵尾从右往左排）和 6b（介母紧贴 main 右边缘）
  - 结果：main 视觉固定，介母占据剩余空间，元音在听感上拉伸
- **main 固定时长定稿**：
  - `a = -Cutoff(main) - Preutter(main)`（main 采样里介母部分的长度）
  - `b = Preutter(med)`（med 采样里介母部分的长度）
  - `mainRaw = (a + b) / 2`（两个物理对等的量取平均）
  - 硬下限只有 20 tick，取消 `T/10`、`T/3` 比例限制
- **时长分配定稿**：
  - `scale = min(1, T / totalNeeded)`，所有音素统一等比缩
  - 保证视觉总和 ≤ `totalDuration`
- **介母期望时长定稿**：`max(Preutter, Overlap)`，最小 20
- **net10 兼容版本**：
  - 背景：OpenUtau 主程序升级到 net10 后，旧 net8 插件无法加载
  - 方案：新增 net10 目标框架的 csproj 副本，源码独立维护
  - 差异：`PhonemeAttributes.toneShift` 在 net8 是 `int`，在 net10 是 `int?`
  - 写法：net8 用 `attr.toneShift`，net10 用 `(attr.toneShift ?? 0)`
  - 结果：net8 与 net10 两份 DLL，分别放入对应版本的 OpenUtau

---

## 十、代码结构导航

**类**：

- `ZHCVnCCvVnCPhonemizer`（普通话版）
- `ZHYUECVnCCvVnCPhonemizer`（粤语版，结构完全相同）

以下是两者共有的成员（粤语版多一个 Pinyin 引用）：

| 成员 | 说明 |
|------|------|
| `SetSinger()` | 加载 `scheme.ini` |
| `LoadSchemeFile()` | 解析规则文件 |
| `ParseRuleString()` | 处理带引号的规则串 |
| `Romanize()` | [虚拟] G2P 转换 |
| `ChangeLyric()` | 写回拼音 |
| `SetUp()` | 批量 G2P 入口 |
| `Process()` | 主入口 |
| `ProcessOnset()` | 开头音 |
| `ProcessContinuation()` | 非开头音 |
| `ComputeVcLength()` | VC 长度计算 |
| `GetVoiceLength()` | `-Cutoff - Preutter` |
| `GetFixedDuration()` | `(voiceLen + consonant) / 2` |
| `ResolveAlias()` | 音素别名解析 |
| `ProcessTransitions()` | 过渡音素布局 |
| `ProcessEnding()` | 尾音到静音 |
| `GetAttr()` | 读取音符属性 |
| `AddMainPhoneme()` | 添加主音素 |
| `CheckOtoUntilHit()` | 依次尝试候选别名 |
| `CheckOtoExists()` | 单音素存在性检查 |
| `AppendVoiceColor()` | 音色后缀 |
| `MakeSimpleResult()` | 快捷返回 |
| `CVnCRule` | 规则数据结构 |

**字段**：

| 字段 | 说明 |
|------|------|
| `singer` | `USinger` 实例 |
| `rules` | `Dictionary<string, CVnCRule>` |

---

*本文档及代码逻辑由 DeepSeek 辅助开发。*